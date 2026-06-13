using System;
using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Double-precision BVH interface. Same shape as <see cref="IBVH"/> but uses
/// <c>double</c> throughout for scenes requiring extreme precision (e.g.
/// planet-scale geometry). Not intended for real-time use.
/// </summary>
public interface IBVHDouble : INativeObject, IDisposable
{
    /// <summary>Build the double-precision BVH from triangle vertices.</summary>
    void Build(ReadOnlySpan<double> vertices, ulong primCount);

    /// <summary>Intersect a ray against the BVH.</summary>
    DoubleIntersectionResult? Intersect(Vector3 origin, Vector3 direction, double maxDistance = double.MaxValue);

    /// <summary>
    /// Intersect a ray against the acceleration structure with explicit
    /// double-precision origin and direction.
    /// </summary>
    DoubleIntersectionResult? Intersect(double originX, double originY, double originZ,
                                        double dirX, double dirY, double dirZ,
                                        double maxDistance = double.MaxValue);

    /// <summary>Returns true if the ray is occluded.</summary>
    bool IsOccluded(Vector3 origin, Vector3 direction, double maxDistance = double.MaxValue);

    /// <summary>
    /// Shadow ray query with explicit double-precision origin and direction.
    /// </summary>
    bool IsOccluded(double originX, double originY, double originZ,
                    double dirX, double dirY, double dirZ,
                    double maxDistance = double.MaxValue);

    /// <summary>Surface Area Heuristic cost (lower is better).</summary>
    double SAHCost(ulong nodeIdx = 0);
}
