using System;
using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's 8-wide CPU BVH (AVX-256 optimized).
/// Traces 8 rays at once using SIMD for maximum throughput on AVX-capable CPUs.
/// </summary>
public class BVH8CPU : NativeObject, IBVH
{
    public BVH8CPU()
        : base(NativeMethods.TBVH_8CPU_Create(), NativeMethods.TBVH_8CPU_Destroy)
    {
    }

    public void Build(float[] vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices array too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        NativeMethods.TBVH_8CPU_Build(Handle, vertices, triCount);
    }

    public void BuildHQ(float[] vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices array too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        NativeMethods.TBVH_8CPU_BuildHQ(Handle, vertices, triCount);
    }

    public void ConvertFrom(BVH source)
    {
        NativeMethods.TBVH_8CPU_ConvertFrom(Handle, source.Handle);
    }

    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.Intersect(Handle, origin, direction, maxDistance, NativeMethods.TBVH_8CPU_Intersect);
    }

    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.IsOccluded(Handle, origin, direction, maxDistance, NativeMethods.TBVH_8CPU_IsOccluded);
    }

    public float SAHCost(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_8CPU_SAHCost(Handle, nodeIdx);
    }

    public void Optimize(uint iterations = 1, bool extreme = false)
    {
        NativeMethods.TBVH_8CPU_Optimize(Handle, iterations, extreme ? 1 : 0);
    }

    public void Refit(uint nodeIdx = 0)
    {
        NativeMethods.TBVH_8CPU_Refit(Handle, nodeIdx);
    }

    public void Save(string filename)
    {
        if (NativeMethods.TBVH_8CPU_Save(Handle, filename) == 0)
            throw new InvalidOperationException($"Failed to save BVH8_CPU to '{filename}'.");
    }

    public void Load(string filename, float[] vertices, uint triCount)
    {
        if (NativeMethods.TBVH_8CPU_Load(Handle, filename, vertices, triCount) == 0)
            throw new InvalidOperationException($"Failed to load BVH8_CPU from '{filename}'.");
    }
}
