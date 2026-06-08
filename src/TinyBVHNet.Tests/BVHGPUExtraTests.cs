using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// Extended tests for BVHGPU: IsOccluded, BuildHQ, BuildIndexed, Optimize, SAHCost.
/// </summary>
public class BVHGPUExtraTests
{
    [Fact]
    public void IsOccluded_Hit_ReturnsTrue()
    {
        using var bvh = new BVHGPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        var origin = new float[] { 0f, 0f, 5f };
        var dir = new float[] { 0f, 0f, -1f };
        Assert.True(bvh.IsOccluded(origin, dir));
    }

    [Fact]
    public void IsOccluded_Miss_ReturnsFalse()
    {
        using var bvh = new BVHGPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        var origin = new float[] { 0f, 0f, 5f };
        var dir = new float[] { 0f, 0f, 1f };
        Assert.False(bvh.IsOccluded(origin, dir));
    }

    [Fact]
    public void IsOccluded_RespectsMaxDistance()
    {
        using var bvh = new BVHGPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        var origin = new float[] { 0f, 0f, 5f };
        var dir = new float[] { 0f, 0f, -1f };
        Assert.False(bvh.IsOccluded(origin, dir, 1f));
    }

    [Fact]
    public void BuildHQ_ProducesValidBVH()
    {
        using var bvh = new BVHGPU();
        bvh.BuildHQ(TestGeometry.UnitCube(), triCount: 12);
        var origin = new float[] { 0f, 0f, 5f };
        var dir = new float[] { 0f, 0f, -1f };
        var result = bvh.Intersect(origin, dir);
        Assert.NotNull(result);
    }

    [Fact]
    public void BuildIndexed_ProducesValidBVH()
    {
        using var bvh = new BVHGPU();
        var verts = new float[] { 0,0,0,1, 10,0,0,1, 0,10,0,1 };
        var indices = new uint[] { 0, 1, 2 };
        bvh.BuildIndexed(verts, indices, triCount: 1);
        Assert.True(bvh.IsBuilt);
    }

    [Fact]
    public void SAHCost_ReturnsNonNegative()
    {
        using var bvh = new BVHGPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        float cost = bvh.SAHCost();
        Assert.True(cost >= 0);
    }

    [Fact]
    public void Optimize_AfterBuild_StillIntersects()
    {
        using var bvh = new BVHGPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Optimize(iterations: 5);
        var origin = new float[] { 0f, 0f, 5f };
        var dir = new float[] { 0f, 0f, -1f };
        var result = bvh.Intersect(origin, dir);
        Assert.NotNull(result);
    }

    [Fact]
    public void NodeCount_AfterBuild_IsPositive()
    {
        using var bvh = new BVHGPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.True(bvh.NodeCount > 0);
    }

    [Fact]
    public void TriangleCount_ReturnsCorrectCount()
    {
        using var bvh = new BVHGPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.Equal(12, bvh.TriangleCount);
    }
}
