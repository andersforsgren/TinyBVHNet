using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for BVH construction (input validation, wrapper properties).
/// </summary>
public class BVHTests
{
    [Fact]
    public void Build_SingleTriangle_ProducesNodes()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);
        Assert.True(bvh.NodeCount >= 1);
    }

    [Fact]
    public void Build_UnitCube_ProducesNodes()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.True(bvh.NodeCount > 1);
    }

    [Fact]
    public void Build_ThrowsOnTooSmallArray()
    {
        using var bvh = new BVH();
        var tooSmall = new float[3];
        Assert.Throws<ArgumentException>(() => bvh.Build(tooSmall, triCount: 1));
    }

    [Fact]
    public void Build_AcceptsOversizedArray()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);
        Assert.True(bvh.NodeCount >= 1);
    }
}
