// TinyBVHNet C API — public header
// Wraps the TinyBVH BVH classes as a C-compatible API for .NET P/Invoke.
#pragma once

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

// Opaque handle to a BVH instance.
typedef void* TBVH_HANDLE;

// ── Regular binary BVH (LAYOUT_BVH) ────────────────────────────

// Create a new BVH instance. Returns NULL on failure.
TBVH_HANDLE TBVH_Create(void);

// Destroy a BVH instance and free all resources.
void TBVH_Destroy(TBVH_HANDLE bvh);

// Build the BVH from triangle data.
// vertices: array of float4 per vertex (x,y,z,w interleaved). Total elements = triCount * 3 * 4.
// triCount: number of triangles.
void TBVH_Build(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);

// Intersect a ray against the BVH.
// origin: float3 ray origin.
// direction: float3 ray direction (should be normalized).
// t: in/out — max ray distance on input, intersection distance on output.
// u, v: out — barycentric coordinates of hit.
// primIdx: out — index of the hit primitive.
// Returns 0 if no hit, 1 if hit.
int32_t TBVH_Intersect(TBVH_HANDLE bvh, const float* origin, const float* direction,
                       float* t, float* u, float* v, uint32_t* primIdx);

// Save the BVH structure to a file.
// Returns 0 on failure, 1 on success.
int32_t TBVH_Save(TBVH_HANDLE bvh, const char* filename);

// Load a BVH structure from a file.
// vertices must match the original build data.
// Returns 0 on failure, 1 on success.
int32_t TBVH_Load(TBVH_HANDLE bvh, const char* filename,
                  const float* vertices, uint32_t triCount);

// Refit the BVH after vertex data has changed (without full rebuild).
void TBVH_Refit(TBVH_HANDLE bvh, uint32_t nodeIdx);

// Get the number of BVH nodes (for debugging / metrics).
int32_t TBVH_NodeCount(TBVH_HANDLE bvh);

// Get the number of triangles in the BVH.
int32_t TBVH_TriangleCount(TBVH_HANDLE bvh);

// Shadow ray query — returns 1 if occluded (any hit before maxDistance), 0 if clear.
int32_t TBVH_IsOccluded(TBVH_HANDLE bvh,
                        const float* origin, const float* direction, float maxDistance);

// Compute the Surface Area Heuristic cost of the BVH tree (lower is better).
float TBVH_SAHCost(TBVH_HANDLE bvh, uint32_t nodeIdx);

// ── BVH extended build methods ────────────────────────────────────

// High-quality build (slower, but better tree quality).
void TBVH_BuildHQ(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);

// Build from indexed vertices.
void TBVH_BuildIndexed(TBVH_HANDLE bvh, const float* vertices,
                       const uint32_t* indices, uint32_t triCount);

// Build from precomputed AABBs (6 floats per primitive: minX, minY, minZ, maxX, maxY, maxZ).
void TBVH_BuildAABB(TBVH_HANDLE bvh, const float* aabbs, uint32_t primCount);

// Load with index array variant.
int32_t TBVH_LoadIndexed(TBVH_HANDLE bvh, const char* filename,
                         const float* vertices, const uint32_t* indices, uint32_t triCount);

// ── BVH extended query / metrics ──────────────────────────────────

// Number of leaf nodes.
int32_t TBVH_LeafCount(TBVH_HANDLE bvh);

// Primitive count in a subtree (default: root).
int32_t TBVH_PrimCount(TBVH_HANDLE bvh, uint32_t nodeIdx);

// Estimated Potential Overlap cost (alternative SAH metric).
float TBVH_EPOCost(TBVH_HANDLE bvh, uint32_t nodeIdx);

// Sphere intersection test — returns 1 if sphere overlaps any primitive.
int32_t TBVH_IntersectSphere(TBVH_HANDLE bvh,
                            const float* center, float radius);

// ── BVH optimization ──────────────────────────────────────────────

// Optimize the BVH tree structure (25 iterations default).
void TBVH_Optimize(TBVH_HANDLE bvh, uint32_t iterations, int32_t extreme, int32_t stochastic);

// Compact the BVH — removes unused nodes, shrinks memory.
void TBVH_Compact(TBVH_HANDLE bvh);

// Split leaf nodes containing more than maxPrims primitives.
void TBVH_SplitLeafs(TBVH_HANDLE bvh, uint32_t maxPrims);

