using System;
using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's SoA (Structure of Arrays) BVH layout.
/// Optimized for SIMD ray traversal with better cache utilization.
/// </summary>
public class BVHSoA : NativeObject, IBVH
{
    public BVHSoA()
        : base(NativeMethods.TBVH_SoA_Create(), NativeMethods.TBVH_SoA_Destroy)
    {
    }

    public void Build(float[] vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices array too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        NativeMethods.TBVH_SoA_Build(Handle, vertices, triCount);
    }

    public void ConvertFrom(BVH source)
    {
        NativeMethods.TBVH_SoA_ConvertFrom(Handle, source.Handle);
    }

    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.Intersect(Handle, origin, direction, maxDistance, NativeMethods.TBVH_SoA_Intersect);
    }

    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.IsOccluded(Handle, origin, direction, maxDistance, NativeMethods.TBVH_SoA_IsOccluded);
    }

    public float SAHCost(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_SoA_SAHCost(Handle, nodeIdx);
    }

    public void Optimize(uint iterations = 1, bool extreme = false)
    {
        NativeMethods.TBVH_SoA_Optimize(Handle, iterations, extreme ? 1 : 0);
    }
}
