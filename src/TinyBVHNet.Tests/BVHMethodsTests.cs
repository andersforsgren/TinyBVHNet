using Xunit;
using System.Numerics;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for extended BVH methods: IsOccluded, BuildHQ, BuildIndexed,
/// BuildAABB, IntersectSphere, SAHCost, LeafCount, PrimCount, EPOCost,
/// Optimize, Compact, SplitLeafs, CombineLeafs.
/// </summary>
public class BVHMethodsTests
{
    [Fact]
    public void IsOccluded_Hit_ReturnsTrue()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bool occluded = bvh.IsOccluded(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.True(occluded);
    }

    [Fact]
    public void IsOccluded_Miss_ReturnsFalse()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bool occluded = bvh.IsOccluded(new Vector3(0, 0, 5), new Vector3(0, 0, 1));
        Assert.False(occluded);
    }

    [Fact]
    public void IsOccluded_RespectsMaxDistance()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        // Max distance too short — should not be occluded
        bool occluded = bvh.IsOccluded(new Vector3(0, 0, 5), new Vector3(0, 0, -1), maxDistance: 1f);
        Assert.False(occluded);
    }

    [Fact]
    public void BuildHQ_ProducesNodes()
    {
        using var bvh = new BVH();
        bvh.BuildHQ(TestGeometry.UnitCube(), triCount: 12);
        Assert.True(bvh.NodeCount > 1);
    }

    [Fact]
    public void BuildIndexed_ProducesNodes()
    {
        using var bvh = new BVH();
        var verts = new float[] { 0,0,0,1, 10,0,0,1, 0,10,0,1 };
        var indices = new uint[] { 0, 1, 2 };
        bvh.BuildIndexed(verts, indices, triCount: 1);
        Assert.True(bvh.NodeCount >= 1);
    }

    [Fact]
    public void BuildAABB_ProducesNodes()
    {
        using var bvh = new BVH();
        // AABBs: minX,minY,minZ, maxX,maxY,maxZ (6 floats)
        var aabbs = new float[] { -1,-1,-1, 1,1,1 };
        bvh.BuildAABB(aabbs, primCount: 1);
        Assert.True(bvh.NodeCount >= 1);
    }

    [Fact]
    public void IntersectSphere_Hit_ReturnsTrue()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bool hit = bvh.IntersectSphere(0, 0, 0, radius: 2f);
        Assert.True(hit);
    }

    [Fact]
    public void IntersectSphere_Miss_ReturnsFalse()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bool hit = bvh.IntersectSphere(10, 10, 10, radius: 0.1f);
        Assert.False(hit);
    }

    [Fact]
    public void SAHCost_ReturnsNonNegative()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        float cost = bvh.SAHCost();
        Assert.True(cost >= 0f);
    }

    [Fact]
    public void LeafCount_AfterBuild_IsPositive()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.True(bvh.LeafCount > 0);
    }

    [Fact]
    public void GetPrimCount_Root_EqualsTriangleCount()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.Equal(12, bvh.GetPrimCount());
    }

    [Fact]
    public void EPOCost_ReturnsNonNegative()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        float cost = bvh.EPOCost();
        Assert.True(cost >= 0f);
    }

    [Fact]
    public void Optimize_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Optimize(iterations: 5);
        // BVH should still work after optimization
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }

    [Fact]
    public void Compact_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Compact();
        Assert.True(bvh.NodeCount >= 1);
    }

    [Fact]
    public void SplitLeafs_AfterBuild_PreservesIntersection()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.SplitLeafs(maxPrims: 4);
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }

    [Fact]
    public void CombineLeafs_AfterBuild_PreservesIntersection()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.CombineLeafs();
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }

    [Fact]
    public void TriangleCount_AfterBuild_ReturnsCorrectCount()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.Equal(12, bvh.TriangleCount);
    }

    [Fact]
    public void IsOccluded_DisposedBVH_Throws()
    {
        var bvh = new BVH();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);
        bvh.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            bvh.IsOccluded(new Vector3(0, 0, 5), new Vector3(0, 0, -1)));
    }
}
