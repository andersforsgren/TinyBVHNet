using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for BVH refitting (wrapper logic only).
/// </summary>
public class RefitTests
{
    [Fact]
    public void Refit_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Refit();
    }
}
