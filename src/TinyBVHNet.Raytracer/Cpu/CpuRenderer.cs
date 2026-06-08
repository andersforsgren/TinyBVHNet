using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using TinyBVHNet;

namespace TinyBVHNet.Raytracer.Cpu;

/// <summary>
/// Monochrome multi-bounce CPU path tracer using TinyBVHNet BVH for intersection.
/// The scene BVH is built once in the constructor; <see cref="Raytracer.IRenderer.Render"/>
/// can be called repeatedly with different cameras without rebuilding.
/// </summary>
public sealed class CpuRenderer : IRenderer, IDisposable
{
    private readonly BVH _bvh;
    private readonly Vector3[][] _triangles;
    private readonly int _imageWidth, _imageHeight;

    // Directional sun
    private static readonly Vector3 SunDir = Vector3.Normalize(new Vector3(0.4f, 0.8f, 0.3f));
    private const float SunIntensity = 3.0f;
    private const float SkyIntensity = 1.0f;
    private const float Epsilon = 1e-4f;
    private const float Albedo = 0.7f;
    private const int RrBounce = 3;
    private const int Seed = 12345;

    private long _totalRays;
    private int _frameIndex;

    /// <summary>Total rays cast during the last render.</summary>
    public long TotalRays => _totalRays;

    /// <summary>
    /// Create a CPU renderer and build the BVH for the given scene.
    /// </summary>
    public CpuRenderer(ObjParser.Scene scene, int imageWidth, int imageHeight)
    {
        _triangles = scene.Triangles;
        _imageWidth = imageWidth;
        _imageHeight = imageHeight;
        _bvh = new BVH();
        _bvh.Build(scene.Vertices, scene.TriangleCount);
    }

    public void Dispose() => _bvh?.Dispose();

    /// <inheritdoc />
    /// <inheritdoc />
    /// <remarks>Always produces exactly 1 sample per pixel of raw HDR luminance.
    /// Call repeatedly and accumulate for progressive refinement.</remarks>
    public float[] Render(Camera camera, RenderOptions options)
    {
        int frameSeed = Interlocked.Increment(ref _frameIndex);
        int w = _imageWidth, h = _imageHeight;
        float[] hdr = new float[w * h];
        var sw = Stopwatch.StartNew();
        _totalRays = 0;

        // Precompute camera basis
        var cw = Vector3.Normalize(camera.Pos - camera.LookAt);
        var cu = Vector3.Normalize(Vector3.Cross(camera.Up, cw));
        var cv = Vector3.Cross(cw, cu);
        float aspect = (float)w / h;
        float halfFovRad = MathF.PI * camera.Fov / 360f;
        float halfH = MathF.Tan(halfFovRad);
        float halfW = halfH * aspect;

        int completedPixels = 0;
        object lockObj = new();

        Parallel.For(0, w * h, idx =>
        {
            int x = idx % w, y = idx / w;
            var localRng = new Random(Seed + idx * 3 + frameSeed * 71993);

            float jx = (float)localRng.NextDouble();
            float jy = (float)localRng.NextDouble();

            float sx = ((x + jx) / w * 2f - 1f);
            float sy = (1f - (y + jy) / h * 2f);
            var dir = Vector3.Normalize(-cw + sx * halfW * cu + sy * halfH * cv);
            var ray = new Ray(camera.Pos, dir);

            hdr[idx] = PathTrace(ray, options.MaxBounces, localRng);

            lock (lockObj)
            {
                completedPixels++;
                if (completedPixels % Math.Max(1, (w * h) / 20) == 0 || completedPixels == w * h)
                {
                    double progress = (double)completedPixels / (w * h) * 100;
                    double elapsed = sw.Elapsed.TotalSeconds;
                    double mrays = _totalRays / (elapsed * 1e6);
                    Console.Write($"\r  CPU: {progress:F0}% | {elapsed:F1}s | {mrays:F2} MRay/s    ");
                }
            }
        });

        sw.Stop();
        double totalSec = sw.Elapsed.TotalSeconds;
        Console.WriteLine($"\r  CPU: Done | {totalSec:F1}s | {_totalRays / (totalSec * 1e6):F2} MRay/s    ");

        return hdr;
    }

