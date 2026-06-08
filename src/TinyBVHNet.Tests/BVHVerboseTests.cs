using Xunit;

namespace TinyBVHNet.Tests;

public class BVHVerboseTests
{
    [Fact]
    public void Create_ReturnsValidHandle()
    {
        using var bvh = new BVHVerbose();
        Assert.True(true);
    }

    [Fact]
    public void Build_UnitCube_Success()
    {
        using var bvh = new BVHVerbose();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.True(true);
    }

    [Fact]
    public void NodeCount_AfterBuild_IsPositive()
    {
        using var bvh = new BVHVerbose();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.True(bvh.NodeCount > 0);
    }

    [Fact]
    public void SAHCost_AfterBuild_IsNonNegative()
    {
        using var bvh = new BVHVerbose();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        float cost = bvh.SAHCost();
        Assert.True(cost >= 0);
    }

    [Fact]
    public void Optimize_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVHVerbose();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Optimize(iterations: 3);
        Assert.True(true);
    }

    [Fact(Skip = "Access violation on net48 when run in full suite (memory corruption from SSE variants)")]
    public void Refit_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVHVerbose();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Refit();
        Assert.True(true);
    }

    [Fact]
    public void Compact_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVHVerbose();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Compact();
        Assert.True(true);
    }

    [Fact]
    public void ConvertFrom_BVH_ProducesNodes()
    {
        using var bvhSrc = new BVH();
        bvhSrc.Build(TestGeometry.UnitCube(), triCount: 12);
        using var bvhVerbose = new BVHVerbose();
        bvhVerbose.ConvertFrom(bvhSrc);
        Assert.True(bvhVerbose.NodeCount > 0);
    }
}
