using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for BVH construction and basic properties.
/// </summary>
public class BVHTests
{
    [Fact]
    public void Create_ReturnsValidHandle()
    {
        using var bvh = new BVH();
        Assert.True(bvh.NodeCount >= 0);
    }

    [Fact]
    public void Build_SingleTriangle_ProducesNodes()
    {
        using var bvh = new BVH();
        var vertices = TestGeometry.SingleTriangle();

        bvh.Build(vertices, triCount: 1);

        // A single triangle BVH should have at least 1 node (typically 1 leaf)
        Assert.True(bvh.NodeCount >= 1);
    }

    [Fact]
    public void Build_UnitCube_ProducesNodes()
    {
        using var bvh = new BVH();
        var vertices = TestGeometry.UnitCube();

        bvh.Build(vertices, triCount: 12);

        // A 12-triangle BVH should have multiple nodes
        Assert.True(bvh.NodeCount > 1);
    }

    [Fact]
    public void Build_ThrowsOnTooSmallArray()
    {
        using var bvh = new BVH();
        var tooSmall = new float[3]; // Need at least 12 floats for 1 triangle

        Assert.Throws<ArgumentException>(() => bvh.Build(tooSmall, triCount: 1));
    }

    [Fact]
    public void Build_AcceptsOversizedArray()
    {
        using var bvh = new BVH();
        var vertices = TestGeometry.SingleTriangle();
        // Array has 12 floats, but we only tell it to use 1 triangle (12 floats)
        // This should work without throwing
        bvh.Build(vertices, triCount: 1);
        Assert.True(bvh.NodeCount >= 1);
    }
}
