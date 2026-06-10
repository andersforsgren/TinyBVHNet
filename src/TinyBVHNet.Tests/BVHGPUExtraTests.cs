using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// BVHGPU-specific methods not on <see cref="IBVH"/>.
/// Only verifies P/Invoke calls don't crash.
/// </summary>
public class BVHGPUExtraTests
{
    [Fact]
    public void BuildHQ_DoesNotThrow()
    {
        using var bvh = new BVHGPU();
        bvh.BuildHQ(TestGeometry.UnitCube(), triCount: 12);
    }

    [Fact]
    public void BuildIndexed_DoesNotThrow()
    {
        using var bvh = new BVHGPU();
        var verts = new float[] { 0, 0, 0, 1, 10, 0, 0, 1, 0, 10, 0, 1 };
        var indices = new uint[] { 0, 1, 2 };
        bvh.BuildIndexed(verts, indices, triCount: 1);
    }

    [Fact]
    public void Optimize_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVHGPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        bvh.Optimize(iterations: 5);
    }

    [Fact]
    public void NodeCount_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVHGPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.True(bvh.NodeCount > 0);
    }

    [Fact]
    public void TriangleCount_AfterBuild_DoesNotThrow()
    {
        using var bvh = new BVHGPU();
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.Equal(12, bvh.TriangleCount);
    }
}
