using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's compressed wide BVH (CWBVH).
/// Uses 8-wide nodes with compression for GPU ray tracing.
/// </summary>
public class BVH8CWBVH : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BVH8CWBVH));
    }

    public BVH8CWBVH()
    {
        _handle = NativeMethods.TBVH_8CWBVH_Create();
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create native BVH8_CWBVH instance.");
    }

    public void Build(float[] vertices, uint triCount)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_8CWBVH_Build(_handle, vertices, triCount);
    }

    public void BuildHQ(float[] vertices, uint triCount)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_8CWBVH_BuildHQ(_handle, vertices, triCount);
    }

    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        ThrowIfDisposed();
        float t = maxDistance;

        int hit = NativeMethods.TBVH_8CWBVH_Intersect(_handle, (float*)&origin, (float*)&direction, ref t, out float u, out float v, out uint primIdx);
        if (hit == 0) return null;
        return new IntersectionResult { Distance = t, U = u, V = v, PrimitiveIndex = primIdx };
    }

    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_8CWBVH_IsOccluded(_handle, (float*)&origin, (float*)&direction, maxDistance) != 0;
    }

    public float SAHCost(uint nodeIdx = 0)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_8CWBVH_SAHCost(_handle, nodeIdx);
    }

    public void Optimize(uint iterations = 1, bool extreme = false)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_8CWBVH_Optimize(_handle, iterations, extreme ? 1 : 0);
    }

    public void Save(string filename)
    {
        ThrowIfDisposed();
        if (NativeMethods.TBVH_8CWBVH_Save(_handle, filename) == 0)
            throw new InvalidOperationException($"Failed to save BVH8_CWBVH to '{filename}'.");
    }

    public void Load(string filename, float[] vertices, uint triCount)
    {
        ThrowIfDisposed();
        if (NativeMethods.TBVH_8CWBVH_Load(_handle, filename, vertices, triCount) == 0)
            throw new InvalidOperationException($"Failed to load BVH8_CWBVH from '{filename}'.");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            NativeMethods.TBVH_8CWBVH_Destroy(_handle);
            _handle = IntPtr.Zero;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
