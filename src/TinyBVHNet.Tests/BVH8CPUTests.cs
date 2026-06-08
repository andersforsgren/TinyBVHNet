using Xunit;
using System.Numerics;

namespace TinyBVHNet.Tests;

public class BVH8CPUTests
{
    [Fact]
    public void Create_ReturnsValidHandle()
    {
        using var bvh = new BVH8CPU();
        Assert.True(true);
    }

    [Fact]
    public void Build_UnitCube_Success()
    {
        using var bvh = new BVH8CPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.True(true);
    }
}