// Combine small leaf nodes to reduce node count.
void TBVH_CombineLeafs(TBVH_HANDLE bvh, uint32_t nodeIdx);

// ── GPU binary BVH (LAYOUT_BVH_GPU, 64-byte Aila-Laine nodes) ─

// Create a new GPU BVH instance.
TBVH_HANDLE TBVH_GPU_Create(void);

// Destroy a GPU BVH instance.
void TBVH_GPU_Destroy(TBVH_HANDLE bvh);

// Build the GPU BVH from triangle data (builds regular BVH internally, then converts).
void TBVH_GPU_Build(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);

// Intersect a ray against the GPU BVH.
int32_t TBVH_GPU_Intersect(TBVH_HANDLE bvh, const float* origin, const float* direction,
                           float* t, float* u, float* v, uint32_t* primIdx);

// Query GPU BVH data sizes needed for GPU upload.
int32_t TBVH_GPU_GetNodeCount(TBVH_HANDLE bvh);
int32_t TBVH_GPU_GetTriangleCount(TBVH_HANDLE bvh);

// Extract GPU BVH data for upload to compute shader.
// nodeData: output float array, size = TBVH_GPU_GetNodeCount() * 16 (4 float4 per node)
// primIndices: output uint array, size = TBVH_GPU_GetTriangleCount() (may be filled with identity)
// vertexData: output float array, size = TBVH_GPU_GetTriangleCount() * 3 * 4 (3 float4 per tri)
// All output buffers must be pre-allocated by the caller.
void TBVH_GPU_GetNodes(TBVH_HANDLE bvh, float* nodeData);
void TBVH_GPU_GetPrimitiveIndices(TBVH_HANDLE bvh, uint32_t* primIndices);
void TBVH_GPU_GetVertices(TBVH_HANDLE bvh, float* vertexData);

// Shadow ray query for GPU BVH.
int32_t TBVH_GPU_IsOccluded(TBVH_HANDLE bvh,
                            const float* origin, const float* direction, float maxDistance);

// SAH cost for GPU BVH.
float TBVH_GPU_SAHCost(TBVH_HANDLE bvh, uint32_t nodeIdx);

// Optimize the GPU BVH tree structure.
void TBVH_GPU_Optimize(TBVH_HANDLE bvh, uint32_t iterations, int32_t extreme);

// ── GPU BVH extended build methods ────────────────────────────────

void TBVH_GPU_BuildHQ(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);
void TBVH_GPU_BuildIndexed(TBVH_HANDLE bvh, const float* vertices,
                           const uint32_t* indices, uint32_t triCount);

// ── 4-wide GPU BVH (LAYOUT_BVH4_GPU, quantized 64-byte nodes) ─

// Create a new 4-wide GPU BVH instance.
TBVH_HANDLE TBVH_GPU4_Create(void);

// Destroy a 4-wide GPU BVH instance.
void TBVH_GPU4_Destroy(TBVH_HANDLE bvh);

// Build the 4-wide GPU BVH from triangle data.
void TBVH_GPU4_Build(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);

// Intersect a ray against the 4-wide GPU BVH.
int32_t TBVH_GPU4_Intersect(TBVH_HANDLE bvh, const float* origin, const float* direction,
                            float* t, float* u, float* v, uint32_t* primIdx);

// Query 4-wide GPU BVH data sizes needed for GPU upload.
int32_t TBVH_GPU4_GetNodeCount(TBVH_HANDLE bvh);
int32_t TBVH_GPU4_GetTriangleCount(TBVH_HANDLE bvh);

// Extract 4-wide GPU BVH data for upload to compute shader.
void TBVH_GPU4_GetNodes(TBVH_HANDLE bvh, float* nodeData);
void TBVH_GPU4_GetPrimitiveIndices(TBVH_HANDLE bvh, uint32_t* primIndices);
void TBVH_GPU4_GetVertices(TBVH_HANDLE bvh, float* vertexData);

// Shadow ray query for 4-wide GPU BVH.
int32_t TBVH_GPU4_IsOccluded(TBVH_HANDLE bvh,
                             const float* origin, const float* direction, float maxDistance);

// SAH cost for 4-wide GPU BVH.
float TBVH_GPU4_SAHCost(TBVH_HANDLE bvh, uint32_t nodeIdx);

// ── GPU4 extended build methods ───────────────────────────────────

