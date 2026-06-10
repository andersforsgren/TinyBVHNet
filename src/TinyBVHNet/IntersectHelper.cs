using System;
using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Shared helpers for the repeated Intersect/IsOccluded P/Invoke pattern
/// used by all IBVH implementations.
/// </summary>
internal static class IntersectHelper
{
    internal unsafe delegate int IntersectDelegate(IntPtr bvh, float* origin, float* direction,
        ref float t, out float u, out float v, out uint primIdx);

    internal unsafe delegate int IsOccludedDelegate(IntPtr bvh, float* origin, float* direction,
        float maxDistance);

    internal static unsafe IntersectionResult? Intersect(IntPtr handle, Vector3 origin, Vector3 direction,
        float maxDistance, IntersectDelegate func)
    {
        float t = maxDistance;
        int hit = func(handle, (float*)&origin, (float*)&direction, ref t, out float u, out float v, out uint primIdx);
        if (hit == 0) return null;
        return new IntersectionResult { Distance = t, U = u, V = v, PrimitiveIndex = primIdx };
    }

    internal static unsafe bool IsOccluded(IntPtr handle, Vector3 origin, Vector3 direction,
        float maxDistance, IsOccludedDelegate func)
    {
        return func(handle, (float*)&origin, (float*)&direction, maxDistance) != 0;
    }
}
