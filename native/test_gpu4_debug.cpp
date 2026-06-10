#define TINYBVH_IMPLEMENTATION
#include "../external/tinybvh/tiny_bvh.h"
#include <cstdio>

static uint32_t fu32(float v) { union { float f; uint32_t u; } uf; uf.f = v; return uf.u; }

int main() {
    printf("Starting...\n"); fflush(stdout);
    const float unitCube[] = {
        -1,-1,1,0,  1,-1,1,0, -1,1,1,0,
        1,-1,1,0,  1,1,1,0, -1,1,1,0,
        1,-1,-1,0, -1,-1,-1,0, 1,1,-1,0,
        -1,-1,-1,0, -1,1,-1,0, 1,1,-1,0,
        1,-1,1,0, 1,-1,-1,0, 1,1,1,0,
        1,-1,-1,0, 1,1,-1,0, 1,1,1,0,
        -1,-1,-1,0, -1,-1,1,0, -1,1,-1,0,
        -1,-1,1,0, -1,1,1,0, -1,1,-1,0,
        -1,1,1,0, 1,1,1,0, -1,1,-1,0,
        1,1,1,0, 1,1,-1,0, -1,1,-1,0,
        -1,-1,-1,0, 1,-1,-1,0, -1,-1,1,0,
        1,-1,-1,0, 1,-1,1,0, -1,-1,1,0
    };

    tinybvh::BVH4_GPU bvh;
    printf("Building BVH...\n"); fflush(stdout);
    bvh.Build((const tinybvh::bvhvec4*)unitCube, 12);
    printf("Build done. usedBlocks=%u allocatedBlocks=%u\n", bvh.usedBlocks, bvh.allocatedBlocks); fflush(stdout);

    // MBVH root info FIRST
    printf("\n--- MBVH root ---\n");
    auto& root = bvh.bvh4.mbvhNode[0];
    printf("  triCount=%d child=[%u,%u,%u,%u] isLeaf=%d\n", 
           (int)root.triCount, root.child[0], root.child[1], root.child[2], root.child[3], (int)root.isLeaf());
    for (int i = 0; i < 4; i++) if (root.child[i]) {
        auto& ch = bvh.bvh4.mbvhNode[root.child[i]];
        printf("  child[%d] triCount=%d isLeaf=%d child=[%u,%u,%u,%u]\n", i,
               (int)ch.triCount, (int)ch.isLeaf(), ch.child[0], ch.child[1], ch.child[2], ch.child[3]);
    }

    // Inspect root node data with uint32 decoding
    printf("\n--- Root node (offset 0) ---\n");
    for (int i = 0; i < 4; i++) {
        const tinybvh::bvhvec4& d = bvh.bvh4Data[i];
        printf("  data[%d] = (%12g, %12g, %12g, %12g)\n", i, d.x, d.y, d.z, d.w);
        printf("         u = (0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
               fu32(d.x), fu32(d.y), fu32(d.z), fu32(d.w));
    }

    // Child info decoding
    uint32_t c0 = fu32(bvh.bvh4Data[3].x);
    uint32_t c1 = fu32(bvh.bvh4Data[3].y);
    uint32_t c2 = fu32(bvh.bvh4Data[3].z);
    uint32_t c3 = fu32(bvh.bvh4Data[3].w);
    printf("\n--- Child Info ---\n");
    auto dec = [](uint32_t ci, int idx) {
        printf("  c%d=0x%08x leaf=%d offset=%d count=%d\n", idx, ci, (ci>>31)&1, ci&0xFFFF, (ci>>16)&0x7FFF);
    };
    dec(c0, 0); dec(c1, 1); dec(c2, 2); dec(c3, 3);

    // Now test actual intersection
    printf("\n--- Intersection test ---\n");
    tinybvh::Ray ray(tinybvh::bvhvec3(0,0,5), tinybvh::bvhvec3(0,0,-1));
    int hit = bvh.Intersect(ray);
    printf("  Hit ray: intersect=%s hit.t=%g\n", hit ? "HIT" : "MISS", ray.hit.t);

    tinybvh::Ray ray2(tinybvh::bvhvec3(0,0,5), tinybvh::bvhvec3(1,0,-1));
    int hit2 = bvh.Intersect(ray2);
    printf("  Diagonal ray: intersect=%s hit.t=%g\n", hit2 ? "HIT" : "MISS", ray2.hit.t);

    tinybvh::Ray ray3(tinybvh::bvhvec3(10,0,0), tinybvh::bvhvec3(0,0,-1));
    int hit3 = bvh.Intersect(ray3);
    printf("  Miss ray: intersect=%s\n", hit3 ? "HIT" : "MISS");

    printf("\nDone.\n");
    return 0;
}
