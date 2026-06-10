using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TinyBVHNet
{
    /// <summary>
    /// Holds all data needed to upload a GPU BVH to a compute shader.
    /// </summary>
    public sealed class GpuBvhData
    {
        /// <summary>Number of BVH nodes.</summary>
        public int NodeCount { get; init; }
        /// <summary>Number of triangles.</summary>
        public int TriangleCount { get; init; }
        /// <summary>BVH nodes as float array (4 float4 per node = 16 floats per node).</summary>
        public float[] Nodes { get; init; } = [];
        /// <summary>Primitive index permutation (identity if not reordered).</summary>
        public uint[] PrimitiveIndices { get; init; } = [];
        /// <summary>Vertex data as float array (3 float4 per triangle = 12 floats per triangle).</summary>
        public float[] Vertices { get; init; } = [];
    }

    /// <summary>
    /// GPU-optimized binary BVH (Aila-Laine layout, 64-byte nodes).
    /// This BVH variant is designed for GPU consumption -- it internally
    /// builds a regular BVH then converts it to GPU-friendly format.
    /// Save/Load/Refit are not supported.
    /// </summary>
    public class BVHGPU : NativeObject, IBVH
    {
        public BVHGPU()
            : base(NativeMethods.TBVH_GPU_Create(), NativeMethods.TBVH_GPU_Destroy)
        {
        }

        /// <summary>
        /// Build the GPU BVH from triangle vertex data.
        /// </summary>
        /// <param name="vertices">Interleaved float4 vertices (3 vertices per triangle = triCount * 3 * 4 floats).</param>
        /// <param name="triCount">Number of triangles.</param>
        public void Build(float[] vertices, uint triCount)
        {
            NativeMethods.TBVH_GPU_Build(Handle, vertices, triCount);
        }

        /// <summary>
        /// High-quality build (slower, better tree).
        /// </summary>
        public void BuildHQ(float[] vertices, uint triCount)
        {
            NativeMethods.TBVH_GPU_BuildHQ(Handle, vertices, triCount);
        }

        /// <summary>
        /// Build from indexed triangle data.
        /// </summary>
        public void BuildIndexed(float[] vertices, uint[] indices, uint triCount)
        {
            NativeMethods.TBVH_GPU_BuildIndexed(Handle, vertices, indices, triCount);
        }

        /// <summary>
        /// Intersect a ray with the GPU BVH.
        /// </summary>
        /// <param name="origin">Ray origin.</param>
        /// <param name="direction">Ray direction (normalized).</param>
        /// <param name="maxDistance">Maximum ray distance.</param>
        /// <returns>IntersectionResult on hit, null on miss.</returns>
        public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = 1e30f)
        {
            float t = maxDistance;
            int result = NativeMethods.TBVH_GPU_Intersect(Handle, (float*)&origin, (float*)&direction, ref t, out float u, out float v, out uint primIdx);
            if (result == 0)
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
        /// Number of BVH nodes (available after building).
        /// </summary>
        public int NodeCount
        {
            get
            {
                return NativeMethods.TBVH_GPU_GetNodeCount(Handle);
            }
        }

        /// <summary>
        /// Number of triangles in the BVH.
        /// </summary>
        public int TriangleCount
        {
            get
            {
                return NativeMethods.TBVH_GPU_GetTriangleCount(Handle);
            }
        }

        /// <summary>
        /// Extract all GPU BVH data ready for upload to a compute shader.
        /// </summary>
        public GpuBvhData ExtractGpuData()
        {
            int nodeCount = NativeMethods.TBVH_GPU_GetNodeCount(Handle);
            int triCount = NativeMethods.TBVH_GPU_GetTriangleCount(Handle);

            if (nodeCount <= 0 || triCount <= 0)
                throw new InvalidOperationException("BVH has not been built yet.");

            var nodes = new float[nodeCount * 16];
            var primIndices = new uint[triCount];
            var vertices = new float[triCount * 3 * 4];

            NativeMethods.TBVH_GPU_GetNodes(Handle, nodes);
            NativeMethods.TBVH_GPU_GetPrimitiveIndices(Handle, primIndices);
            NativeMethods.TBVH_GPU_GetVertices(Handle, vertices);

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
            return NativeMethods.TBVH_GPU_IsOccluded(Handle, (float*)&origin, (float*)&direction, maxDistance) != 0;
        }

        /// <summary>
        /// Compute the Surface Area Heuristic cost of the BVH tree (lower is better).
        /// </summary>
        public float SAHCost(uint nodeIdx = 0)
        {
            return NativeMethods.TBVH_GPU_SAHCost(Handle, nodeIdx);
        }

        /// <summary>
        /// Optimize the BVH tree structure to reduce SAH cost.
        /// </summary>
        /// <param name="iterations">Number of optimization iterations (default 25).</param>
        /// <param name="extreme">If true, uses extreme (slower) optimization strategy.</param>
        public void Optimize(uint iterations = 25, bool extreme = false)
        {
            NativeMethods.TBVH_GPU_Optimize(Handle, iterations, extreme ? 1 : 0);
        }
    }
}