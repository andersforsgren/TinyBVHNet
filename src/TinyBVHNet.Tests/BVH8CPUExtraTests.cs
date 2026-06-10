using Xunit;
using System.Numerics;

namespace TinyBVHNet.Tests;

/// <summary>BVH8CPU type-specific extra tests (not covered by IBVH interface contract).</summary>
public class BVH8CPUExtraTests
{
    [Fact]
    public void Optimize_AfterBuild_StillIntersects()
    {
        using var bvh = new BVH8CPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Optimize(iterations: 5);
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }

    [Fact]
    public void BuildHQ_StillIntersects()
    {
        using var bvh = new BVH8CPU();
        bvh.BuildHQ(TestGeometry.UnitCube(), triCount: 12);
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }

    [Fact]
    public void ConvertFrom_BVH_StillIntersects()
    {
        using var bvhSrc = new BVH();
        bvhSrc.Build(TestGeometry.UnitCube(), triCount: 12);
        using var bvh = new BVH8CPU();
        bvh.ConvertFrom(bvhSrc);
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }

    [Fact]
    public void Refit_AfterBuild_StillIntersects()
    {
        using var bvh = new BVH8CPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Refit();
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }
}
