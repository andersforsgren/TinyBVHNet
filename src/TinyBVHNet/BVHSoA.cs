using System;
using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's SoA (Structure of Arrays) BVH layout.
/// Optimized for SIMD ray traversal with better cache utilization.
/// </summary>
public class BVHSoA : NativeObject, IBVH
{
    /// <summary>Creates a new SoA BVH instance.</summary>
    public BVHSoA()
        : base(NativeMethods.TBVH_SoA_Create(), NativeMethods.TBVH_SoA_Destroy)
    {
    }

    /// <inheritdoc/>
    public unsafe void Build(ReadOnlySpan<float> vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices span too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        fixed (float* ptr = vertices)
            NativeMethods.TBVH_SoA_Build(Handle, ptr, triCount);
    }

    /// <summary>Convert from a standard BVH.</summary>
    public void ConvertFrom(BVH source)
    {
        NativeMethods.TBVH_SoA_ConvertFrom(Handle, source.Handle);
    }

    /// <inheritdoc/>
    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.Intersect(Handle, origin, direction, maxDistance, NativeMethods.TBVH_SoA_Intersect);
    }

    /// <inheritdoc/>
    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.IsOccluded(Handle, origin, direction, maxDistance, NativeMethods.TBVH_SoA_IsOccluded);
    }

    /// <inheritdoc/>
    public float SAHCost(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_SoA_SAHCost(Handle, nodeIdx);
    }

    /// <summary>Optimize the BVH tree structure.</summary>
    public void Optimize(uint iterations = 1, bool extreme = false)
    {
        NativeMethods.TBVH_SoA_Optimize(Handle, iterations, extreme ? 1 : 0);
    }
}
