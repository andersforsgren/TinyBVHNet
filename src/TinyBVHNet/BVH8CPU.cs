using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's 8-wide CPU BVH (AVX-256 optimized).
/// Traces 8 rays at once using SIMD for maximum throughput on AVX-capable CPUs.
/// </summary>
public class BVH8CPU : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BVH8CPU));
    }

    public BVH8CPU()
    {
        _handle = NativeMethods.TBVH_8CPU_Create();
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create native BVH8_CPU instance.");
    }

    public void Build(float[] vertices, uint triCount)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_8CPU_Build(_handle, vertices, triCount);
    }

    public void BuildHQ(float[] vertices, uint triCount)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_8CPU_BuildHQ(_handle, vertices, triCount);
    }

    public void ConvertFrom(BVH source)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_8CPU_ConvertFrom(_handle, source.Handle);
    }

    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        ThrowIfDisposed();
        float t = maxDistance;

        int hit = NativeMethods.TBVH_8CPU_Intersect(_handle, (float*)&origin, (float*)&direction, ref t, out float u, out float v, out uint primIdx);
        if (hit == 0) return null;
        return new IntersectionResult { Distance = t, U = u, V = v, PrimitiveIndex = primIdx };
    }

    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_8CPU_IsOccluded(_handle, (float*)&origin, (float*)&direction, maxDistance) != 0;
    }

    public float SAHCost(uint nodeIdx = 0)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_8CPU_SAHCost(_handle, nodeIdx);
    }

    public void Optimize(uint iterations = 1, bool extreme = false)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_8CPU_Optimize(_handle, iterations, extreme ? 1 : 0);
    }

    public void Refit(uint nodeIdx = 0)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_8CPU_Refit(_handle, nodeIdx);
    }

    public void Save(string filename)
    {
        ThrowIfDisposed();
        if (NativeMethods.TBVH_8CPU_Save(_handle, filename) == 0)
            throw new InvalidOperationException($"Failed to save BVH8_CPU to '{filename}'.");
    }

    public void Load(string filename, float[] vertices, uint triCount)
    {
        ThrowIfDisposed();
        if (NativeMethods.TBVH_8CPU_Load(_handle, filename, vertices, triCount) == 0)
            throw new InvalidOperationException($"Failed to load BVH8_CPU from '{filename}'.");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            NativeMethods.TBVH_8CPU_Destroy(_handle);
            _handle = IntPtr.Zero;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
