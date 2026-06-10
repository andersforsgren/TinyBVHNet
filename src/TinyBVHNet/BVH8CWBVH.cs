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
        NativeMethods.TBVH_8CWBVH_Build(Handle, vertices, triCount);
    }

    public void BuildHQ(float[] vertices, uint triCount)
    {
        NativeMethods.TBVH_8CWBVH_BuildHQ(Handle, vertices, triCount);
    }

    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        float t = maxDistance;

        int hit = NativeMethods.TBVH_8CWBVH_Intersect(Handle, (float*)&origin, (float*)&direction, ref t, out float u, out float v, out uint primIdx);
        if (hit == 0) return null;
        return new IntersectionResult { Distance = t, U = u, V = v, PrimitiveIndex = primIdx };
    }

    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return NativeMethods.TBVH_8CWBVH_IsOccluded(Handle, (float*)&origin, (float*)&direction, maxDistance) != 0;
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
