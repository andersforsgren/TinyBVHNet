using System;
using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's 4-wide CPU BVH (SSE-optimized).
/// Traces 4 rays at once using SIMD for improved throughput.
/// </summary>
public class BVH4CPU : NativeObject, IBVH
{
    /// <summary>Creates a new 4-wide CPU BVH instance.</summary>
    public BVH4CPU()
        : base(NativeMethods.TBVH_4CPU_Create(), NativeMethods.TBVH_4CPU_Destroy)
    {
    }

    /// <inheritdoc/>
    public unsafe void Build(ReadOnlySpan<float> vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices span too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        fixed (float* ptr = vertices)
            NativeMethods.TBVH_4CPU_Build(Handle, ptr, triCount);
    }

    /// <summary>High-quality build (slower, better tree).</summary>
    public unsafe void BuildHQ(ReadOnlySpan<float> vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices span too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        fixed (float* ptr = vertices)
            NativeMethods.TBVH_4CPU_BuildHQ(Handle, ptr, triCount);
    }

    /// <summary>Convert from a standard BVH.</summary>
    public void ConvertFrom(BVH source)
    {
        NativeMethods.TBVH_4CPU_ConvertFrom(Handle, source.Handle);
    }

    /// <inheritdoc/>
    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.Intersect(Handle, origin, direction, maxDistance, NativeMethods.TBVH_4CPU_Intersect);
    }

    /// <inheritdoc/>
    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.IsOccluded(Handle, origin, direction, maxDistance, NativeMethods.TBVH_4CPU_IsOccluded);
    }

    /// <inheritdoc/>
    public float SAHCost(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_4CPU_SAHCost(Handle, nodeIdx);
    }

    /// <summary>Optimize the BVH tree structure.</summary>
    public void Optimize(uint iterations = 1, bool extreme = false)
    {
        NativeMethods.TBVH_4CPU_Optimize(Handle, iterations, extreme ? 1 : 0);
    }

    /// <summary>Refit the BVH after vertex changes.</summary>
    public void Refit(uint nodeIdx = 0)
    {
        NativeMethods.TBVH_4CPU_Refit(Handle, nodeIdx);
    }

    /// <summary>Save the BVH to a file.</summary>
    public void Save(string filename)
    {
        if (NativeMethods.TBVH_4CPU_Save(Handle, filename) == 0)
            throw new InvalidOperationException($"Failed to save BVH4_CPU to '{filename}'.");
    }

    /// <summary>Load a previously saved BVH.</summary>
    public unsafe void Load(string filename, ReadOnlySpan<float> vertices, uint triCount)
    {
        fixed (float* ptr = vertices)
        {
            if (NativeMethods.TBVH_4CPU_Load(Handle, filename, ptr, triCount) == 0)
                throw new InvalidOperationException($"Failed to load BVH4_CPU from '{filename}'.");
        }
    }
}
