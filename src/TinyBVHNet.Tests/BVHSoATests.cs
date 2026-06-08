using Xunit;
using System.Numerics;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for the BVH_SoA (Structure-of-Arrays) wrapper.
/// </summary>
public class BVHSoATests
{
    [Fact]
    public void Create_ReturnsValidHandle()
    {
        using var bvh = new BVHSoA();
        Assert.True(true);
    }

    [Fact]
    public void Build_UnitCube_Success()
    {
        using var bvh = new BVHSoA();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.True(true);
    }

    [Fact]
    public void Intersect_HitsUnitCube_FromAbove()
    {
        using var bvh = new BVHSoA();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
        Assert.True(result.Value.Distance > 0);
    }

    [Fact]
    public void Intersect_MissesUnitCube_ShootingAway()
    {
        using var bvh = new BVHSoA();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, 1));
        Assert.Null(result);
    }

    [Fact]
    public void IsOccluded_Hit_ReturnsTrue()
    {
        using var bvh = new BVHSoA();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bool occluded = bvh.IsOccluded(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.True(occluded);
    }

    [Fact]
    public void IsOccluded_Miss_ReturnsFalse()
    {
        using var bvh = new BVHSoA();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bool occluded = bvh.IsOccluded(new Vector3(0, 0, 5), new Vector3(0, 0, 1));
        Assert.False(occluded);
    }

    [Fact]
    public void SAHCost_ReturnsNonNegative()
    {
        using var bvh = new BVHSoA();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        float cost = bvh.SAHCost();
        Assert.True(cost >= 0);
    }

    [Fact]
    public void Optimize_AfterBuild_StillIntersects()
    {
        using var bvh = new BVHSoA();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Optimize(iterations: 3);
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }

    [Fact]
    public void ConvertFrom_BVH_StillIntersects()
    {
        using var bvhSrc = new BVH();
        bvhSrc.Build(TestGeometry.UnitCube(), triCount: 12);
        using var bvhSoA = new BVHSoA();
        bvhSoA.ConvertFrom(bvhSrc);
        var result = bvhSoA.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }

    [Fact]
    public void Intersect_SingleTriangle_Hit()
    {
        using var bvh = new BVHSoA();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);
        var result = bvh.Intersect(new Vector3(1, 1, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }

    [Fact]
    public void Intersect_SingleTriangle_Miss()
    {
        using var bvh = new BVHSoA();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);
        var result = bvh.Intersect(new Vector3(1, 1, 0), new Vector3(1, 0, 0));
        Assert.Null(result);
    }
}
