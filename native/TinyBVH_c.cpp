// TinyBVHNet C API — implementation
// Wraps the tinybvh::BVH class as extern "C" functions.

#define TINYBVH_IMPLEMENTATION
#include "tiny_bvh.h"

#include "TinyBVH_c.h"

#include <cstring>

// Wrap the BVH in a struct so we can use an opaque pointer.
struct TBVH_Instance
{
    tinybvh::BVH bvh;
    tinybvh::bvhvec4* vertexCopy = nullptr; // kept alive for Load
    uint32_t triCount = 0;
};

TBVH_HANDLE TBVH_Create(void)
{
    auto* inst = new TBVH_Instance();
    return static_cast<TBVH_HANDLE>(inst);
}

void TBVH_Destroy(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    delete[] inst->vertexCopy;
    delete inst;
}

void TBVH_Build(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Instance*>(handle);

    // Keep a copy of vertices in case we need them for Load later.
    delete[] inst->vertexCopy;
    uint32_t floatCount = triCount * 3 * 4; // 3 verts per tri, 4 floats per vert
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->triCount = triCount;

    inst->bvh.Build(inst->vertexCopy, triCount);
}

int32_t TBVH_Intersect(TBVH_HANDLE handle,
                       const float* origin, const float* direction,
                       float* t, float* u, float* v, uint32_t* primIdx)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return 0; // not built yet

    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    // NB: BVH::Intersect returns traversal cost, NOT a hit bool.
    // Check ray.hit.t < original max distance to determine hit.
    const float maxT = *t;
    tinybvh::Ray ray(O, D, maxT);

    inst->bvh.Intersect(ray);

    *t = ray.hit.t;
    *u = ray.hit.u;
    *v = ray.hit.v;
    *primIdx = ray.hit.prim;

    return (ray.hit.t < maxT) ? 1 : 0;
}

int32_t TBVH_Save(TBVH_HANDLE handle, const char* filename)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    inst->bvh.Save(filename);
    return 1;
}

int32_t TBVH_Load(TBVH_HANDLE handle, const char* filename,
                  const float* vertices, uint32_t triCount)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Instance*>(handle);

    delete[] inst->vertexCopy;
    uint32_t floatCount = triCount * 3 * 4;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->triCount = triCount;

    return inst->bvh.Load(filename, inst->vertexCopy, triCount) ? 1 : 0;
}

void TBVH_Refit(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return; // not built yet
    inst->bvh.Refit(nodeIdx);
}

int32_t TBVH_NodeCount(TBVH_HANDLE handle)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return 0; // not built yet
    return inst->bvh.NodeCount();
}

int32_t TBVH_TriangleCount(TBVH_HANDLE handle)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return 0; // not built yet
    return static_cast<int32_t>(inst->triCount);
}

int32_t TBVH_IsOccluded(TBVH_HANDLE handle,
                        const float* origin, const float* direction, float maxDistance)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return 0; // not built yet

    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    tinybvh::Ray ray(O, D, maxDistance);

    return inst->bvh.IsOccluded(ray) ? 1 : 0;
}

float TBVH_SAHCost(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return 0.0f;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return 0.0f; // not built yet
    return inst->bvh.SAHCost(nodeIdx);
}

// ── BVH extended build methods ────────────────────────────────────

void TBVH_BuildHQ(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->triCount = triCount;
    inst->bvh.BuildHQ(inst->vertexCopy, triCount);
}

void TBVH_BuildIndexed(TBVH_HANDLE handle, const float* vertices,
                       const uint32_t* indices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->triCount = triCount;
    inst->bvh.Build(inst->vertexCopy, indices, triCount);
}

void TBVH_BuildAABB(TBVH_HANDLE handle, const float* aabbs, uint32_t primCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    inst->triCount = primCount;
    inst->bvh.BuildAABB(reinterpret_cast<const tinybvh::bvhvec4*>(aabbs), primCount);
}

int32_t TBVH_LoadIndexed(TBVH_HANDLE handle, const char* filename,
                         const float* vertices, const uint32_t* indices, uint32_t triCount)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->triCount = triCount;
    return inst->bvh.Load(filename, inst->vertexCopy, indices, triCount) ? 1 : 0;
}

// ── BVH extended query / metrics ──────────────────────────────────

int32_t TBVH_LeafCount(TBVH_HANDLE handle)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    return inst->bvh.LeafCount();
}

