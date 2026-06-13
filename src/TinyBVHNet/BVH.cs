using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around the TinyBVH native library.
/// Provides a safe, object-oriented API for building and querying BVH structures.
/// </summary>
public class BVH : NativeObject, IBVH
{   
    /// <summary>
    /// Creates a new BVH instance. Call <see cref="Build(ReadOnlySpan{float}, uint)"/> to construct the hierarchy.
    /// </summary>
    public BVH()
        : base(NativeMethods.TBVH_Create(), NativeMethods.TBVH_Destroy)
    {
    }

    /// <inheritdoc/>
    public unsafe void Build(ReadOnlySpan<float> vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices span too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        fixed (float* ptr = vertices)
            NativeMethods.TBVH_Build(Handle, ptr, triCount);
    }

    /// <summary>High-quality build (slower, better tree).</summary>
    public unsafe void BuildHQ(ReadOnlySpan<float> vertices, uint triCount)
    {
        if (vertices.Length < triCount * 3 * 4)
            throw new ArgumentException($"Vertices span too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
        fixed (float* ptr = vertices)
            NativeMethods.TBVH_BuildHQ(Handle, ptr, triCount);
    }

    /// <summary>Build from indexed triangle data.</summary>
    public unsafe void BuildIndexed(ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices, uint triCount)
    {
        fixed (float* vPtr = vertices)
        fixed (uint* iPtr = indices)
            NativeMethods.TBVH_BuildIndexed(Handle, vPtr, iPtr, triCount);
    }

    /// <summary>Build from precomputed AABBs.</summary>
    public unsafe void BuildAABB(ReadOnlySpan<float> aabbs, uint primCount)
    {
        fixed (float* ptr = aabbs)
            NativeMethods.TBVH_BuildAABB(Handle, ptr, primCount);
    }

    /// <inheritdoc/>
    public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.Intersect(Handle, origin, direction, maxDistance, NativeMethods.TBVH_Intersect);
    }

    /// <summary>
    /// Save the BVH to a file for fast reloading.
    /// </summary>
    public void Save(string filename)
    {
        if (NativeMethods.TBVH_Save(Handle, filename) == 0)
            throw new InvalidOperationException($"Failed to save BVH to '{filename}'.");
    }

    /// <summary>Load a previously saved BVH.</summary>
    public unsafe void Load(string filename, ReadOnlySpan<float> vertices, uint triCount)
    {
        fixed (float* ptr = vertices)
        {
            if (NativeMethods.TBVH_Load(Handle, filename, ptr, triCount) == 0)
                throw new InvalidOperationException($"Failed to load BVH from '{filename}'.");
        }
    }

    /// <summary>
    /// Load a previously saved BVH with index data.
    /// </summary>
    public unsafe void LoadIndexed(string filename, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices, uint triCount)
    {
        fixed (float* vPtr = vertices)
        fixed (uint* iPtr = indices)
        {
            if (NativeMethods.TBVH_LoadIndexed(Handle, filename, vPtr, iPtr, triCount) == 0)
                throw new InvalidOperationException($"Failed to load BVH from '{filename}'.");
        }
    }

    /// <summary>Refit the BVH after vertex changes.</summary>
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

    /// <inheritdoc/>
    public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = float.MaxValue)
    {
        return IntersectHelper.IsOccluded(Handle, origin, direction, maxDistance, NativeMethods.TBVH_IsOccluded);
    }

    /// <inheritdoc/>
    public float SAHCost(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_SAHCost(Handle, nodeIdx);
    }

    /// <summary>Number of leaf nodes.</summary>
    public int LeafCount
    {
        get
        {
            return NativeMethods.TBVH_LeafCount(Handle);
        }
    }

    /// <summary>Primitive count in a subtree.</summary>
    public int GetPrimCount(uint nodeIdx = 0)
    {
        return NativeMethods.TBVH_PrimCount(Handle, nodeIdx);
    }

    /// <summary>Estimated Potential Overlap cost.</summary>
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

    /// <summary>Optimize the BVH tree structure.</summary>
    public void Optimize(uint iterations = 25, bool extreme = false, bool stochastic = false)
    {
        NativeMethods.TBVH_Optimize(Handle, iterations, extreme ? 1 : 0, stochastic ? 1 : 0);
    }

    /// <summary>Compact the BVH to shrink memory.</summary>
    public void Compact()
    {
        NativeMethods.TBVH_Compact(Handle);
    }

    /// <summary>Split leaf nodes containing many primitives.</summary>
    public void SplitLeafs(uint maxPrims)
    {
        NativeMethods.TBVH_SplitLeafs(Handle, maxPrims);
    }

    /// <summary>Combine small leaf nodes.</summary>
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
