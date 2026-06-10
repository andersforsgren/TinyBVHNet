using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around the TinyBVH native library.
/// Provides a safe, object-oriented API for building and querying BVH structures.
/// </summary>
public class BVH : NativeObject, IBVH
{   
    /// <summary>
    /// Creates a new BVH instance. Call <see cref="Build"/> to construct the hierarchy.
    /// </summary>
    public BVH()
        : base(NativeMethods.TBVH_Create(), NativeMethods.TBVH_Destroy)
    {
    }

    /// <summary>
    /// Build the BVH from an array of triangle vertices.
    /// Each vertex is a float4 (X, Y, Z, W), and each triangle has 3 vertices.
    /// So vertices.Length should equal triCount * 3 * 4.
    /// </summary>
    public void Build(float[] vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices array too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        NativeMethods.TBVH_Build(Handle, vertices, triCount);
    }

    /// <summary>
    /// High-quality build (slower, better tree). Same vertex format as <see cref="Build"/>.
    /// </summary>
    public void BuildHQ(float[] vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices array too small.", nameof(vertices));
        NativeMethods.TBVH_BuildHQ(Handle, vertices, triCount);
    }

    /// <summary>
    /// Build from indexed triangle data. vertices is float4-per-vertex,
    /// indices references 3 indices per triangle.
    /// </summary>
    public void BuildIndexed(float[] vertices, uint[] indices, uint triCount)
    {
        NativeMethods.TBVH_BuildIndexed(Handle, vertices, indices, triCount);
    }

    /// <summary>
    /// Build from precomputed AABBs (6 floats per primitive: minX, minY, minZ, maxX, maxY, maxZ).
    /// </summary>
    public void BuildAABB(float[] aabbs, uint primCount)
    {
        NativeMethods.TBVH_BuildAABB(Handle, aabbs, primCount);
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
        float t = maxDistance;

        int hit = NativeMethods.TBVH_Intersect(Handle, (float*)&origin, (float*)&direction, ref t, out float u, out float v, out uint primIdx);

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
        if (NativeMethods.TBVH_Save(Handle, filename) == 0)
            throw new InvalidOperationException($"Failed to save BVH to '{filename}'.");
    }

    /// <summary>
    /// Load a previously saved BVH. Requires the same vertex data used for building.
    /// </summary>
    public void Load(string filename, float[] vertices, uint triCount)
    {
        if (NativeMethods.TBVH_Load(Handle, filename, vertices, triCount) == 0)
            throw new InvalidOperationException($"Failed to load BVH from '{filename}'.");
    }

    /// <summary>
    /// Load a previously saved BVH with index array.
    /// </summary>
    public void LoadIndexed(string filename, float[] vertices, uint[] indices, uint triCount)
    {
        if (NativeMethods.TBVH_LoadIndexed(Handle, filename, vertices, indices, triCount) == 0)
            throw new InvalidOperationException($"Failed to load BVH from '{filename}'.");
    }

    /// <summary>
    /// Refit the BVH after vertex positions have changed, without a full rebuild.
    /// Much faster than <see cref="Build"/>, but may degrade quality over time.
    /// </summary>
    public void Refit(uint nodeIdx = 0)
    {
        NativeMethods.TBVH_Refit(Handle, nodeIdx);
    }

    /// <summary>
    /// Returns the total number of nodes in the BVH.
    /// </summary>
    public int NodeCount
    {
        get
        {
            return NativeMethods.TBVH_NodeCount(Handle);
        }
    }

    /// <summary>
    /// Returns the number of triangles in the BVH.
    /// </summary>
    public int TriangleCount
    {
        get
        {
            return NativeMethods.TBVH_TriangleCount(Handle);
        }
    }

    /// <summary>
    /// Shadow ray query -- returns true if the ray to maxDistance is occluded by any geometry.
    /// </summary>
    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return NativeMethods.TBVH_IsOccluded(Handle, (float*)&origin, (float*)&direction, maxDistance) != 0;
    }

    /// <summary>
    /// Compute the Surface Area Heuristic cost of the BVH tree (lower is better).
    /// </summary>
    public float SAHCost(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_SAHCost(Handle, nodeIdx);
    }

    /// <summary>
    /// Returns the number of leaf nodes in the BVH.
    /// </summary>
    public int LeafCount
    {
        get
        {
            return NativeMethods.TBVH_LeafCount(Handle);
        }
    }

    /// <summary>
    /// Returns the primitive count in a subtree (default: root).
    /// </summary>
    public int GetPrimCount(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_PrimCount(Handle, nodeIdx);
    }

    /// <summary>
    /// Estimated Potential Overlap cost (alternative SAH metric, lower is better).
    /// </summary>
    public float EPOCost(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_EPOCost(Handle, nodeIdx);
    }

    /// <summary>
    /// Test if a sphere overlaps any BVH primitive.
    /// </summary>
    public unsafe bool IntersectSphere(float centerX, float centerY, float centerZ, float radius)
    {
        var center = stackalloc float[3] { centerX, centerY, centerZ };
        return NativeMethods.TBVH_IntersectSphere(Handle, center, radius) != 0;
    }

    /// <summary>
    /// Optimize the BVH tree structure using SAH-based tree rotations.
    /// </summary>
    /// <param name="iterations">Number of optimization passes (default 25).</param>
    /// <param name="extreme">Use exhaustive (slower but better) optimization.</param>
    /// <param name="stochastic">Use stochastic optimization.</param>
    public void Optimize(uint iterations = 25, bool extreme = false, bool stochastic = false)
    {
        NativeMethods.TBVH_Optimize(Handle, iterations, extreme ? 1 : 0, stochastic ? 1 : 0);
    }

    /// <summary>
    /// Compact the BVH -- removes unused nodes, shrinks memory footprint.
    /// </summary>
    public void Compact()
    {
        NativeMethods.TBVH_Compact(Handle);
    }

    /// <summary>
    /// Split leaf nodes containing more than maxPrims primitives.
    /// </summary>
    public void SplitLeafs(uint maxPrims)
    {
        NativeMethods.TBVH_SplitLeafs(Handle, maxPrims);
    }

    /// <summary>
    /// Combine small leaf nodes to reduce node count.
    /// </summary>
    public void CombineLeafs(uint nodeIdx = 0)
    {
        NativeMethods.TBVH_CombineLeafs(Handle, nodeIdx);
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