int32_t TBVH_PrimCount(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    return inst->bvh.PrimCount(nodeIdx);
}

float TBVH_EPOCost(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return 0.0f;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    return inst->bvh.EPOCost(nodeIdx);
}

int32_t TBVH_IntersectSphere(TBVH_HANDLE handle,
                            const float* center, float radius)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    tinybvh::bvhvec3 C(center[0], center[1], center[2]);
    return inst->bvh.IntersectSphere(C, radius) ? 1 : 0;
}

// ── BVH optimization ──────────────────────────────────────────────

void TBVH_Optimize(TBVH_HANDLE handle, uint32_t iterations, int32_t extreme, int32_t stochastic)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    inst->bvh.Optimize(iterations, extreme != 0, stochastic != 0);
}

void TBVH_Compact(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    inst->bvh.Compact();
}

void TBVH_SplitLeafs(TBVH_HANDLE handle, uint32_t maxPrims)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    inst->bvh.SplitLeafs(maxPrims);
}

void TBVH_CombineLeafs(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Instance*>(handle);
    inst->bvh.CombineLeafs(nodeIdx);
}

// ── GPU binary BVH (LAYOUT_BVH_GPU) ────────────────────────────

struct TBVH_GPU_Instance
{
    tinybvh::BVH_GPU bvh;
    tinybvh::bvhvec4* vertexCopy = nullptr; // must outlive the BVH
    uint32_t triCount = 0;
};

TBVH_HANDLE TBVH_GPU_Create(void)
{
    auto* inst = new TBVH_GPU_Instance();
    return static_cast<TBVH_HANDLE>(inst);
}

void TBVH_GPU_Destroy(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);
    delete[] inst->vertexCopy;
    delete inst;
}

void TBVH_GPU_Build(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->triCount = triCount;

    inst->bvh.Build(inst->vertexCopy, triCount);
}

int32_t TBVH_GPU_Intersect(TBVH_HANDLE handle,
                           const float* origin, const float* direction,
                           float* t, float* u, float* v, uint32_t* primIdx)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return 0; // not built yet

    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    const float maxT = *t;
    tinybvh::Ray ray(O, D, maxT);

    inst->bvh.Intersect(ray);

    *t = ray.hit.t;
    *u = ray.hit.u;
    *v = ray.hit.v;
    *primIdx = ray.hit.prim;

    return (ray.hit.t < maxT) ? 1 : 0;
}

int32_t TBVH_GPU_GetNodeCount(TBVH_HANDLE handle)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return 0; // not built yet
    return static_cast<int32_t>(inst->bvh.usedNodes);
}

int32_t TBVH_GPU_GetTriangleCount(TBVH_HANDLE handle)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return 0; // not built yet
    return static_cast<int32_t>(inst->bvh.triCount);
}

void TBVH_GPU_GetNodes(TBVH_HANDLE handle, float* nodeData)
{
    if (!handle || !nodeData) return;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return; // not built yet
    // Each BVHNode is 64 bytes = 16 floats (4 x float4)
    uint32_t nodeCount = inst->bvh.usedNodes;
    std::memcpy(nodeData, inst->bvh.bvhNode, nodeCount * 16 * sizeof(float));
}

void TBVH_GPU_GetPrimitiveIndices(TBVH_HANDLE handle, uint32_t* primIndices)
{
    if (!handle || !primIndices) return;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return; // not built yet
    uint32_t triCount = inst->bvh.triCount;
    if (inst->bvh.bvh.primIdx)
    {
        std::memcpy(primIndices, inst->bvh.bvh.primIdx, triCount * sizeof(uint32_t));
    }
    else
    {
        // Identity mapping
        for (uint32_t i = 0; i < triCount; i++) primIndices[i] = i;
    }
}

void TBVH_GPU_GetVertices(TBVH_HANDLE handle, float* vertexData)
{
    if (!handle || !vertexData) return;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return; // not built yet
    // 3 float4 per triangle = 12 floats per triangle
    uint32_t triCount = inst->bvh.triCount;
    std::memcpy(vertexData, inst->vertexCopy, triCount * 3 * 4 * sizeof(float));
}

int32_t TBVH_GPU_IsOccluded(TBVH_HANDLE handle,
                            const float* origin, const float* direction, float maxDistance)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return 0;

    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    tinybvh::Ray ray(O, D, maxDistance);

    return inst->bvh.IsOccluded(ray) ? 1 : 0;
}

