using System;
using System.Numerics;

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
        /// <summary>Creates a new GPU BVH instance.</summary>
        public BVHGPU()
            : base(NativeMethods.TBVH_GPU_Create(), NativeMethods.TBVH_GPU_Destroy)
        {
        }

        /// <inheritdoc/>
        public unsafe void Build(ReadOnlySpan<float> vertices, uint triCount)
        {
            if (vertices.Length < triCount * 3 * 4)
                throw new ArgumentException($"Vertices span too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
            fixed (float* ptr = vertices)
                NativeMethods.TBVH_GPU_Build(Handle, ptr, triCount);
        }

        /// <summary>High-quality build (slower, better tree).</summary>
        public unsafe void BuildHQ(ReadOnlySpan<float> vertices, uint triCount)
        {
            if (vertices.Length < triCount * 3 * 4)
                throw new ArgumentException($"Vertices span too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
            fixed (float* ptr = vertices)
                NativeMethods.TBVH_GPU_BuildHQ(Handle, ptr, triCount);
        }

        /// <summary>Build from indexed triangle data.</summary>
        public unsafe void BuildIndexed(ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices, uint triCount)
        {
            fixed (float* vPtr = vertices)
            fixed (uint* iPtr = indices)
                NativeMethods.TBVH_GPU_BuildIndexed(Handle, vPtr, iPtr, triCount);
        }

        /// <inheritdoc/>
        public unsafe IntersectionResult? Intersect(Vector3 origin, Vector3 direction, float maxDistance = 1e30f)
        {
            return IntersectHelper.Intersect(Handle, origin, direction, maxDistance, NativeMethods.TBVH_GPU_Intersect);
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

            unsafe
            {
                fixed (float* nPtr = nodes)
                fixed (uint* pPtr = primIndices)
                fixed (float* vPtr = vertices)
                {
                    NativeMethods.TBVH_GPU_GetNodes(Handle, nPtr);
                    NativeMethods.TBVH_GPU_GetPrimitiveIndices(Handle, pPtr);
                    NativeMethods.TBVH_GPU_GetVertices(Handle, vPtr);
                }
            }

            return new GpuBvhData
            {
                NodeCount = nodeCount,
                TriangleCount = triCount,
                Nodes = nodes,
                PrimitiveIndices = primIndices,
                Vertices = vertices
            };
        }

        /// <inheritdoc/>
        public unsafe bool IsOccluded(Vector3 origin, Vector3 direction, float maxDistance = 1e30f)
        {
            return IntersectHelper.IsOccluded(Handle, origin, direction, maxDistance, NativeMethods.TBVH_GPU_IsOccluded);
        }

        /// <inheritdoc/>
        public float SAHCost(uint nodeIdx = 0)
        {
            return NativeMethods.TBVH_GPU_SAHCost(Handle, nodeIdx);
        }

        /// <summary>Optimize the BVH tree structure.</summary>
        public void Optimize(uint iterations = 25, bool extreme = false)
        {
            NativeMethods.TBVH_GPU_Optimize(Handle, iterations, extreme ? 1 : 0);
        }
    }
}