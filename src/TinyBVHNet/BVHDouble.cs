using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Double-precision BVH (LAYOUT_BVH_DOUBLE, 64-byte nodes).
/// Uses 64-bit floating point throughout for scenes requiring
/// extremely high precision. Not intended for real-time use.
/// </summary>
public class BVHDouble : NativeObject, IBVHDouble
{
    /// <summary>Creates a new double-precision BVH instance.</summary>
    public BVHDouble()
        : base(NativeMethods.TBVH_Double_Create(), NativeMethods.TBVH_Double_Destroy)
    {
    }

    /// <inheritdoc/>
    public unsafe void Build(ReadOnlySpan<double> vertices, ulong primCount)
    {
        if ((ulong)vertices.Length < primCount * 9)
            throw new ArgumentException($"Vertices span too small. Expected at least {primCount * 9}, got {vertices.Length}.", nameof(vertices));
        fixed (double* ptr = vertices)
            NativeMethods.TBVH_Double_Build(Handle, ptr, primCount);
    }

    /// <inheritdoc/>
    public unsafe DoubleIntersectionResult? Intersect(Vector3 origin, Vector3 direction, double maxDistance = double.MaxValue)
    {
        return Intersect(origin.X, origin.Y, origin.Z,
                         direction.X, direction.Y, direction.Z, maxDistance);
    }

    /// <inheritdoc/>
    public unsafe DoubleIntersectionResult? Intersect(double originX, double originY, double originZ,
                                                       double dirX, double dirY, double dirZ,
                                                       double maxDistance = double.MaxValue)
    {
        double* originPtr = stackalloc double[3] { originX, originY, originZ };
        double* dirPtr = stackalloc double[3] { dirX, dirY, dirZ };
        double t = maxDistance;

        int hit = NativeMethods.TBVH_Double_Intersect(Handle, originPtr, dirPtr,
            ref t, out double u, out double v, out ulong primIdx);

        if (hit == 0) return null;
        return new DoubleIntersectionResult { Distance = t, U = u, V = v, PrimitiveIndex = primIdx };
    }

    /// <inheritdoc/>
    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, double maxDistance = double.MaxValue)
    {
        return IsOccluded(origin.X, origin.Y, origin.Z,
                          direction.X, direction.Y, direction.Z, maxDistance);
    }

    /// <inheritdoc/>
    public unsafe bool IsOccluded(double originX, double originY, double originZ,
                                   double dirX, double dirY, double dirZ,
                                   double maxDistance = double.MaxValue)
    {
        double* originPtr = stackalloc double[3] { originX, originY, originZ };
        double* dirPtr = stackalloc double[3] { dirX, dirY, dirZ };
        return NativeMethods.TBVH_Double_IsOccluded(Handle, originPtr, dirPtr, maxDistance) != 0;
    }

    /// <inheritdoc/>
    public double SAHCost(ulong nodeIdx = 0)
    {
        return NativeMethods.TBVH_Double_SAHCost(Handle, nodeIdx);
    }
}

/// <summary>
/// Result of a double-precision ray-BVH intersection query.
/// </summary>
public readonly struct DoubleIntersectionResult
{
    /// <summary>Distance along the ray to the intersection point (double precision).</summary>
    public double Distance { get; init; }

    /// <summary>Barycentric coordinate U at the hit point.</summary>
    public double U { get; init; }

    /// <summary>Barycentric coordinate V at the hit point.</summary>
    public double V { get; init; }

    /// <summary>Index of the hit primitive (64-bit).</summary>
    public ulong PrimitiveIndex { get; init; }
}