float TBVH_GPU_SAHCost(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return 0.0f;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return 0.0f;
    return inst->bvh.SAHCost(nodeIdx);
}

void TBVH_GPU_Optimize(TBVH_HANDLE handle, uint32_t iterations, int32_t extreme)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return;
    inst->bvh.Optimize(iterations, extreme != 0);
}

// ── GPU BVH extended build methods ────────────────────────────────

void TBVH_GPU_BuildHQ(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);
    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->triCount = triCount;
    inst->bvh.BuildHQ(inst->vertexCopy, triCount);
}

void TBVH_GPU_BuildIndexed(TBVH_HANDLE handle, const float* vertices,
                           const uint32_t* indices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_GPU_Instance*>(handle);
    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->triCount = triCount;
    inst->bvh.Build(inst->vertexCopy, indices, triCount);
}

// ── 4-wide GPU BVH (LAYOUT_BVH4_GPU) ───────────────────────────

struct TBVH_GPU4_Instance
{
    tinybvh::BVH4_GPU bvh;
    tinybvh::bvhvec4* vertexCopy = nullptr; // must outlive the BVH
    uint32_t triCount = 0;
};

TBVH_HANDLE TBVH_GPU4_Create(void)
{
    auto* inst = new TBVH_GPU4_Instance();
    return static_cast<TBVH_HANDLE>(inst);
}

void TBVH_GPU4_Destroy(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    delete[] inst->vertexCopy;
    delete inst;
}

void TBVH_GPU4_Build(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->triCount = triCount;

    inst->bvh.Build(inst->vertexCopy, triCount);
}

int32_t TBVH_GPU4_Intersect(TBVH_HANDLE handle,
                            const float* origin, const float* direction,
                            float* t, float* u, float* v, uint32_t* primIdx)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    if (inst->bvh.usedBlocks == 0) return 0; // not built yet

    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    const float maxT = *t;
    tinybvh::Ray ray(O, D, maxT);

    inst->bvh.Intersect(ray);

    *t = ray.hit.t;
    *u = ray.hit.u;
    *v = ray.hit.v;
    *primIdx = ray.hit.prim;

    return (ray.hit.t < maxT) ? 1 : 0;
}

int32_t TBVH_GPU4_GetNodeCount(TBVH_HANDLE handle)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    if (inst->bvh.usedBlocks == 0) return 0;
    return static_cast<int32_t>(inst->bvh.usedBlocks);
}

int32_t TBVH_GPU4_GetTriangleCount(TBVH_HANDLE handle)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    if (inst->bvh.usedBlocks == 0) return 0;
    return static_cast<int32_t>(inst->bvh.bvh4.triCount);
}

void TBVH_GPU4_GetNodes(TBVH_HANDLE handle, float* nodeData)
{
    if (!handle || !nodeData) return;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    if (inst->bvh.usedBlocks == 0) return;
    // Each BVHNode is 64 bytes = 4 bvhvec4 = 16 floats per node
    uint32_t nodeCount = inst->bvh.usedBlocks;
    std::memcpy(nodeData, inst->bvh.bvh4Data, nodeCount * 16 * sizeof(float));
}

void TBVH_GPU4_GetPrimitiveIndices(TBVH_HANDLE handle, uint32_t* primIndices)
{
    if (!handle || !primIndices) return;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    if (inst->bvh.usedBlocks == 0) return;
    uint32_t triCount = inst->bvh.bvh4.triCount;
    uint32_t* src = inst->bvh.bvh4.bvh.primIdx;
    if (src)
        std::memcpy(primIndices, src, triCount * sizeof(uint32_t));
    else
        for (uint32_t i = 0; i < triCount; i++) primIndices[i] = i;
}

void TBVH_GPU4_GetVertices(TBVH_HANDLE handle, float* vertexData)
{
    if (!handle || !vertexData) return;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    if (inst->bvh.usedBlocks == 0) return;
    uint32_t triCount = inst->bvh.bvh4.triCount;
    std::memcpy(vertexData, inst->vertexCopy, triCount * 3 * 4 * sizeof(float));
}

int32_t TBVH_GPU4_IsOccluded(TBVH_HANDLE handle,
                             const float* origin, const float* direction, float maxDistance)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    if (inst->bvh.usedBlocks == 0) return 0;

    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    tinybvh::Ray ray(O, D, maxDistance);

    return inst->bvh.IsOccluded(ray) ? 1 : 0;
}

