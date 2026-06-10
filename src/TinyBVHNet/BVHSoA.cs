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
        NativeMethods.TBVH_SoA_Build(Handle, vertices, triCount);
    }

    public void ConvertFrom(BVH source)
    {
        NativeMethods.TBVH_SoA_ConvertFrom(Handle, source.Handle);
    }

    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        float t = maxDistance;

        int hit = NativeMethods.TBVH_SoA_Intersect(Handle, (float*)&origin, (float*)&direction, ref t, out float u, out float v, out uint primIdx);
        if (hit == 0) return null;
        return new IntersectionResult { Distance = t, U = u, V = v, PrimitiveIndex = primIdx };
    }

    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return NativeMethods.TBVH_SoA_IsOccluded(Handle, (float*)&origin, (float*)&direction, maxDistance) != 0;
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
