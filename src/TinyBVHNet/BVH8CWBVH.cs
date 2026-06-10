using System;
using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's compressed wide BVH (CWBVH).
/// Uses 8-wide nodes with compression for GPU ray tracing.
/// </summary>
public class BVH8CWBVH : NativeObject, IBVH
{
    public BVH8CWBVH()
        : base(NativeMethods.TBVH_8CWBVH_Create(), NativeMethods.TBVH_8CWBVH_Destroy)
    {
    }

    public void Build(float[] vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices array too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        NativeMethods.TBVH_8CWBVH_Build(Handle, vertices, triCount);
    }

    public void BuildHQ(float[] vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices array too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        NativeMethods.TBVH_8CWBVH_BuildHQ(Handle, vertices, triCount);
    }

    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.Intersect(Handle, origin, direction, maxDistance, NativeMethods.TBVH_8CWBVH_Intersect);
    }

    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.IsOccluded(Handle, origin, direction, maxDistance, NativeMethods.TBVH_8CWBVH_IsOccluded);
    }

    public float SAHCost(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_8CWBVH_SAHCost(Handle, nodeIdx);
    }

    public void Optimize(uint iterations = 1, bool extreme = false)
    {
        NativeMethods.TBVH_8CWBVH_Optimize(Handle, iterations, extreme ? 1 : 0);
    }

    public void Save(string filename)
    {
        if (NativeMethods.TBVH_8CWBVH_Save(Handle, filename) == 0)
            throw new InvalidOperationException($"Failed to save BVH8_CWBVH to '{filename}'.");
    }

    public void Load(string filename, float[] vertices, uint triCount)
    {
        if (NativeMethods.TBVH_8CWBVH_Load(Handle, filename, vertices, triCount) == 0)
            throw new InvalidOperationException($"Failed to load BVH8_CWBVH from '{filename}'.");
    }
}
