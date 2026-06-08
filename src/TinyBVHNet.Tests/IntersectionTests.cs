using System.Numerics;
using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for ray-BVH intersection.
/// </summary>
public class IntersectionTests
{
    [Fact]
    public void Intersect_HitSingleTriangle_ReturnsResult()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);

        // Ray from above, pointing straight down at the center of the triangle
        var origin = new Vector3(1, 1, 5);
        var direction = new Vector3(0, 0, -1);

        var result = bvh.Intersect(origin, direction);

        Assert.NotNull(result);
        Assert.Equal(5f, result.Value.Distance, precision: 4);
        Assert.True(result.Value.PrimitiveIndex < 1);
    }

    [Fact]
    public void Intersect_MissTriangle_ReturnsNull()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);

        // Ray from above, pointing straight down but well outside the triangle
        var origin = new Vector3(20, 20, 5);
        var direction = new Vector3(0, 0, -1);

        var result = bvh.Intersect(origin, direction);

        Assert.Null(result);
    }

    [Fact]
    public void Intersect_HitUnitCube_FromAbove()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);

        // Ray from above, pointing straight down at the center
        var origin = new Vector3(0, 0, 5);
        var direction = new Vector3(0, 0, -1);

        var result = bvh.Intersect(origin, direction);

        Assert.NotNull(result);
        // Should hit the +Z face at z=1, so distance should be ~4
        Assert.Equal(4f, result.Value.Distance, precision: 3);
    }

    [Fact]
    public void Intersect_HitUnitCube_FromSide()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);

        // Ray from the side, pointing at center
        var origin = new Vector3(5, 0, 0);
        var direction = new Vector3(-1, 0, 0);

        var result = bvh.Intersect(origin, direction);

        Assert.NotNull(result);
        // Should hit the +X face at x=1, so distance should be ~4
        Assert.Equal(4f, result.Value.Distance, precision: 3);
    }

    [Fact]
    public void Intersect_MissUnitCube_ShootingAway()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);

        // Ray starting above, shooting upward (away from cube)
        var origin = new Vector3(0, 0, 5);
        var direction = new Vector3(0, 0, 1);

        var result = bvh.Intersect(origin, direction);

        Assert.Null(result);
    }

    [Fact]
    public void Intersect_UsesMaxDistance()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);

        var origin = new Vector3(0, 0, 5);
        var direction = new Vector3(0, 0, -1);

        // Max distance too short — should miss
        var result = bvh.Intersect(origin, direction, maxDistance: 1f);

        Assert.Null(result);
    }

    [Fact]
    public void Intersect_DisposedBVH_Throws()
    {
        var bvh = new BVH();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);
        bvh.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1)));
    }
}
