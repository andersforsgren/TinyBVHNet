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
        /// <summary>Creates a new 4-wide GPU BVH instance.</summary>
        public BVH4GPU()
            : base(NativeMethods.TBVH_GPU4_Create(), NativeMethods.TBVH_GPU4_Destroy)
        {
        }

        /// <inheritdoc/>
        public unsafe void Build(ReadOnlySpan<float> vertices, uint triCount)
        {
            if (vertices.Length < triCount * 3 * 4)
                throw new ArgumentException($"Vertices span too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
            fixed (float* ptr = vertices)
                NativeMethods.TBVH_GPU4_Build(Handle, ptr, triCount);
        }

        /// <summary>High-quality build (slower, better tree).</summary>
        public unsafe void BuildHQ(ReadOnlySpan<float> vertices, uint triCount)
        {
            if (vertices.Length < triCount * 3 * 4)
                throw new ArgumentException($"Vertices span too small. Expected at least {triCount * 3 * 4}, got {vertices.Length}.", nameof(vertices));
            fixed (float* ptr = vertices)
                NativeMethods.TBVH_GPU4_BuildHQ(Handle, ptr, triCount);
        }

        /// <summary>Build from indexed triangle data.</summary>
        public unsafe void BuildIndexed(ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices, uint triCount)
        {
            fixed (float* vPtr = vertices)
            fixed (uint* iPtr = indices)
                NativeMethods.TBVH_GPU4_BuildIndexed(Handle, vPtr, iPtr, triCount);
        }

        /// <inheritdoc/>
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

            unsafe
            {
                fixed (float* nPtr = nodes)
                fixed (uint* pPtr = primIndices)
                fixed (float* vPtr = vertices)
                {
                    NativeMethods.TBVH_GPU4_GetNodes(Handle, nPtr);
                    NativeMethods.TBVH_GPU4_GetPrimitiveIndices(Handle, pPtr);
                    NativeMethods.TBVH_GPU4_GetVertices(Handle, vPtr);
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
            return IntersectHelper.IsOccluded(Handle, origin, direction, maxDistance, NativeMethods.TBVH_GPU4_IsOccluded);
        }

        /// <inheritdoc/>
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

        /// <summary>Optimize the BVH tree structure.</summary>
        public void Optimize(uint iterations = 25, bool extreme = false)
        {
            NativeMethods.TBVH_GPU4_Optimize(Handle, iterations, extreme ? 1 : 0);
        }
    }
}