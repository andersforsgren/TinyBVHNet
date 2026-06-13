using System;
using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's compressed wide BVH (CWBVH).
/// Uses 8-wide nodes with compression for GPU ray tracing.
/// </summary>
public class BVH8CWBVH : NativeObject, IBVH
{
    /// <summary>Creates a new compressed wide BVH instance.</summary>
    public BVH8CWBVH()
        : base(NativeMethods.TBVH_8CWBVH_Create(), NativeMethods.TBVH_8CWBVH_Destroy)
    {
    }

    /// <inheritdoc/>
    public unsafe void Build(ReadOnlySpan<float> vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices span too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        fixed (float* ptr = vertices)
            NativeMethods.TBVH_8CWBVH_Build(Handle, ptr, triCount);
    }

    /// <summary>High-quality build (slower, better tree).</summary>
    public unsafe void BuildHQ(ReadOnlySpan<float> vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices span too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        fixed (float* ptr = vertices)
            NativeMethods.TBVH_8CWBVH_BuildHQ(Handle, ptr, triCount);
    }

    /// <inheritdoc/>
    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.Intersect(Handle, origin, direction, maxDistance, NativeMethods.TBVH_8CWBVH_Intersect);
    }

    /// <inheritdoc/>
    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.IsOccluded(Handle, origin, direction, maxDistance, NativeMethods.TBVH_8CWBVH_IsOccluded);
    }

    /// <inheritdoc/>
    public float SAHCost(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_8CWBVH_SAHCost(Handle, nodeIdx);
    }

    /// <summary>Optimize the BVH tree structure.</summary>
    public void Optimize(uint iterations = 1, bool extreme = false)
    {
        NativeMethods.TBVH_8CWBVH_Optimize(Handle, iterations, extreme ? 1 : 0);
    }

    /// <summary>Save the BVH to a file.</summary>
    public void Save(string filename)
    {
        if (NativeMethods.TBVH_8CWBVH_Save(Handle, filename) == 0)
            throw new InvalidOperationException($"Failed to save BVH8_CWBVH to '{filename}'.");
    }

    /// <summary>Load a previously saved BVH.</summary>
    public unsafe void Load(string filename, ReadOnlySpan<float> vertices, uint triCount)
    {
        fixed (float* ptr = vertices)
        {
            if (NativeMethods.TBVH_8CWBVH_Load(Handle, filename, ptr, triCount) == 0)
                throw new InvalidOperationException($"Failed to load BVH8_CWBVH from '{filename}'.");
        }
    }
}
