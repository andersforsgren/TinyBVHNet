using System;
using System.Numerics;

namespace TinyBVHNet
{
    /// <summary>
    /// 4-wide quantized GPU BVH (64-byte nodes, LAYOUT_BVH4_GPU).
    /// Internally builds an MBVH&lt;4&gt; then converts to GPU-friendly
    /// quantized format. Save/Load/Refit are not supported.
    /// </summary>
    public class BVH4GPU : NativeObject, IBVH
    {
        public BVH4GPU()
            : base(NativeMethods.TBVH_GPU4_Create(), NativeMethods.TBVH_GPU4_Destroy)
        {
        }

        /// <summary>
        /// Build the 4-wide GPU BVH from triangle vertex data.
        /// </summary>
        /// <param name="vertices">Interleaved float4 vertices (3 vertices per triangle = triCount * 3 * 4 floats).</param>
        /// <param name="triCount">Number of triangles.</param>
        public void Build(float[] vertices, uint triCount)
        {
            if (vertices.Length < triCount * 3 * 4)
                throw new ArgumentException($"Vertices array too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
            NativeMethods.TBVH_GPU4_Build(Handle, vertices, triCount);
        }

        /// <summary>
        /// High-quality build (slower, better tree).
        /// </summary>
        public void BuildHQ(float[] vertices, uint triCount)
        {
            if (vertices.Length < triCount * 3 * 4)
                throw new ArgumentException($"Vertices array too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
            NativeMethods.TBVH_GPU4_BuildHQ(Handle, vertices, triCount);
        }

        /// <summary>
        /// Build from indexed triangle data.
        /// </summary>
        public void BuildIndexed(float[] vertices, uint[] indices, uint triCount)
        {
            NativeMethods.TBVH_GPU4_BuildIndexed(Handle, vertices, indices, triCount);
        }

        /// <summary>
        /// Intersect a ray with the 4-wide GPU BVH.
        /// </summary>
        /// <param name="origin">Ray origin.</param>
        /// <param name="direction">Ray direction (normalized).</param>
        /// <param name="maxDistance">Maximum ray distance.</param>
        /// <returns>IntersectionResult on hit, null on miss.</returns>
        public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = 1e30f)
        {
            return IntersectHelper.Intersect(Handle, origin, direction, maxDistance, NativeMethods.TBVH_GPU4_Intersect);
        }

        /// <summary>
        /// Number of BVH nodes (available after building).
        /// </summary>
        public int NodeCount
        {
            get
            {
                return NativeMethods.TBVH_GPU4_GetNodeCount(Handle);
            }
        }

        /// <summary>
        /// Number of triangles in the BVH.
        /// </summary>
        public int TriangleCount
        {
            get
            {
                return NativeMethods.TBVH_GPU4_GetTriangleCount(Handle);
            }
        }

        /// <summary>
        /// Extract all 4-wide GPU BVH data ready for upload to a compute shader.
        /// </summary>
        public GpuBvhData ExtractGpuData()
        {
            int nodeCount = NativeMethods.TBVH_GPU4_GetNodeCount(Handle);
            int triCount = NativeMethods.TBVH_GPU4_GetTriangleCount(Handle);

            if (nodeCount <= 0 || triCount <= 0)
                throw new InvalidOperationException("BVH has not been built yet.");

            var nodes = new float[nodeCount * 16];
            var primIndices = new uint[triCount];
            var vertices = new float[triCount * 3 * 4];

            NativeMethods.TBVH_GPU4_GetNodes(Handle, nodes);
            NativeMethods.TBVH_GPU4_GetPrimitiveIndices(Handle, primIndices);
            NativeMethods.TBVH_GPU4_GetVertices(Handle, vertices);

            return new GpuBvhData
            {
                NodeCount = nodeCount,
                TriangleCount = triCount,
                Nodes = nodes,
                PrimitiveIndices = primIndices,
                Vertices = vertices
            };
        }

        /// <summary>
        /// Shadow ray query -- returns true if the ray to maxDistance is occluded by any geometry.
        /// </summary>
        public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = 1e30f)
        {
            return IntersectHelper.IsOccluded(Handle, origin, direction, maxDistance, NativeMethods.TBVH_GPU4_IsOccluded);
        }

        /// <summary>
        /// Compute the Surface Area Heuristic cost of the BVH tree (lower is better).
        /// </summary>
        public float SAHCost(uint nodeIdx = 0)
        {
            return NativeMethods.TBVH_GPU4_SAHCost(Handle, nodeIdx);
        }

        /// <summary>
        /// Returns the number of leaf nodes in the BVH.
        /// </summary>
        public int LeafCount
        {
            get
            {
                return NativeMethods.TBVH_GPU4_LeafCount(Handle);
            }
        }

        /// <summary>
        /// Optimize the BVH tree structure to reduce SAH cost.
        /// </summary>
        /// <param name="iterations">Number of optimization iterations (default 25).</param>
        /// <param name="extreme">If true, uses extreme (slower) optimization strategy.</param>
        public void Optimize(uint iterations = 25, bool extreme = false)
        {
            NativeMethods.TBVH_GPU4_Optimize(Handle, iterations, extreme ? 1 : 0);
        }
    }
}