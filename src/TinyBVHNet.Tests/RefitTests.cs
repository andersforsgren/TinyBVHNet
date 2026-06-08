using System.Numerics;
using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for BVH refitting (updating bounds without full rebuild).
/// </summary>
public class RefitTests
{
    [Fact]
    public void Refit_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);

        // Refitting immediately after build should be safe
        bvh.Refit();

        // BVH should still work for intersection
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }

    [Fact]
    public void Refit_PreservesNodeCount()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        int countBefore = bvh.NodeCount;

        bvh.Refit();

        Assert.Equal(countBefore, bvh.NodeCount);
    }

    [Fact]
    public void Refit_DisposedBVH_Throws()
    {
        var bvh = new BVH();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);
        bvh.Dispose();

        Assert.Throws<ObjectDisposedException>(() => bvh.Refit());
    }
}
