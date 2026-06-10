#define TINYBVH_IMPLEMENTATION
#include "../external/tinybvh/tiny_bvh.h"
#include <cstdio>
#include <cmath>

int main() {
    // Unit cube: 12 triangles, 36 vertices, each 4 floats = 144 floats
    const float unitCube[] = {
        // Front face (z=1)
        -1,-1,1,0,  1,-1,1,0, -1,1,1,0,  // tri 0
        1,-1,1,0,  1,1,1,0, -1,1,1,0,    // tri 1
        // Back face (z=-1)
        1,-1,-1,0, -1,-1,-1,0, 1,1,-1,0,  // tri 2
        -1,-1,-1,0, -1,1,-1,0, 1,1,-1,0,  // tri 3
        // Right face (x=1)
        1,-1,1,0, 1,-1,-1,0, 1,1,1,0,    // tri 4
        1,-1,-1,0, 1,1,-1,0, 1,1,1,0,    // tri 5
        // Left face (x=-1)
        -1,-1,-1,0, -1,-1,1,0, -1,1,-1,0, // tri 6
        -1,-1,1,0, -1,1,1,0, -1,1,-1,0,  // tri 7
        // Top face (y=1)
        -1,1,1,0, 1,1,1,0, -1,1,-1,0,    // tri 8
        1,1,1,0, 1,1,-1,0, -1,1,-1,0,    // tri 9
        // Bottom face (y=-1)
        -1,-1,-1,0, 1,-1,-1,0, -1,-1,1,0, // tri 10
        1,-1,-1,0, 1,-1,1,0, -1,-1,1,0   // tri 11
    };

    tinybvh::BVH4_GPU bvh;
    bvh.Build((const tinybvh::bvhvec4*)unitCube, 12);
    printf("Build done. usedBlocks=%u\n", bvh.usedBlocks);

    // Test 1: Hit from above
    {
        tinybvh::Ray ray(tinybvh::bvhvec3(0,0,5), tinybvh::bvhvec3(0,0,-1));
        bvh.Intersect(ray);
        printf("Hit test (0,0,5)->(0,0,-1): hit.t=%g prim=%u u=%g v=%g\n", 
               ray.hit.t, ray.hit.prim, ray.hit.u, ray.hit.v);
        printf("  Result: %s\n", ray.hit.t < 1e30f ? "HIT" : "MISS");
    }

    // Test 2: Hit from above with shorter maxT
    {
        tinybvh::Ray ray(tinybvh::bvhvec3(0,0,5), tinybvh::bvhvec3(0,0,-1), 10.f);
        bvh.Intersect(ray);
        printf("Hit test maxT=10: hit.t=%g\n", ray.hit.t);
        printf("  Result: %s\n", ray.hit.t < 10.f ? "HIT" : "MISS");
    }

    // Test 3: Miss
    {
        tinybvh::Ray ray(tinybvh::bvhvec3(10,10,10), tinybvh::bvhvec3(0,0,1));
        bvh.Intersect(ray);
        float origT = ray.hit.t; // save
        printf("Miss test (10,10,10)->(0,0,1): hit.t=%g\n", ray.hit.t);
        printf("  Result: %s\n", ray.hit.t < 1e30f ? "HIT (BAD!)" : "MISS");
    }

    // Test 4: IsOccluded
    {
        tinybvh::Ray ray(tinybvh::bvhvec3(0,0,5), tinybvh::bvhvec3(0,0,-1));
        bool occ = bvh.IsOccluded(ray);
        printf("IsOccluded (0,0,5)->(0,0,-1): %s\n", occ ? "TRUE" : "FALSE");
    }

    // Test 5: Non-zero direction
    {
        tinybvh::Ray ray(tinybvh::bvhvec3(0,0,5), tinybvh::bvhvec3(0.1f, 0, -1));
        bvh.Intersect(ray);
        printf("Non-zero dir (0,0,5)->(0.1,0,-1): hit.t=%g prim=%u\n", ray.hit.t, ray.hit.prim);
        printf("  Result: %s\n", ray.hit.t < 1e30f ? "HIT" : "MISS");
    }

    return 0;
}
