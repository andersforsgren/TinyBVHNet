using Xunit;
using System.Numerics;

namespace TinyBVHNet.Tests;

/// <summary>BVH_SoA type-specific tests (not covered by IBVH interface contract).</summary>
public class BVHSoATests
{
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
}
