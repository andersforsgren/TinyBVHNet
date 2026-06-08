using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around the TinyBVH VoxelSet (grid-based acceleration structure).
/// Fixed-size 256³ voxel grid useful for scenes where a uniform spatial grid
/// is preferable to a BVH.
/// </summary>
public class BVHVoxelSet : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BVHVoxelSet));
    }

    public BVHVoxelSet()
    {
        _handle = NativeMethods.TBVH_VoxelSet_Create();
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create native VoxelSet instance.");
    }

    /// <summary>Set a voxel value at grid coordinates.</summary>
    public void Set(uint x, uint y, uint z, uint v)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_VoxelSet_Set(_handle, x, y, z, v);
    }

    /// <summary>Update the top-level acceleration grid from populated voxels.</summary>
    public void UpdateTopGrid()
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_VoxelSet_UpdateTopGrid(_handle);
    }

    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        ThrowIfDisposed();
        float t = maxDistance;

        int hit = NativeMethods.TBVH_VoxelSet_Intersect(_handle, (float*)&origin, (float*)&direction, ref t, out float u, out float v, out uint primIdx);
        if (hit == 0) return null;
        return new IntersectionResult { Distance = t, U = u, V = v, PrimitiveIndex = primIdx };
    }

    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_VoxelSet_IsOccluded(_handle, (float*)&origin, (float*)&direction, maxDistance) != 0;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            NativeMethods.TBVH_VoxelSet_Destroy(_handle);
            _handle = IntPtr.Zero;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
