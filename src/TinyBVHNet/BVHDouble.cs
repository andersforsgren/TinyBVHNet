using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Double-precision BVH (LAYOUT_BVH_DOUBLE, 64-byte nodes).
/// Uses 64-bit floating point throughout for scenes requiring
/// extremely high precision. Not intended for real-time use.
/// </summary>
public class BVHDouble : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BVHDouble));
    }

    public BVHDouble()
    {
        _handle = NativeMethods.TBVH_Double_Create();
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create native BVH_Double instance.");
    }

    /// <summary>
    /// Build the double-precision BVH from vertex data.
    /// Each vertex is 3 doubles (x, y, z), each triangle is 3 vertices.
    /// So vertices.Length should equal primCount * 3 * 3.
    /// </summary>
    public void Build(double[] vertices, ulong primCount)
    {
        ThrowIfDisposed();
        if ((ulong)vertices.Length < primCount * 9)
            throw new ArgumentException($"Vertices array too small. Expected at least {primCount * 9}, got {vertices.Length}.", nameof(vertices));
        NativeMethods.TBVH_Double_Build(_handle, vertices, primCount);
    }

    /// <summary>
    /// Intersect a double-precision ray against the BVH.
    /// </summary>
    public unsafe DoubleIntersectionResult? Intersect(Vector3 origin, Vector3 direction, double maxDistance = double.MaxValue)
    {
        ThrowIfDisposed();
        double* originPtr = stackalloc double[3] { origin.X, origin.Y, origin.Z };
        double* dirPtr = stackalloc double[3] { direction.X, direction.Y, direction.Z };
        double t = maxDistance;

        int hit = NativeMethods.TBVH_Double_Intersect(_handle, originPtr, dirPtr,
            ref t, out double u, out double v, out ulong primIdx);

        if (hit == 0) return null;
        return new DoubleIntersectionResult { Distance = t, U = u, V = v, PrimitiveIndex = primIdx };
    }

    /// <summary>
    /// Shadow ray query — returns true if the ray to maxDistance is occluded.
    /// </summary>
    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, double maxDistance = double.MaxValue)
    {
        ThrowIfDisposed();
        double* originPtr = stackalloc double[3] { origin.X, origin.Y, origin.Z };
        double* dirPtr = stackalloc double[3] { direction.X, direction.Y, direction.Z };
        return NativeMethods.TBVH_Double_IsOccluded(_handle, originPtr, dirPtr, maxDistance) != 0;
    }

    /// <summary>
    /// Compute the Surface Area Heuristic cost (lower is better).
    /// </summary>
    public double SAHCost(ulong nodeIdx = 0)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_Double_SAHCost(_handle, nodeIdx);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            NativeMethods.TBVH_Double_Destroy(_handle);
            _handle = IntPtr.Zero;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
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
