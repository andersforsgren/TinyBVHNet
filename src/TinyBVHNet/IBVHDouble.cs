using System;
using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Double-precision BVH interface. Same shape as <see cref="IBVH"/> but uses
/// <c>double</c> throughout for scenes requiring extreme precision (e.g.
/// planet-scale geometry). Not intended for real-time use.
/// </summary>
public interface IBVHDouble : IDisposable
{
    /// <summary>
    /// Build the acceleration structure from triangle vertices in double precision.
    /// Each vertex is 3 doubles (x, y, z), each triangle is 3 vertices.
    /// vertices.Length should equal primCount * 9.
    /// </summary>
    void Build(double[] vertices, ulong primCount);

    /// <summary>
    /// Intersect a ray against the acceleration structure using double-precision
    /// coordinates. Returns null on miss, or a <see cref="DoubleIntersectionResult"/> on hit.
    /// </summary>
    DoubleIntersectionResult? Intersect(Vector3 origin, Vector3 direction, double maxDistance = double.MaxValue);

    /// <summary>
    /// Intersect a ray against the acceleration structure with explicit
    /// double-precision origin and direction.
    /// </summary>
    DoubleIntersectionResult? Intersect(double originX, double originY, double originZ,
                                        double dirX, double dirY, double dirZ,
                                        double maxDistance = double.MaxValue);

    /// <summary>
    /// Shadow ray query -- returns true if the ray is occluded by any geometry
    /// within <paramref name="maxDistance"/>.
    /// </summary>
    bool IsOccluded(Vector3 origin, Vector3 direction, double maxDistance = double.MaxValue);

    /// <summary>
    /// Shadow ray query with explicit double-precision origin and direction.
    /// </summary>
    bool IsOccluded(double originX, double originY, double originZ,
                    double dirX, double dirY, double dirZ,
                    double maxDistance = double.MaxValue);

    /// <summary>
    /// Compute the Surface Area Heuristic cost (lower is better).
    /// </summary>
    double SAHCost(ulong nodeIdx = 0);
}
