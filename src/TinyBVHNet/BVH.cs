using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around the TinyBVH native library.
/// Provides a safe, object-oriented API for building and querying BVH structures.
/// </summary>
public class BVH : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    /// <summary>Internal access to native handle for cross-type operations.</summary>
    internal IntPtr Handle => _handle;

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BVH));
    }

    /// <summary>
    /// Creates a new BVH instance. Call <see cref="Build"/> to construct the hierarchy.
    /// </summary>
    public BVH()
    {
        _handle = NativeMethods.TBVH_Create();
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create native BVH instance.");
    }

    /// <summary>
    /// Build the BVH from an array of triangle vertices.
    /// Each vertex is a float4 (X, Y, Z, W), and each triangle has 3 vertices.
    /// So vertices.Length should equal triCount * 3 * 4.
    /// </summary>
    public void Build(float[] vertices, uint triCount)
    {
        ThrowIfDisposed();
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices array too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        NativeMethods.TBVH_Build(_handle, vertices, triCount);
    }

    /// <summary>
    /// High-quality build (slower, better tree). Same vertex format as <see cref="Build"/>.
    /// </summary>
    public void BuildHQ(float[] vertices, uint triCount)
    {
        ThrowIfDisposed();
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices array too small.", nameof(vertices));
        NativeMethods.TBVH_BuildHQ(_handle, vertices, triCount);
    }

    /// <summary>
    /// Build from indexed triangle data. vertices is float4-per-vertex,
    /// indices references 3 indices per triangle.
    /// </summary>
    public void BuildIndexed(float[] vertices, uint[] indices, uint triCount)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_BuildIndexed(_handle, vertices, indices, triCount);
    }

    /// <summary>
    /// Build from precomputed AABBs (6 floats per primitive: minX, minY, minZ, maxX, maxY, maxZ).
    /// </summary>
    public void BuildAABB(float[] aabbs, uint primCount)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_BuildAABB(_handle, aabbs, primCount);
    }

    /// <summary>
    /// Intersect a ray against the BVH.
    /// </summary>
    /// <param name="origin">Ray origin.</param>
    /// <param name="direction">Ray direction (normalized).</param>
    /// <param name="maxDistance">Maximum ray distance.</param>
    /// <returns>An <see cref="IntersectionResult"/> if a hit was found, or null.</returns>
    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        ThrowIfDisposed();

        float t = maxDistance;

        int hit = NativeMethods.TBVH_Intersect(_handle, (float*)&origin, (float*)&direction, ref t, out float u, out float v, out uint primIdx);

        if (hit == 0)
            return null;

        return new IntersectionResult
        {
            Distance = t,
            U = u,
            V = v,
            PrimitiveIndex = primIdx
        };
    }

    /// <summary>
    /// Save the BVH to a file for fast reloading.
    /// </summary>
    public void Save(string filename)
    {
        ThrowIfDisposed();
        if (NativeMethods.TBVH_Save(_handle, filename) == 0)
            throw new InvalidOperationException($"Failed to save BVH to '{filename}'.");
    }

    /// <summary>
    /// Load a previously saved BVH. Requires the same vertex data used for building.
    /// </summary>
    public void Load(string filename, float[] vertices, uint triCount)
    {
        ThrowIfDisposed();
        if (NativeMethods.TBVH_Load(_handle, filename, vertices, triCount) == 0)
            throw new InvalidOperationException($"Failed to load BVH from '{filename}'.");
    }

    /// <summary>
    /// Load a previously saved BVH with index array.
    /// </summary>
    public void LoadIndexed(string filename, float[] vertices, uint[] indices, uint triCount)
    {
        ThrowIfDisposed();
        if (NativeMethods.TBVH_LoadIndexed(_handle, filename, vertices, indices, triCount) == 0)
            throw new InvalidOperationException($"Failed to load BVH from '{filename}'.");
    }

    /// <summary>
    /// Refit the BVH after vertex positions have changed, without a full rebuild.
    /// Much faster than <see cref="Build"/>, but may degrade quality over time.
    /// </summary>
    public void Refit(uint nodeIdx = 0)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_Refit(_handle, nodeIdx);
    }

    /// <summary>
    /// Returns the total number of nodes in the BVH.
    /// </summary>
    public int NodeCount
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.TBVH_NodeCount(_handle);
        }
    }

    /// <summary>
    /// Returns the number of triangles in the BVH.
    /// </summary>
    public int TriangleCount
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.TBVH_TriangleCount(_handle);
        }
    }

    /// <summary>
    /// Shadow ray query — returns true if the ray to maxDistance is occluded by any geometry.
    /// </summary>
    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_IsOccluded(_handle, (float*)&origin, (float*)&direction, maxDistance) != 0;
    }

    /// <summary>
    /// Compute the Surface Area Heuristic cost of the BVH tree (lower is better).
    /// </summary>
    public float SAHCost(uint nodeIdx = 0)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_SAHCost(_handle, nodeIdx);
    }

    /// <summary>
    /// Returns the number of leaf nodes in the BVH.
    /// </summary>
    public int LeafCount
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.TBVH_LeafCount(_handle);
        }
    }

    /// <summary>
    /// Returns the primitive count in a subtree (default: root).
    /// </summary>
    public int GetPrimCount(uint nodeIdx = 0)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_PrimCount(_handle, nodeIdx);
    }

    /// <summary>
    /// Estimated Potential Overlap cost (alternative SAH metric, lower is better).
    /// </summary>
    public float EPOCost(uint nodeIdx = 0)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_EPOCost(_handle, nodeIdx);
    }

    /// <summary>
    /// Test if a sphere overlaps any BVH primitive.
    /// </summary>
    public unsafe bool IntersectSphere(float centerX, float centerY, float centerZ, float radius)
    {
        ThrowIfDisposed();
        var center = stackalloc float[3] { centerX, centerY, centerZ };
        return NativeMethods.TBVH_IntersectSphere(_handle, center, radius) != 0;
    }

    /// <summary>
    /// Optimize the BVH tree structure using SAH-based tree rotations.
    /// </summary>
    /// <param name="iterations">Number of optimization passes (default 25).</param>
    /// <param name="extreme">Use exhaustive (slower but better) optimization.</param>
    /// <param name="stochastic">Use stochastic optimization.</param>
    public void Optimize(uint iterations = 25, bool extreme = false, bool stochastic = false)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_Optimize(_handle, iterations, extreme ? 1 : 0, stochastic ? 1 : 0);
    }

    /// <summary>
    /// Compact the BVH — removes unused nodes, shrinks memory footprint.
    /// </summary>
    public void Compact()
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_Compact(_handle);
    }

    /// <summary>
    /// Split leaf nodes containing more than maxPrims primitives.
    /// </summary>
    public void SplitLeafs(uint maxPrims)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_SplitLeafs(_handle, maxPrims);
    }

    /// <summary>
    /// Combine small leaf nodes to reduce node count.
    /// </summary>
    public void CombineLeafs(uint nodeIdx = 0)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_CombineLeafs(_handle, nodeIdx);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            NativeMethods.TBVH_Destroy(_handle);
            _handle = IntPtr.Zero;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Result of a ray-BVH intersection query.
/// </summary>
public readonly struct IntersectionResult
{
    /// <summary>Distance along the ray to the intersection point.</summary>
    public float Distance { get; init; }

    /// <summary>Barycentric coordinate U at the hit point.</summary>
    public float U { get; init; }

    /// <summary>Barycentric coordinate V at the hit point.</summary>
    public float V { get; init; }

    /// <summary>Index of the hit primitive (triangle).</summary>
    public uint PrimitiveIndex { get; init; }
}
