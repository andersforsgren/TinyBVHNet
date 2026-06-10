using Xunit;
using System.Numerics;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for BVHDouble wrapper logic. BVHDouble uses a separate
/// interface and is not included in <see cref="IBVHIntegrationTests"/>.
/// </summary>
public class BVHDoubleTests
{
    [Fact]
    public void Build_SingleTriangle_DoesNotThrow()
    {
        using var bvh = new BVHDouble();
        bvh.Build(TestGeometry.SingleTriangleDouble(), primCount: 1);
    }

    [Fact]
    public void Build_ThrowsOnTooSmallArray()
    {
        using var bvh = new BVHDouble();
        var tooSmall = new double[5];
        Assert.Throws<ArgumentException>(() => bvh.Build(tooSmall, primCount: 1));
    }

    [Fact]
    public void Intersect_Hit_ReturnsNonNull()
    {
        using var bvh = new BVHDouble();
        bvh.Build(TestGeometry.SingleTriangleDouble(), primCount: 1);
        var result = bvh.Intersect(
            new Vector3(2f, 2f, -1f),
            new Vector3(0f, 0f, 1f),
            maxDistance: 10.0);
        Assert.NotNull(result);
    }

    [Fact]
    public void IsOccluded_Hit_DoesNotThrow()
    {
        using var bvh = new BVHDouble();
        bvh.Build(TestGeometry.SingleTriangleDouble(), primCount: 1);
        bvh.IsOccluded(
            new Vector3(2f, 2f, -1f),
            new Vector3(0f, 0f, 1f),
            maxDistance: 10.0);
    }
}
