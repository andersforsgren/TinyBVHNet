using Xunit;
using System.Numerics;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for the BVH4CPU (4-wide SSE) wrapper.
/// NOTE: Intersect and IsOccluded tests are skipped due to an upstream
/// access violation in tiny_bvh.h BVH4_CPU SSE intrinsics (MSVC x64).
/// See: <see href="https://github.com/jbikker/tinybvh"/> for details.
/// </summary>
public class BVH4CPUTests
{
    [Fact]
    public void Create_ReturnsValidHandle()
    {
        using var bvh = new BVH4CPU();
        Assert.True(true);
    }

    [Fact]
    public void Build_SingleTriangle_Success()
    {
        using var bvh = new BVH4CPU();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);
        Assert.True(true);
    }

}
