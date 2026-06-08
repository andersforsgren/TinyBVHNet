using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's SoA (Structure of Arrays) BVH layout.
/// Optimized for SIMD ray traversal with better cache utilization.
/// </summary>
public class BVHSoA : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BVHSoA));
    }

    public BVHSoA()
    {
        _handle = NativeMethods.TBVH_SoA_Create();
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create native BVH_SoA instance.");
    }

    public void Build(float[] vertices, uint triCount)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_SoA_Build(_handle, vertices, triCount);
    }

    public void ConvertFrom(BVH source)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_SoA_ConvertFrom(_handle, source.Handle);
    }

    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        ThrowIfDisposed();
        float t = maxDistance;

        int hit = NativeMethods.TBVH_SoA_Intersect(_handle, (float*)&origin, (float*)&direction, ref t, out float u, out float v, out uint primIdx);
        if (hit == 0) return null;
        return new IntersectionResult { Distance = t, U = u, V = v, PrimitiveIndex = primIdx };
    }

    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_SoA_IsOccluded(_handle, (float*)&origin, (float*)&direction, maxDistance) != 0;
    }

    public float SAHCost(uint nodeIdx = 0)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_SoA_SAHCost(_handle, nodeIdx);
    }

    public void Optimize(uint iterations = 1, bool extreme = false)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_SoA_Optimize(_handle, iterations, extreme ? 1 : 0);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            NativeMethods.TBVH_SoA_Destroy(_handle);
            _handle = IntPtr.Zero;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
