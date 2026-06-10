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
    /// <summary>
    /// Build the acceleration structure from triangle vertices.
    /// Each triangle is 3 float4 vertices (x, y, z, w), so
    /// vertices.Length should equal triCount * 3 * 4.
    /// </summary>
    void Build(float[] vertices, uint triCount);

    /// <summary>
    /// Intersect a ray against the acceleration structure.
    /// Returns null on miss, or an <see cref="IntersectionResult"/> on hit.
    /// </summary>
    IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue);

    /// <summary>
    /// Shadow ray query -- returns true if the ray is occluded by any geometry
    /// within <paramref name="maxDistance"/>.
    /// </summary>
    bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue);

    /// <summary>
    /// Compute the Surface Area Heuristic cost (lower is better).
    /// </summary>
    float SAHCost(uint nodeIdx = 0);
}
