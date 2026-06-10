using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around the TinyBVH VoxelSet (grid-based acceleration structure).
/// Fixed-size 256^3 voxel grid useful for scenes where a uniform spatial grid
/// is preferable to a BVH.
/// </summary>
public class BVHVoxelSet : NativeObject
{
    public BVHVoxelSet()
        : base(NativeMethods.TBVH_VoxelSet_Create(), NativeMethods.TBVH_VoxelSet_Destroy)
    {
    }

    /// <summary>Set a voxel value at grid coordinates.</summary>
    public void Set(uint x, uint y, uint z, uint v)
    {
        NativeMethods.TBVH_VoxelSet_Set(Handle, x, y, z, v);
    }

    /// <summary>Update the top-level acceleration grid from populated voxels.</summary>
    public void UpdateTopGrid()
    {
        NativeMethods.TBVH_VoxelSet_UpdateTopGrid(Handle);
    }

    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        float t = maxDistance;

        int hit = NativeMethods.TBVH_VoxelSet_Intersect(Handle, (float*)&origin, (float*)&direction, ref t, out float u, out float v, out uint primIdx);
        if (hit == 0) return null;
        return new IntersectionResult { Distance = t, U = u, V = v, PrimitiveIndex = primIdx };
    }

    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return NativeMethods.TBVH_VoxelSet_IsOccluded(Handle, (float*)&origin, (float*)&direction, maxDistance) != 0;
    }
}