void TBVH_GPU4_BuildHQ(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);
void TBVH_GPU4_BuildIndexed(TBVH_HANDLE bvh, const float* vertices,
                            const uint32_t* indices, uint32_t triCount);

// ── GPU4 extended query / metrics ─────────────────────────────────

int32_t TBVH_GPU4_LeafCount(TBVH_HANDLE bvh);

// ── GPU4 optimization ─────────────────────────────────────────────

void TBVH_GPU4_Optimize(TBVH_HANDLE bvh, uint32_t iterations, int32_t extreme);

// ── VoxelSet (voxel grid, inherits BVHBase) ──────────────────────

TBVH_HANDLE TBVH_VoxelSet_Create(void);
void TBVH_VoxelSet_Destroy(TBVH_HANDLE bvh);
void TBVH_VoxelSet_Set(TBVH_HANDLE bvh, uint32_t x, uint32_t y, uint32_t z, uint32_t v);
void TBVH_VoxelSet_UpdateTopGrid(TBVH_HANDLE bvh);
int32_t TBVH_VoxelSet_Intersect(TBVH_HANDLE bvh,
                                const float* origin, const float* direction,
                                float* t, float* u, float* v, uint32_t* primIdx);
int32_t TBVH_VoxelSet_IsOccluded(TBVH_HANDLE bvh,
                                 const float* origin, const float* direction, float maxDistance);

// ── BLASInstance (bottom-level acceleration structure) ───────────

TBVH_HANDLE TBVH_BLASInstance_Create(uint32_t idx);
void TBVH_BLASInstance_Destroy(TBVH_HANDLE bvh);
void TBVH_BLASInstance_Update(TBVH_HANDLE blas, TBVH_HANDLE bvh);
void TBVH_BLASInstance_InvertTransform(TBVH_HANDLE blas);
void TBVH_BLASInstance_SetTransform(TBVH_HANDLE blas, const float* matrix4x4);

// ── Structure-of-Arrays BVH (LAYOUT_BVH_SOA) ─────────────────────

TBVH_HANDLE TBVH_SoA_Create(void);
void TBVH_SoA_Destroy(TBVH_HANDLE bvh);
void TBVH_SoA_Build(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);
void TBVH_SoA_ConvertFrom(TBVH_HANDLE bvh, TBVH_HANDLE sourceBvh);
int32_t TBVH_SoA_Intersect(TBVH_HANDLE bvh,
                           const float* origin, const float* direction,
                           float* t, float* u, float* v, uint32_t* primIdx);
int32_t TBVH_SoA_IsOccluded(TBVH_HANDLE bvh,
                            const float* origin, const float* direction, float maxDistance);
float TBVH_SoA_SAHCost(TBVH_HANDLE bvh, uint32_t nodeIdx);
void TBVH_SoA_Optimize(TBVH_HANDLE bvh, uint32_t iterations, int32_t extreme);

// ── Verbose BVH (LAYOUT_BVH_VERBOSE, debugging / optimizer) ──────

TBVH_HANDLE TBVH_Verbose_Create(void);
void TBVH_Verbose_Destroy(TBVH_HANDLE bvh);
void TBVH_Verbose_ConvertFrom(TBVH_HANDLE bvh, TBVH_HANDLE sourceBvh);
void TBVH_Verbose_Build(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);
int32_t TBVH_Verbose_NodeCount(TBVH_HANDLE bvh);
float TBVH_Verbose_SAHCost(TBVH_HANDLE bvh, uint32_t nodeIdx);
void TBVH_Verbose_Refit(TBVH_HANDLE bvh, uint32_t nodeIdx);
void TBVH_Verbose_Optimize(TBVH_HANDLE bvh, uint32_t iterations, int32_t extreme, int32_t stochastic);
void TBVH_Verbose_Compact(TBVH_HANDLE bvh);

// ── 4-wide CPU BVH (LAYOUT_BVH4_CPU, SSE) ────────────────────────

TBVH_HANDLE TBVH_4CPU_Create(void);
void TBVH_4CPU_Destroy(TBVH_HANDLE bvh);
void TBVH_4CPU_Build(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);
void TBVH_4CPU_BuildHQ(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);
void TBVH_4CPU_ConvertFrom(TBVH_HANDLE bvh, TBVH_HANDLE sourceBvh);
int32_t TBVH_4CPU_Intersect(TBVH_HANDLE bvh,
                            const float* origin, const float* direction,
                            float* t, float* u, float* v, uint32_t* primIdx);
