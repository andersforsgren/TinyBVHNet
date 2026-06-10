using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for BVH-specific methods not on the <see cref="IBVH"/> interface.
/// Only verifies P/Invoke calls don't crash -- does not test native correctness.
/// </summary>
public class BVHMethodsTests
{
    [Fact]
    public void BuildHQ_DoesNotThrow()
    {
        using var bvh = new BVH();
        bvh.BuildHQ(TestGeometry.UnitCube(), triCount: 12);
    }

    [Fact]
    public void BuildIndexed_DoesNotThrow()
    {
        using var bvh = new BVH();
        var verts = new float[] { 0, 0, 0, 1, 10, 0, 0, 1, 0, 10, 0, 1 };
        var indices = new uint[] { 0, 1, 2 };
        bvh.BuildIndexed(verts, indices, triCount: 1);
    }

    [Fact]
    public void BuildAABB_DoesNotThrow()
    {
        using var bvh = new BVH();
        var aabbs = new float[] { -1, -1, -1, 1, 1, 1 };
        bvh.BuildAABB(aabbs, primCount: 1);
    }

    [Fact]
    public void IntersectSphere_Hit_DoesNotThrow()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.IntersectSphere(0, 0, 0, radius: 2f);
    }

    [Fact]
    public void IntersectSphere_Miss_DoesNotThrow()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.IntersectSphere(10, 10, 10, radius: 0.1f);
    }

    [Fact]
    public void Optimize_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Optimize(iterations: 5);
    }

    [Fact]
    public void Compact_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Compact();
    }

    [Fact]
    public void SplitLeafs_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.SplitLeafs(maxPrims: 4);
    }

    [Fact]
    public void CombineLeafs_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.CombineLeafs();
    }
}
