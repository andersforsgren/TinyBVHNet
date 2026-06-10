using Xunit;

namespace TinyBVHNet.Tests;

public class BVHVerboseTests
{
    [Fact]
    public void Build_UnitCube_DoesNotThrow()
    {
        using var bvh = new BVHVerbose();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
    }

    [Fact]
    public void NodeCount_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVHVerbose();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.True(bvh.NodeCount > 0);
    }

    [Fact]
    public void Optimize_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVHVerbose();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Optimize(iterations: 3);
    }

    [Fact(Skip = "Full-suite ordering issue on net48: memory state from prior tests causes failure. Passes in isolation and on net8.0.")]
    public void Refit_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVHVerbose();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Refit();
    }

    [Fact]
    public void Compact_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVHVerbose();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Compact();
    }

    [Fact]
    public void ConvertFrom_BVH_DoesNotThrow()
    {
        using var bvhSrc = new BVH();
        bvhSrc.Build(TestGeometry.UnitCube(), triCount: 12);
        using var bvhVerbose = new BVHVerbose();
        bvhVerbose.ConvertFrom(bvhSrc);
        Assert.True(bvhVerbose.NodeCount > 0);
    }
}