float TBVH_GPU4_SAHCost(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return 0.0f;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    if (inst->bvh.usedBlocks == 0) return 0.0f;
    return inst->bvh.SAHCost(nodeIdx);
}

// ── GPU4 extended build methods ───────────────────────────────────

void TBVH_GPU4_BuildHQ(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->triCount = triCount;
    inst->bvh.BuildHQ(inst->vertexCopy, triCount);
}

void TBVH_GPU4_BuildIndexed(TBVH_HANDLE handle, const float* vertices,
                            const uint32_t* indices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->triCount = triCount;
    inst->bvh.Build(inst->vertexCopy, indices, triCount);
}

int32_t TBVH_GPU4_LeafCount(TBVH_HANDLE handle)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    return inst->bvh.bvh4.LeafCount();
}

void TBVH_GPU4_Optimize(TBVH_HANDLE handle, uint32_t iterations, int32_t extreme)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_GPU4_Instance*>(handle);
    inst->bvh.Optimize(iterations, extreme != 0);
}

// ── VoxelSet ──────────────────────────────────────────────────────

struct TBVH_VS_Instance
{
    tinybvh::VoxelSet vs;
};

TBVH_HANDLE TBVH_VoxelSet_Create(void)
{
    auto* inst = new TBVH_VS_Instance();
    return static_cast<TBVH_HANDLE>(inst);
}

void TBVH_VoxelSet_Destroy(TBVH_HANDLE handle)
{
    if (!handle) return;
    delete static_cast<TBVH_VS_Instance*>(handle);
}

void TBVH_VoxelSet_Set(TBVH_HANDLE handle, uint32_t x, uint32_t y, uint32_t z, uint32_t v)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_VS_Instance*>(handle);
    inst->vs.Set(x, y, z, v);
}

void TBVH_VoxelSet_UpdateTopGrid(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_VS_Instance*>(handle);
    inst->vs.UpdateTopGrid();
}

int32_t TBVH_VoxelSet_Intersect(TBVH_HANDLE handle,
                                const float* origin, const float* direction,
                                float* t, float* u, float* v, uint32_t* primIdx)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_VS_Instance*>(handle);

    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    const float maxT = *t;
    tinybvh::Ray ray(O, D, maxT);

    inst->vs.Intersect(ray);

    *t = ray.hit.t;
    *u = ray.hit.u;
    *v = ray.hit.v;
    *primIdx = ray.hit.prim;
    return (ray.hit.t < maxT) ? 1 : 0;
}

int32_t TBVH_VoxelSet_IsOccluded(TBVH_HANDLE handle,
                                 const float* origin, const float* direction, float maxDistance)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_VS_Instance*>(handle);
    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    tinybvh::Ray ray(O, D, maxDistance);
    return inst->vs.IsOccluded(ray) ? 1 : 0;
}

// ── BLASInstance ──────────────────────────────────────────────────

struct TBVH_BLAS_Instance
{
    tinybvh::BLASInstance blas;
    tinybvh::BVH* blasBVH = nullptr; // keep alive for Update
};

TBVH_HANDLE TBVH_BLASInstance_Create(uint32_t idx)
{
    auto* inst = new TBVH_BLAS_Instance();
    inst->blas = tinybvh::BLASInstance(idx);
    return static_cast<TBVH_HANDLE>(inst);
}

void TBVH_BLASInstance_Destroy(TBVH_HANDLE handle)
{
    if (!handle) return;
    delete static_cast<TBVH_BLAS_Instance*>(handle);
}

void TBVH_BLASInstance_Update(TBVH_HANDLE blasHandle, TBVH_HANDLE bvhHandle)
{
    if (!blasHandle || !bvhHandle) return;
    auto* inst = static_cast<TBVH_BLAS_Instance*>(blasHandle);
    auto* bvhInst = static_cast<TBVH_Instance*>(bvhHandle);
    inst->blasBVH = &bvhInst->bvh;
    inst->blas.Update(inst->blasBVH);
}

void TBVH_BLASInstance_InvertTransform(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_BLAS_Instance*>(handle);
    inst->blas.InvertTransform();
}

void TBVH_BLASInstance_SetTransform(TBVH_HANDLE handle, const float* matrix4x4)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_BLAS_Instance*>(handle);
    std::memcpy(inst->blas.transform.cell, matrix4x4, 16 * sizeof(float));
}