    private float PathTrace(Ray ray, int maxBounces, Random rng)
    {
        float throughput = 1f;
        float radiance = 0f;

        for (int bounce = 0; bounce <= maxBounces; bounce++)
        {
            if (!IsFinite(ray.Origin) || !IsFinite(ray.Direction) || ray.Direction.LengthSquared() < 1e-12f)
                break;

            var hit = _bvh.Intersect(ray.Origin, ray.Direction, float.MaxValue);
            Interlocked.Increment(ref _totalRays);

            if (hit is null)
            {
                radiance += throughput * SkyRadiance(ray.Direction);
                break;
            }

            float t = hit.Value.Distance;
            var point = ray.Origin + ray.Direction * t;
            var normal = ComputeNormal(hit.Value.PrimitiveIndex);
            if (Vector3.Dot(normal, ray.Direction) > 0)
                normal = -normal;

            float sunVis = ShadowRay(point + normal * Epsilon, SunDir);
            float nDotSun = Math.Max(0, Vector3.Dot(normal, SunDir));
            radiance += throughput * Albedo * SunIntensity * nDotSun * sunVis;

            Vector3 wi = SampleCosineHemisphere(normal, rng);
            throughput *= Albedo;
            ray = new Ray(point + normal * Epsilon, wi);

            if (bounce >= RrBounce)
            {
                float p = Math.Min(throughput, 0.7f);
                if ((float)rng.NextDouble() > p) break;
                throughput /= p;
            }
        }

        return radiance;
    }

    private static float SkyRadiance(Vector3 dir)
    {
        float y = Math.Clamp(dir.Y, 0, 1);
        float sky = SkyIntensity * (0.3f + 0.7f * y);
        float cosTheta = Vector3.Dot(dir, SunDir);
        float sunDisk = cosTheta > 0.9999f ? SunIntensity * 100f : 0f;
        return sky + sunDisk;
    }

    private float ShadowRay(Vector3 origin, Vector3 direction)
    {
        if (!IsFinite(origin) || !IsFinite(direction)) 
            return 0f;
        bool hit = _bvh.IsOccluded(origin, direction);
        Interlocked.Increment(ref _totalRays);
        return hit ? 0f : 1f;
    }

    private static Vector3 SampleCosineHemisphere(Vector3 normal, Random rng)
    {
        float r1 = (float)rng.NextDouble();
        float r2 = (float)rng.NextDouble();
        float phi = 2f * MathF.PI * r1;
        float r = MathF.Sqrt(r2);

        Vector3 u, v;
        if (Math.Abs(normal.X) > 0.9f)
        {
            u = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, normal));
            v = Vector3.Cross(normal, u);
        }
        else
        {
            u = Vector3.Normalize(Vector3.Cross(Vector3.UnitZ, normal));
            v = Vector3.Cross(normal, u);
        }

        float x = r * MathF.Cos(phi);
        float y = MathF.Sqrt(1f - r2);
        float z = r * MathF.Sin(phi);
        return Vector3.Normalize(x * u + y * normal + z * v);
    }

    private Vector3 ComputeNormal(uint triIndex)
    {
        var tri = _triangles[triIndex];
        return Vector3.Normalize(Vector3.Cross(tri[1] - tri[0], tri[2] - tri[0]));
    }

    private static bool IsFinite(Vector3 v) =>
        float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}

/// <summary>Internal ray helper used by CPU path tracer.</summary>
internal readonly struct Ray(Vector3 origin, Vector3 direction)
{
    public Vector3 Origin { get; } = origin;
    public Vector3 Direction { get; } = direction;
}
