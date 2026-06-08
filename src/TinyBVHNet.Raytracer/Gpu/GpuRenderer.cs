using System;
using System.Diagnostics;
using System.Numerics;
using TinyBVHNet;

namespace TinyBVHNet.Raytracer.Gpu;

/// <summary>
/// GPU path tracer: builds GPU BVH from scene in the constructor, then
/// <see cref="Render"/> can be called multiple times with different cameras
/// without rebuilding the BVH or re-uploading to Vulkan.
/// </summary>
public unsafe class GpuRenderer : IRenderer, IDisposable
{
    private readonly VulkanContext _vk;
    private readonly int _width, _height;
    private readonly Action<string>? _progress;
    private bool _sceneLoaded;
    private uint _frameIndex;

    /// <summary>Elapsed GPU time for the last render.</summary>
    public TimeSpan LastGpuTime { get; private set; }

    public GpuRenderer(ObjParser.Scene scene, int width, int height, Action<string>? progress = null)
    {
        _width = width;
        _height = height;
        _progress = progress;
        _vk = new VulkanContext();
        _vk.Initialize(width, height);

        // Build GPU BVH
        progress?.Invoke("Building GPU BVH...");
        using var gpuBvh = new BVHGPU();
        gpuBvh.Build(scene.Vertices, scene.TriangleCount);

        var data = gpuBvh.ExtractGpuData();
        progress?.Invoke($"BVH: {data.NodeCount} nodes, {data.TriangleCount} triangles");

        // Convert managed arrays to byte arrays for upload
        byte[] nodeBytes = FloatsToBytes(data.Nodes);
        byte[] primBytes = UintsToBytes(data.PrimitiveIndices);
        byte[] vertBytes = FloatsToBytes(data.Vertices);

        // Pre-compute triangle data: edge1, edge2, face normal per triangle
        float[] triDataFloats = new float[data.TriangleCount * 12];
        for (int t = 0; t < data.TriangleCount; t++)
        {
            int vb = t * 12;   // 3 float4 per triangle in vertices
            int tb = t * 12;   // triData base

            float v0x = data.Vertices[vb + 0], v0y = data.Vertices[vb + 1], v0z = data.Vertices[vb + 2];
            float v1x = data.Vertices[vb + 4], v1y = data.Vertices[vb + 5], v1z = data.Vertices[vb + 6];
            float v2x = data.Vertices[vb + 8], v2y = data.Vertices[vb + 9], v2z = data.Vertices[vb + 10];

            // edge1 = v1 - v0
            float e1x = v1x - v0x, e1y = v1y - v0y, e1z = v1z - v0z;
            // edge2 = v2 - v0
            float e2x = v2x - v0x, e2y = v2y - v0y, e2z = v2z - v0z;
            // normal = normalize(cross(edge1, edge2))
            float nx = e1y * e2z - e1z * e2y;
            float ny = e1z * e2x - e1x * e2z;
            float nz = e1x * e2y - e1y * e2x;
            float invLen = 1.0f / MathF.Sqrt(nx * nx + ny * ny + nz * nz);
            nx *= invLen; ny *= invLen; nz *= invLen;

            triDataFloats[tb + 0] = e1x; triDataFloats[tb + 1] = e1y; triDataFloats[tb + 2] = e1z;
            triDataFloats[tb + 4] = e2x; triDataFloats[tb + 5] = e2y; triDataFloats[tb + 6] = e2z;
            triDataFloats[tb + 8] = nx;  triDataFloats[tb + 9] = ny;  triDataFloats[tb + 10] = nz;
        }
        byte[] triDataBytes = FloatsToBytes(triDataFloats);

        progress?.Invoke($"Uploading to GPU ({nodeBytes.Length / 1024.0f:F1} KB nodes, " +
            $"{primBytes.Length / 1024.0f:F1} KB indices, {vertBytes.Length / 1024.0f:F1} KB vertices, " +
            $"{triDataBytes.Length / 1024.0f:F1} KB triData)...");
        _vk.UploadBvhData(nodeBytes, primBytes, vertBytes, triDataBytes);
        _sceneLoaded = true;
    }

    /// <inheritdoc />
    /// <remarks>Always dispatches exactly 1 sample per pixel. The output is raw HDR
    /// (non-tonemapped) — the caller accumulates and tonemaps.</remarks>
    public float[] Render(Camera camera, RenderOptions options)
    {
        if (!_sceneLoaded)
            throw new InvalidOperationException("Scene not loaded. Call the constructor first.");

        uint frameSeed = _frameIndex++;

        // Compute camera basis vectors for shader push constants
        var w = Vector3.Normalize(camera.Pos - camera.LookAt);
        var uu = Vector3.Normalize(Vector3.Cross(camera.Up, w));
        var vv = Vector3.Cross(w, uu);

        float aspect = (float)_width / _height;
        float halfFovRad = MathF.PI * camera.Fov / 360.0f;
        float halfHeight = MathF.Tan(halfFovRad);
        float halfWidth = halfHeight * aspect;

        var sunDir = Vector3.Normalize(new Vector3(0.4f, 0.8f, 0.3f));

        var gpuParams = new VulkanContext.GpuParams
        {
            ImageWidth = (uint)_width,
            ImageHeight = (uint)_height,
            MaxBounces = (uint)options.MaxBounces,
            FrameSeed = frameSeed,
            CameraPosX = camera.Pos.X,
            CameraPosY = camera.Pos.Y,
            CameraPosZ = camera.Pos.Z,
            CameraUX = uu.X, CameraUY = uu.Y, CameraUZ = uu.Z,
            CameraVX = vv.X, CameraVY = vv.Y, CameraVZ = vv.Z,
            CameraWX = w.X, CameraWY = w.Y, CameraWZ = w.Z,
            HalfWidth = halfWidth,
            HalfHeight = halfHeight,
            SunDirX = sunDir.X, SunDirY = sunDir.Y, SunDirZ = sunDir.Z
        };

        _progress?.Invoke($"Dispatching {_width}x{_height} at 1 spp (frame {frameSeed})...");
        var gpuSw = Stopwatch.StartNew();
        _vk.Dispatch(gpuParams);
        _vk.WaitIdle();
        gpuSw.Stop();
        LastGpuTime = gpuSw.Elapsed;

        _progress?.Invoke("Reading back GPU results...");
        float[] rgbaFloat = _vk.ReadOutput();

        // Convert RGBA → RGB (drop alpha), shader now outputs raw HDR
        float[] rgbFloat = new float[_width * _height * 3];
        for (int i = 0; i < _width * _height; i++)
        {
            rgbFloat[i * 3 + 0] = rgbaFloat[i * 4 + 0];
            rgbFloat[i * 3 + 1] = rgbaFloat[i * 4 + 1];
            rgbFloat[i * 3 + 2] = rgbaFloat[i * 4 + 2];
        }

        long estimatedRays = (long)_width * _height * (1 + 2 * options.MaxBounces);
        double mrays = estimatedRays / (LastGpuTime.TotalSeconds * 1e6);
        _progress?.Invoke($"  GPU: Done | {LastGpuTime.TotalSeconds:F3}s | {mrays:F2} MRay/s");

        return rgbFloat;
    }

    private static byte[] FloatsToBytes(float[] floats)
    {
        byte[] bytes = new byte[floats.Length * sizeof(float)];
        System.Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static byte[] UintsToBytes(uint[] uints)
    {
        byte[] bytes = new byte[uints.Length * sizeof(uint)];
        System.Buffer.BlockCopy(uints, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public void Dispose()
    {
        _vk.Dispose();
    }
}