// ── Structure-of-Arrays BVH ───────────────────────────────────────

struct TBVH_SoA_Instance
{
    tinybvh::BVH_SoA bvh;
    tinybvh::bvhvec4* vertexCopy = nullptr;
};

TBVH_HANDLE TBVH_SoA_Create(void)
{
    auto* inst = new TBVH_SoA_Instance();
    return static_cast<TBVH_HANDLE>(inst);
}

void TBVH_SoA_Destroy(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_SoA_Instance*>(handle);
    delete[] inst->vertexCopy;
    delete inst;
}

void TBVH_SoA_Build(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_SoA_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->bvh.Build(inst->vertexCopy, triCount);
}

void TBVH_SoA_ConvertFrom(TBVH_HANDLE handle, TBVH_HANDLE sourceHandle)
{
    if (!handle || !sourceHandle) return;
    auto* inst = static_cast<TBVH_SoA_Instance*>(handle);
    auto* src = static_cast<TBVH_Instance*>(sourceHandle);
    inst->bvh.ConvertFrom(src->bvh, true);
}

int32_t TBVH_SoA_Intersect(TBVH_HANDLE handle,
                           const float* origin, const float* direction,
                           float* t, float* u, float* v, uint32_t* primIdx)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_SoA_Instance*>(handle);
    if (inst->bvh.bvhNode == 0) return 0;

    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    const float maxT = *t;
    tinybvh::Ray ray(O, D, maxT);

    inst->bvh.Intersect(ray);

    *t = ray.hit.t;
    *u = ray.hit.u;
    *v = ray.hit.v;
    *primIdx = ray.hit.prim;
    return (ray.hit.t < maxT) ? 1 : 0;
}

int32_t TBVH_SoA_IsOccluded(TBVH_HANDLE handle,
                            const float* origin, const float* direction, float maxDistance)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_SoA_Instance*>(handle);
    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    tinybvh::Ray ray(O, D, maxDistance);
    return inst->bvh.IsOccluded(ray) ? 1 : 0;
}

float TBVH_SoA_SAHCost(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return 0.0f;
    auto* inst = static_cast<TBVH_SoA_Instance*>(handle);
    return inst->bvh.SAHCost(nodeIdx);
}

void TBVH_SoA_Optimize(TBVH_HANDLE handle, uint32_t iterations, int32_t extreme)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_SoA_Instance*>(handle);
    inst->bvh.Optimize(iterations, extreme != 0);
}

// ── Verbose BVH ───────────────────────────────────────────────────

struct TBVH_Verbose_Instance
{
    tinybvh::BVH_Verbose bvh;
    tinybvh::bvhvec4* vertexCopy = nullptr;
};

TBVH_HANDLE TBVH_Verbose_Create(void)
{
    auto* inst = new TBVH_Verbose_Instance();
    return static_cast<TBVH_HANDLE>(inst);
}

void TBVH_Verbose_Destroy(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Verbose_Instance*>(handle);
    delete[] inst->vertexCopy;
    delete inst;
}

void TBVH_Verbose_ConvertFrom(TBVH_HANDLE handle, TBVH_HANDLE sourceHandle)
{
    if (!handle || !sourceHandle) return;
    auto* inst = static_cast<TBVH_Verbose_Instance*>(handle);
    auto* src = static_cast<TBVH_Instance*>(sourceHandle);
    inst->bvh.ConvertFrom(src->bvh, true);
}

void TBVH_Verbose_Build(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Verbose_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    // BVH_Verbose uses BVH internally; we construct BVH first then convert
    tinybvh::BVH tempBvh;
    tempBvh.Build(inst->vertexCopy, triCount);
    inst->bvh.ConvertFrom(tempBvh, true);
}

int32_t TBVH_Verbose_NodeCount(TBVH_HANDLE handle)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Verbose_Instance*>(handle);
    return inst->bvh.NodeCount();
}

float TBVH_Verbose_SAHCost(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return 0.0f;
    auto* inst = static_cast<TBVH_Verbose_Instance*>(handle);
    return inst->bvh.SAHCost(nodeIdx);
}

void TBVH_Verbose_Refit(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Verbose_Instance*>(handle);
    inst->bvh.Refit(nodeIdx);
}

