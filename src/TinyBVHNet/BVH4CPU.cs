using System;
using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's 4-wide CPU BVH (SSE-optimized).
/// Traces 4 rays at once using SIMD for improved throughput.
/// </summary>
public class BVH4CPU : NativeObject, IBVH
{
    public BVH4CPU()
        : base(NativeMethods.TBVH_4CPU_Create(), NativeMethods.TBVH_4CPU_Destroy)
    {
    }

    public void Build(float[] vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices array too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        NativeMethods.TBVH_4CPU_Build(Handle, vertices, triCount);
    }

    public void BuildHQ(float[] vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices array too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        NativeMethods.TBVH_4CPU_BuildHQ(Handle, vertices, triCount);
    }

    public void ConvertFrom(BVH source)
    {
        NativeMethods.TBVH_4CPU_ConvertFrom(Handle, source.Handle);
    }

    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.Intersect(Handle, origin, direction, maxDistance, NativeMethods.TBVH_4CPU_Intersect);
    }

    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.IsOccluded(Handle, origin, direction, maxDistance, NativeMethods.TBVH_4CPU_IsOccluded);
    }

    public float SAHCost(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_4CPU_SAHCost(Handle, nodeIdx);
    }

    public void Optimize(uint iterations = 1, bool extreme = false)
    {
        NativeMethods.TBVH_4CPU_Optimize(Handle, iterations, extreme ? 1 : 0);
    }

    public void Refit(uint nodeIdx = 0)
    {
        NativeMethods.TBVH_4CPU_Refit(Handle, nodeIdx);
    }

    public void Save(string filename)
    {
        if (NativeMethods.TBVH_4CPU_Save(Handle, filename) == 0)
            throw new InvalidOperationException($"Failed to save BVH4_CPU to '{filename}'.");
    }

    public void Load(string filename, float[] vertices, uint triCount)
    {
        if (NativeMethods.TBVH_4CPU_Load(Handle, filename, vertices, triCount) == 0)
            throw new InvalidOperationException($"Failed to load BVH4_CPU from '{filename}'.");
    }
}
