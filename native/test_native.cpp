// Minimal native test for BVH4_CPU and BVH8_CPU intersect
#define TINYBVH_IMPLEMENTATION
#include "../external/tinybvh/tiny_bvh.h"
#include <cstdio>
#include <cstring>

int main(int argc, char* argv[])
{
    const char* test = argc > 1 ? argv[1] : "all";
    printf("=== Native Test: %s ===\n\n", test);

    // Single triangle
    tinybvh::bvhvec4 vertices[3] = {
        tinybvh::bvhvec4(0, 0, 0, 0),
        tinybvh::bvhvec4(1, 0, 0, 0),
        tinybvh::bvhvec4(0, 1, 0, 0),
    };

    if (strcmp(test, "bvh4") == 0 || strcmp(test, "all") == 0)
    {
        printf("--- BVH4_CPU ---\n"); fflush(stdout);
        printf("Creating BVH4...\n"); fflush(stdout);
        tinybvh::BVH4_CPU bvh4;
        printf("Building...\n"); fflush(stdout);
        bvh4.Build(vertices, 1);
        printf("Build OK, usedBlocks=%u\n", bvh4.usedBlocks); fflush(stdout);

        printf("Creating ray...\n"); fflush(stdout);
        tinybvh::Ray ray(tinybvh::bvhvec3(0.25f, 0.25f, -1.0f),
                         tinybvh::bvhvec3(0.0f, 0.0f, 1.0f));
        printf("Intersecting...\n"); fflush(stdout);
        int hit = bvh4.Intersect(ray);
        printf("Intersect(hit):  hit=%d t=%.3f u=%.3f v=%.3f prim=%u\n",
            hit, ray.hit.t, ray.hit.u, ray.hit.v, ray.hit.prim); fflush(stdout);

        printf("Creating zero-dir miss ray...\n"); fflush(stdout);
        tinybvh::Ray ray2(tinybvh::bvhvec3(2.0f, 2.0f, -1.0f),
                          tinybvh::bvhvec3(0.0f, 0.0f, 1.0f));
        printf("Ray2 created. rD=(%.3f,%.3f,%.3f). Intersecting...\n", ray2.rD.x, ray2.rD.y, ray2.rD.z); fflush(stdout);
        printf("About to call Intersect...\n"); fflush(stdout);
        hit = bvh4.Intersect(ray2);
        printf("Intersect returned: hit=%d t=%.3f\n", hit, ray2.hit.t); fflush(stdout);
        printf("BVH4_CPU PASSED - about to destroy.\n"); fflush(stdout);
    }

    if (strcmp(test, "bvh8") == 0 || strcmp(test, "all") == 0)
    {
        printf("--- BVH8_CPU ---\n");
        tinybvh::BVH8_CPU bvh8;
        bvh8.Build(vertices, 1);
        printf("Build OK, usedBlocks=%u\n", bvh8.usedBlocks);

        tinybvh::Ray ray(tinybvh::bvhvec3(0.25f, 0.25f, -1.0f),
                         tinybvh::bvhvec3(0.0f, 0.0f, 1.0f));
        int hit = bvh8.Intersect(ray);
        printf("Intersect(hit):  hit=%d t=%.3f u=%.3f v=%.3f prim=%u\n",
            hit, ray.hit.t, ray.hit.u, ray.hit.v, ray.hit.prim);

        tinybvh::Ray ray2(tinybvh::bvhvec3(2.0f, 2.0f, -1.0f),
                          tinybvh::bvhvec3(0.0f, 0.0f, 1.0f));
        hit = bvh8.Intersect(ray2);
        printf("Intersect(miss): hit=%d t=%.3f\n", hit, ray2.hit.t);
        printf("BVH8_CPU PASSED.\n\n");
    }

    printf("=== DONE ===\n");
    return 0;
}