void TBVH_Verbose_Optimize(TBVH_HANDLE handle, uint32_t iterations, int32_t extreme, int32_t stochastic)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Verbose_Instance*>(handle);
    inst->bvh.Optimize(iterations, extreme != 0, stochastic != 0);
}

void TBVH_Verbose_Compact(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Verbose_Instance*>(handle);
    inst->bvh.Compact();
}

// ── 4-wide CPU BVH ────────────────────────────────────────────────

struct TBVH_4CPU_Instance
{
    tinybvh::BVH4_CPU bvh;
    tinybvh::bvhvec4* vertexCopy = nullptr;
};

TBVH_HANDLE TBVH_4CPU_Create(void)
{
    auto* inst = new TBVH_4CPU_Instance();
    return static_cast<TBVH_HANDLE>(inst);
}

void TBVH_4CPU_Destroy(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_4CPU_Instance*>(handle);
    delete[] inst->vertexCopy;
    delete inst;
}

void TBVH_4CPU_Build(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_4CPU_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->bvh.Build(inst->vertexCopy, triCount);
}

void TBVH_4CPU_BuildHQ(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_4CPU_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->bvh.BuildHQ(inst->vertexCopy, triCount);
}

void TBVH_4CPU_ConvertFrom(TBVH_HANDLE handle, TBVH_HANDLE sourceHandle)
{
    if (!handle || !sourceHandle) return;
    auto* inst = static_cast<TBVH_4CPU_Instance*>(handle);
    auto* src = static_cast<TBVH_Instance*>(sourceHandle);
    tinybvh::MBVH<4> mbvh;
    mbvh.ConvertFrom(src->bvh, true);
    inst->bvh.ConvertFrom(mbvh);
}

int32_t TBVH_4CPU_Intersect(TBVH_HANDLE handle,
                            const float* origin, const float* direction,
                            float* t, float* u, float* v, uint32_t* primIdx)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_4CPU_Instance*>(handle);
    if (inst->bvh.bvh4Data == 0) return 0;

    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    const float maxT = *t;
    tinybvh::Ray ray(O, D, maxT);

    inst->bvh.Intersect(ray);

    *t = ray.hit.t;
    *u = ray.hit.u;
    *v = ray.hit.v;
    *primIdx = ray.hit.prim;
    return (ray.hit.t < maxT) ? 1 : 0;
}

int32_t TBVH_4CPU_IsOccluded(TBVH_HANDLE handle,
                             const float* origin, const float* direction, float maxDistance)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_4CPU_Instance*>(handle);
    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    tinybvh::Ray ray(O, D, maxDistance);
    return inst->bvh.IsOccluded(ray) ? 1 : 0;
}

float TBVH_4CPU_SAHCost(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return 0.0f;
    auto* inst = static_cast<TBVH_4CPU_Instance*>(handle);
    return inst->bvh.SAHCost(nodeIdx);
}

void TBVH_4CPU_Optimize(TBVH_HANDLE handle, uint32_t iterations, int32_t extreme)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_4CPU_Instance*>(handle);
    inst->bvh.Optimize(iterations, extreme != 0);
}

void TBVH_4CPU_Refit(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_4CPU_Instance*>(handle);
    inst->bvh.Refit();
}

int32_t TBVH_4CPU_Save(TBVH_HANDLE handle, const char* filename)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_4CPU_Instance*>(handle);
    inst->bvh.Save(filename);
    return 1;
}

int32_t TBVH_4CPU_Load(TBVH_HANDLE handle, const char* filename,
                       const float* vertices, uint32_t triCount)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_4CPU_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));

    return inst->bvh.Load(filename, triCount) ? 1 : 0;
}

// ── 8-wide CPU BVH ────────────────────────────────────────────────

struct TBVH_8CPU_Instance
{
    tinybvh::BVH8_CPU bvh;
    tinybvh::bvhvec4* vertexCopy = nullptr;
};

TBVH_HANDLE TBVH_8CPU_Create(void)
{
    auto* inst = new TBVH_8CPU_Instance();
    return static_cast<TBVH_HANDLE>(inst);
}

void TBVH_8CPU_Destroy(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_8CPU_Instance*>(handle);
    delete[] inst->vertexCopy;
    delete inst;
}

void TBVH_8CPU_Build(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_8CPU_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->bvh.Build(inst->vertexCopy, triCount);
}

void TBVH_8CPU_BuildHQ(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_8CPU_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->bvh.BuildHQ(inst->vertexCopy, triCount);
}

