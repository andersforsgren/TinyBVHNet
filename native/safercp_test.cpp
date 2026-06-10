#define TINYBVH_IMPLEMENTATION
#include "../external/tinybvh/tiny_bvh.h"
#include <cstdio>
#include <cmath>
int main() {
    float r0 = tinybvh::tinybvh_safercp(0.0f);
    float r1 = tinybvh::tinybvh_safercp(1.0f);
    float rm = tinybvh::tinybvh_safercp(-0.0f);
    printf("safercp(0)  = %g  inf=%d  nan=%d\n", r0, (int)std::isinf(r0), (int)std::isnan(r0));
    printf("safercp(-0) = %g  inf=%d  nan=%d\n", rm, (int)std::isinf(rm), (int)std::isnan(rm));
    printf("safercp(1)  = %g\n", r1);
    printf("BVH_FAR     = %g\n", (float)BVH_FAR);
    return 0;
}
