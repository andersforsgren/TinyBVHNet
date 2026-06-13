using System;
using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's Verbose BVH -- a debugging/inspection variant
/// that stores detailed per-node statistics. Not intended for real-time ray tracing.
/// </summary>
public class BVHVerbose : NativeObject
{
    /// <summary>Creates a new verbose BVH instance for debugging.</summary>
    public BVHVerbose()
        : base(NativeMethods.TBVH_Verbose_Create(), NativeMethods.TBVH_Verbose_Destroy)
    {
    }

    /// <summary>Convert from a standard BVH.</summary>
    public void ConvertFrom(BVH source)
    {
        NativeMethods.TBVH_Verbose_ConvertFrom(Handle, source.Handle);
    }

    /// <summary>Build from triangle vertex data.</summary>
    public unsafe void Build(ReadOnlySpan<float> vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices span too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        fixed (float* ptr = vertices)
            NativeMethods.TBVH_Verbose_Build(Handle, ptr, triCount);
    }

    /// <summary>Total number of nodes.</summary>
    public int NodeCount
    {
        get
        {
            return NativeMethods.TBVH_Verbose_NodeCount(Handle);
        }
    }

    /// <summary>Surface Area Heuristic cost.</summary>
    public float SAHCost(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_Verbose_SAHCost(Handle, nodeIdx);
    }

    /// <summary>Optimize the BVH tree structure.</summary>
    public void Optimize(uint iterations = 1, bool extreme = false, bool stochastic = false)
    {
        NativeMethods.TBVH_Verbose_Optimize(Handle, iterations, extreme ? 1 : 0, stochastic ? 1 : 0);
    }

    /// <summary>Refit the BVH after vertex changes.</summary>
    public void Refit(uint nodeIdx = 0)
    {
        NativeMethods.TBVH_Verbose_Refit(Handle, nodeIdx);
    }

    /// <summary>Compact the BVH to shrink memory.</summary>
    public void Compact()
    {
        NativeMethods.TBVH_Verbose_Compact(Handle);
    }
}