void TBVH_8CPU_ConvertFrom(TBVH_HANDLE handle, TBVH_HANDLE sourceHandle)
{
    if (!handle || !sourceHandle) return;
    auto* inst = static_cast<TBVH_8CPU_Instance*>(handle);
    auto* src = static_cast<TBVH_Instance*>(sourceHandle);
    tinybvh::MBVH<8> mbvh;
    mbvh.ConvertFrom(src->bvh, true);
    inst->bvh.ConvertFrom(mbvh);
}

int32_t TBVH_8CPU_Intersect(TBVH_HANDLE handle,
                            const float* origin, const float* direction,
                            float* t, float* u, float* v, uint32_t* primIdx)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_8CPU_Instance*>(handle);
    if (inst->bvh.bvh8Data == 0) return 0;

    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    const float maxT = *t;
    tinybvh::Ray ray(O, D, maxT);

    inst->bvh.Intersect(ray);

    *t = ray.hit.t;
    *u = ray.hit.u;
    *v = ray.hit.v;
    *primIdx = ray.hit.prim;
    return (ray.hit.t < maxT) ? 1 : 0;
}

int32_t TBVH_8CPU_IsOccluded(TBVH_HANDLE handle,
                             const float* origin, const float* direction, float maxDistance)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_8CPU_Instance*>(handle);
    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    tinybvh::Ray ray(O, D, maxDistance);
    return inst->bvh.IsOccluded(ray) ? 1 : 0;
}

float TBVH_8CPU_SAHCost(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return 0.0f;
    auto* inst = static_cast<TBVH_8CPU_Instance*>(handle);
    return inst->bvh.SAHCost(nodeIdx);
}

void TBVH_8CPU_Optimize(TBVH_HANDLE handle, uint32_t iterations, int32_t extreme)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_8CPU_Instance*>(handle);
    inst->bvh.Optimize(iterations, extreme != 0);
}

void TBVH_8CPU_Refit(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_8CPU_Instance*>(handle);
    inst->bvh.Refit();
}

int32_t TBVH_8CPU_Save(TBVH_HANDLE handle, const char* filename)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_8CPU_Instance*>(handle);
    inst->bvh.Save(filename);
    return 1;
}

int32_t TBVH_8CPU_Load(TBVH_HANDLE handle, const char* filename,
                       const float* vertices, uint32_t triCount)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_8CPU_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));

    return inst->bvh.Load(filename, triCount) ? 1 : 0;
}

// ── 8-wide Compressed Wide BVH for GPU ────────────────────────────

struct TBVH_8CWBVH_Instance
{
    tinybvh::BVH8_CWBVH bvh;
    tinybvh::bvhvec4* vertexCopy = nullptr;
};

TBVH_HANDLE TBVH_8CWBVH_Create(void)
{
    auto* inst = new TBVH_8CWBVH_Instance();
    return static_cast<TBVH_HANDLE>(inst);
}

void TBVH_8CWBVH_Destroy(TBVH_HANDLE handle)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_8CWBVH_Instance*>(handle);
    delete[] inst->vertexCopy;
    delete inst;
}

void TBVH_8CWBVH_Build(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_8CWBVH_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->bvh.Build(inst->vertexCopy, triCount);
}

void TBVH_8CWBVH_BuildHQ(TBVH_HANDLE handle, const float* vertices, uint32_t triCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_8CWBVH_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));
    inst->bvh.BuildHQ(inst->vertexCopy, triCount);
}

int32_t TBVH_8CWBVH_Intersect(TBVH_HANDLE handle,
                              const float* origin, const float* direction,
                              float* t, float* u, float* v, uint32_t* primIdx)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_8CWBVH_Instance*>(handle);

    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    const float maxT = *t;
    tinybvh::Ray ray(O, D, maxT);

    inst->bvh.Intersect(ray);

    *t = ray.hit.t;
    *u = ray.hit.u;
    *v = ray.hit.v;
    *primIdx = ray.hit.prim;
    return (ray.hit.t < maxT) ? 1 : 0;
}

int32_t TBVH_8CWBVH_IsOccluded(TBVH_HANDLE handle,
                               const float* origin, const float* direction, float maxDistance)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_8CWBVH_Instance*>(handle);
    tinybvh::bvhvec3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhvec3 D(direction[0], direction[1], direction[2]);
    tinybvh::Ray ray(O, D, maxDistance);
    return inst->bvh.IsOccluded(ray) ? 1 : 0;
}

