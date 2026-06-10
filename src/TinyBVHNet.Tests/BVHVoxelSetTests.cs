using Xunit;
using System.Numerics;

namespace TinyBVHNet.Tests;


public class BVHVoxelSetTests
{
    [Fact]
    public void Create_ReturnsValidHandle()
    {
        using var voxels = new BVHVoxelSet();
        Assert.NotEqual(IntPtr.Zero, voxels.Handle);
    }

    [Fact]
    public void Set_And_UpdateTopGrid_DoesNotThrow()
    {
        using var voxels = new BVHVoxelSet();
        voxels.Set(10, 20, 30, v: 1);
        voxels.UpdateTopGrid();
    }
}
