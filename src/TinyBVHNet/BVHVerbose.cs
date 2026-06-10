using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's Verbose BVH -- a debugging/inspection variant
/// that stores detailed per-node statistics. Not intended for real-time ray tracing.
/// </summary>
public class BVHVerbose : NativeObject
{
    public BVHVerbose()
        : base(NativeMethods.TBVH_Verbose_Create(), NativeMethods.TBVH_Verbose_Destroy)
    {
    }

    public void ConvertFrom(BVH source)
    {
        NativeMethods.TBVH_Verbose_ConvertFrom(Handle, source.Handle);
    }

    /// <summary>Build from scratch using vertex data.</summary>
    public void Build(float[] vertices, uint triCount)
    {
        NativeMethods.TBVH_Verbose_Build(Handle, vertices, triCount);
    }

    public int NodeCount
    {
        get
        {
            return NativeMethods.TBVH_Verbose_NodeCount(Handle);
        }
    }

    public float SAHCost(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_Verbose_SAHCost(Handle, nodeIdx);
    }

    public void Optimize(uint iterations = 1, bool extreme = false, bool stochastic = false)
    {
        NativeMethods.TBVH_Verbose_Optimize(Handle, iterations, extreme ? 1 : 0, stochastic ? 1 : 0);
    }

    public void Refit(uint nodeIdx = 0)
    {
        NativeMethods.TBVH_Verbose_Refit(Handle, nodeIdx);
    }

    public void Compact()
    {
        NativeMethods.TBVH_Verbose_Compact(Handle);
    }
}