float TBVH_8CWBVH_SAHCost(TBVH_HANDLE handle, uint32_t nodeIdx)
{
    if (!handle) return 0.0f;
    auto* inst = static_cast<TBVH_8CWBVH_Instance*>(handle);
    return inst->bvh.SAHCost(nodeIdx);
}

void TBVH_8CWBVH_Optimize(TBVH_HANDLE handle, uint32_t iterations, int32_t extreme)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_8CWBVH_Instance*>(handle);
    inst->bvh.Optimize(iterations, extreme != 0);
}

int32_t TBVH_8CWBVH_Save(TBVH_HANDLE handle, const char* filename)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_8CWBVH_Instance*>(handle);
    inst->bvh.Save(filename);
    return 1;
}

int32_t TBVH_8CWBVH_Load(TBVH_HANDLE handle, const char* filename,
                         const float* vertices, uint32_t triCount)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_8CWBVH_Instance*>(handle);

    uint32_t floatCount = triCount * 3 * 4;
    delete[] inst->vertexCopy;
    inst->vertexCopy = new tinybvh::bvhvec4[triCount * 3];
    std::memcpy(inst->vertexCopy, vertices, floatCount * sizeof(float));

    return inst->bvh.Load(filename, triCount) ? 1 : 0;
}

// ── BVH_Double (double precision) ───────────────────────────────

struct TBVH_Double_Instance
{
    tinybvh::BVH_Double bvh;
};

TBVH_HANDLE TBVH_Double_Create(void)
{
    auto* inst = new TBVH_Double_Instance();
    return static_cast<TBVH_HANDLE>(inst);
}

void TBVH_Double_Destroy(TBVH_HANDLE handle)
{
    if (!handle) return;
    delete static_cast<TBVH_Double_Instance*>(handle);
}

void TBVH_Double_Build(TBVH_HANDLE handle, const double* vertices, uint64_t primCount)
{
    if (!handle) return;
    auto* inst = static_cast<TBVH_Double_Instance*>(handle);
    inst->bvh.Build(reinterpret_cast<const tinybvh::bvhdbl3*>(vertices), primCount);
}

int32_t TBVH_Double_Intersect(TBVH_HANDLE handle,
                              const double* origin, const double* direction,
                              double* t, double* u, double* v, uint64_t* primIdx)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Double_Instance*>(handle);

    tinybvh::bvhdbl3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhdbl3 D(direction[0], direction[1], direction[2]);
    tinybvh::RayEx ray(O, D, *t);

    int32_t hit = inst->bvh.Intersect(ray);

    *t = ray.hit.t;
    *u = ray.hit.u;
    *v = ray.hit.v;
    *primIdx = ray.hit.prim;
    return hit;
}

int32_t TBVH_Double_IsOccluded(TBVH_HANDLE handle,
                               const double* origin, const double* direction,
                               double maxDistance)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_Double_Instance*>(handle);

    tinybvh::bvhdbl3 O(origin[0], origin[1], origin[2]);
    tinybvh::bvhdbl3 D(direction[0], direction[1], direction[2]);
    tinybvh::RayEx ray(O, D, maxDistance);

    return inst->bvh.IsOccluded(ray) ? 1 : 0;
}

double TBVH_Double_SAHCost(TBVH_HANDLE handle, uint64_t nodeIdx)
{
    if (!handle) return 0.0;
    auto* inst = static_cast<TBVH_Double_Instance*>(handle);
    return inst->bvh.SAHCost(nodeIdx);
}

// ── JobSystem ─────────────────────────────────────────────────────

struct TBVH_JS_Instance
{
    tinybvh::JobSystem js;
};

TBVH_HANDLE TBVH_JobSystem_Create(void)
{
    auto* inst = new TBVH_JS_Instance();
    return static_cast<TBVH_HANDLE>(inst);
}

void TBVH_JobSystem_Destroy(TBVH_HANDLE handle)
{
    if (!handle) return;
    delete static_cast<TBVH_JS_Instance*>(handle);
}

int32_t TBVH_JobSystem_IsBusy(TBVH_HANDLE handle)
{
    if (!handle) return 0;
    auto* inst = static_cast<TBVH_JS_Instance*>(handle);
    return inst->js.IsBusy() ? 1 : 0;
}
