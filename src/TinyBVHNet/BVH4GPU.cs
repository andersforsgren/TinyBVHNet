using System;

namespace TinyBVHNet
{
    /// <summary>
    /// 4-wide quantized GPU BVH (64-byte nodes, LAYOUT_BVH4_GPU).
    /// Internally builds an MBVH&lt;4&gt; then converts to GPU-friendly
    /// quantized format. Save/Load/Refit are not supported.
    /// </summary>
    public class BVH4GPU : IDisposable
    {
        private IntPtr _handle;
        private bool _isDisposed;

        public bool IsBuilt => _handle != IntPtr.Zero;

        public BVH4GPU()
        {
            _handle = NativeMethods.TBVH_GPU4_Create();
            if (_handle == IntPtr.Zero)
                throw new OutOfMemoryException("Failed to create native BVH4_GPU instance.");
        }

        /// <summary>
        /// Build the 4-wide GPU BVH from triangle vertex data.
        /// </summary>
        /// <param name="vertices">Interleaved float4 vertices (3 vertices per triangle = triCount * 3 * 4 floats).</param>
        /// <param name="triCount">Number of triangles.</param>
        public void Build(float[] vertices, uint triCount)
        {
            ThrowIfDisposed();
            NativeMethods.TBVH_GPU4_Build(_handle, vertices, triCount);
        }

        /// <summary>
        /// High-quality build (slower, better tree).
        /// </summary>
        public void BuildHQ(float[] vertices, uint triCount)
        {
            ThrowIfDisposed();
            NativeMethods.TBVH_GPU4_BuildHQ(_handle, vertices, triCount);
        }

        /// <summary>
        /// Build from indexed triangle data.
        /// </summary>
        public void BuildIndexed(float[] vertices, uint[] indices, uint triCount)
        {
            ThrowIfDisposed();
            NativeMethods.TBVH_GPU4_BuildIndexed(_handle, vertices, indices, triCount);
        }

        /// <summary>
        /// Intersect a ray with the 4-wide GPU BVH.
        /// </summary>
        /// <param name="origin">Ray origin (3 floats).</param>
        /// <param name="direction">Ray direction (3 floats).</param>
        /// <param name="t">Initialized to max distance on input, set to hit distance on output.</param>
        /// <returns>IntersectionResult on hit, null on miss.</returns>
        public unsafe IntersectionResult? Intersect(float[] origin, float[] direction, float t = 1e30f)
        {
            ThrowIfDisposed();
            fixed (float* oPtr = origin, dPtr = direction)
            {
                float hitT = t;
                int result = NativeMethods.TBVH_GPU4_Intersect(_handle, oPtr, dPtr, ref hitT, out float u, out float v, out uint primIdx);
                if (result == 0)
                    return null;
                return new IntersectionResult
                {
                    Distance = hitT,
                    U = u,
                    V = v,
                    PrimitiveIndex = primIdx
                };
            }
        }

        /// <summary>
        /// Number of BVH nodes (available after building).
        /// </summary>
        public int NodeCount
        {
            get
            {
                ThrowIfDisposed();
                return NativeMethods.TBVH_GPU4_GetNodeCount(_handle);
            }
        }

        /// <summary>
        /// Number of triangles in the BVH.
        /// </summary>
        public int TriangleCount
        {
            get
            {
                ThrowIfDisposed();
                return NativeMethods.TBVH_GPU4_GetTriangleCount(_handle);
            }
        }

        /// <summary>
        /// Extract all 4-wide GPU BVH data ready for upload to a compute shader.
        /// </summary>
        public GpuBvhData ExtractGpuData()
        {
            ThrowIfDisposed();
            int nodeCount = NativeMethods.TBVH_GPU4_GetNodeCount(_handle);
            int triCount = NativeMethods.TBVH_GPU4_GetTriangleCount(_handle);

            if (nodeCount <= 0 || triCount <= 0)
                throw new InvalidOperationException("BVH has not been built yet.");

            var nodes = new float[nodeCount * 16];
            var primIndices = new uint[triCount];
            var vertices = new float[triCount * 3 * 4];

            NativeMethods.TBVH_GPU4_GetNodes(_handle, nodes);
            NativeMethods.TBVH_GPU4_GetPrimitiveIndices(_handle, primIndices);
            NativeMethods.TBVH_GPU4_GetVertices(_handle, vertices);

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
        /// Shadow ray query — returns true if the ray to maxDistance is occluded by any geometry.
        /// </summary>
        public unsafe bool IsOccluded(float[] origin, float[] direction, float maxDistance = 1e30f)
        {
            ThrowIfDisposed();
            fixed (float* oPtr = origin, dPtr = direction)
                return NativeMethods.TBVH_GPU4_IsOccluded(_handle, oPtr, dPtr, maxDistance) != 0;
        }

        /// <summary>
        /// Compute the Surface Area Heuristic cost of the BVH tree (lower is better).
        /// </summary>
        public float SAHCost(uint nodeIdx = 0)
        {
            ThrowIfDisposed();
            return NativeMethods.TBVH_GPU4_SAHCost(_handle, nodeIdx);
        }

        /// <summary>
        /// Returns the number of leaf nodes in the BVH.
        /// </summary>
        public int LeafCount
        {
            get
            {
                ThrowIfDisposed();
                return NativeMethods.TBVH_GPU4_LeafCount(_handle);
            }
        }

        /// <summary>
        /// Optimize the BVH tree structure to reduce SAH cost.
        /// </summary>
        /// <param name="iterations">Number of optimization iterations (default 25).</param>
        /// <param name="extreme">If true, uses extreme (slower) optimization strategy.</param>
        public void Optimize(uint iterations = 25, bool extreme = false)
        {
            ThrowIfDisposed();
            NativeMethods.TBVH_GPU4_Optimize(_handle, iterations, extreme ? 1 : 0);
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.TBVH_GPU4_Destroy(_handle);
                _handle = IntPtr.Zero;
            }
        }

        private void ThrowIfDisposed()
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(_isDisposed, this);
#else
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(BVH4GPU), "The BVH4GPU instance has been disposed.");
#endif
        }
    }
}