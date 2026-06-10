using Xunit;
using System.Numerics;

namespace TinyBVHNet.Tests;

/// <summary>BVH8_CWBVH type-specific tests (not covered by IBVH interface contract).</summary>
public class BVH8CWBVHTests
{
    [Fact]
    public void BuildHQ_StillIntersects()
    {
        using var bvh = new BVH8CWBVH();
        bvh.BuildHQ(TestGeometry.UnitCube(), triCount: 12);
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }

    [Fact]
    public void Optimize_AfterBuild_StillIntersects()
    {
        using var bvh = new BVH8CWBVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Optimize(iterations: 3);
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }
}