int32_t TBVH_4CPU_IsOccluded(TBVH_HANDLE bvh,
                             const float* origin, const float* direction, float maxDistance);
float TBVH_4CPU_SAHCost(TBVH_HANDLE bvh, uint32_t nodeIdx);
void TBVH_4CPU_Optimize(TBVH_HANDLE bvh, uint32_t iterations, int32_t extreme);
void TBVH_4CPU_Refit(TBVH_HANDLE bvh);
int32_t TBVH_4CPU_Save(TBVH_HANDLE bvh, const char* filename);
int32_t TBVH_4CPU_Load(TBVH_HANDLE bvh, const char* filename,
                       const float* vertices, uint32_t triCount);

// ── 8-wide CPU BVH (LAYOUT_BVH8_CPU, AVX-256) ────────────────────

TBVH_HANDLE TBVH_8CPU_Create(void);
void TBVH_8CPU_Destroy(TBVH_HANDLE bvh);
void TBVH_8CPU_Build(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);
void TBVH_8CPU_BuildHQ(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);
void TBVH_8CPU_ConvertFrom(TBVH_HANDLE bvh, TBVH_HANDLE sourceBvh);
int32_t TBVH_8CPU_Intersect(TBVH_HANDLE bvh,
                            const float* origin, const float* direction,
                            float* t, float* u, float* v, uint32_t* primIdx);
int32_t TBVH_8CPU_IsOccluded(TBVH_HANDLE bvh,
                             const float* origin, const float* direction, float maxDistance);
float TBVH_8CPU_SAHCost(TBVH_HANDLE bvh, uint32_t nodeIdx);
void TBVH_8CPU_Optimize(TBVH_HANDLE bvh, uint32_t iterations, int32_t extreme);
void TBVH_8CPU_Refit(TBVH_HANDLE bvh);
int32_t TBVH_8CPU_Save(TBVH_HANDLE bvh, const char* filename);
int32_t TBVH_8CPU_Load(TBVH_HANDLE bvh, const char* filename,
                       const float* vertices, uint32_t triCount);

// ── Compressed Wide BVH for GPU (LAYOUT_BVH8_CWBVH) ──────────────

TBVH_HANDLE TBVH_8CWBVH_Create(void);
void TBVH_8CWBVH_Destroy(TBVH_HANDLE bvh);
void TBVH_8CWBVH_Build(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);
void TBVH_8CWBVH_BuildHQ(TBVH_HANDLE bvh, const float* vertices, uint32_t triCount);
int32_t TBVH_8CWBVH_Intersect(TBVH_HANDLE bvh,
                              const float* origin, const float* direction,
                              float* t, float* u, float* v, uint32_t* primIdx);
int32_t TBVH_8CWBVH_IsOccluded(TBVH_HANDLE bvh,
                               const float* origin, const float* direction, float maxDistance);
float TBVH_8CWBVH_SAHCost(TBVH_HANDLE bvh, uint32_t nodeIdx);
void TBVH_8CWBVH_Optimize(TBVH_HANDLE bvh, uint32_t iterations, int32_t extreme);
int32_t TBVH_8CWBVH_Save(TBVH_HANDLE bvh, const char* filename);
int32_t TBVH_8CWBVH_Load(TBVH_HANDLE bvh, const char* filename,
                         const float* vertices, uint32_t triCount);

// ── BVH_Double (LAYOUT_BVH_DOUBLE, 64-bit precision) ───────────

TBVH_HANDLE TBVH_Double_Create(void);
void TBVH_Double_Destroy(TBVH_HANDLE bvh);
void TBVH_Double_Build(TBVH_HANDLE bvh, const double* vertices, uint64_t primCount);
int32_t TBVH_Double_Intersect(TBVH_HANDLE bvh,
                             const double* origin, const double* direction,
                             double* t, double* u, double* v, uint64_t* primIdx);
int32_t TBVH_Double_IsOccluded(TBVH_HANDLE bvh,
                              const double* origin, const double* direction,
                              double maxDistance);
double TBVH_Double_SAHCost(TBVH_HANDLE bvh, uint64_t nodeIdx);

// ── JobSystem (threading) ────────────────────────────────────────

TBVH_HANDLE TBVH_JobSystem_Create(void);
void TBVH_JobSystem_Destroy(TBVH_HANDLE js);
int32_t TBVH_JobSystem_IsBusy(TBVH_HANDLE js);

#ifdef __cplusplus
}
#endif
