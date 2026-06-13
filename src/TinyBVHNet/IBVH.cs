using System;
using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Core interface for a ray-traceable float-valued BVH acceleration structure.
/// Implemented by BVH, BVH4CPU, BVH8CPU, BVH8CWBVH, BVH4GPU, BVHGPU, and BVHSoA.
/// BVHVerbose and BVHVoxelSet do NOT implement this -- they have different APIs.
/// BVHDouble uses <see cref="IBVHDouble"/> instead.
/// </summary>
public interface IBVH : INativeObject, IDisposable
{
    /// <summary>Build the BVH from triangle vertices.</summary>
    void Build(ReadOnlySpan<float> vertices, uint triCount);

    /// <summary>Intersect a ray against the BVH.</summary>
    IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue);

    /// <summary>Returns true if the ray is occluded.</summary>
    bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue);

    /// <summary>Surface Area Heuristic cost (lower is better).</summary>
    float SAHCost(uint nodeIdx = 0);
}
