using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for the BVHBlasInstance wrapper.
/// BLASInstance is used in TLAS/BLAS hierarchies and has no intersection
/// methods of its own.
/// </summary>
public class BVHBlasInstanceTests
{
    [Fact]
    public void Create_DefaultIdx_ReturnsValidHandle()
    {
        using var blasInst = new BVHBlasInstance();
        Assert.NotEqual(IntPtr.Zero, blasInst.Handle);
    }

    [Fact]
    public void Create_ExplicitIdx_ReturnsValidHandle()
    {
        using var blasInst = new BVHBlasInstance(idx: 7);
        Assert.NotEqual(IntPtr.Zero, blasInst.Handle);
    }

    [Fact]
    public void Update_WithBuiltBVH_DoesNotThrow()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);
        using var blasInst = new BVHBlasInstance();
        blasInst.Update(bvh);
    }

    [Fact]
    public void Update_WithDisposedBVH_Throws()
    {
        // Accessing the Handle of a disposed BVH now throws,
        // which prevents passing a dangling pointer to native code.
        using var blasInst = new BVHBlasInstance();
        var bvh = new BVH();
        bvh.Dispose();
        Assert.Throws<ObjectDisposedException>(() => blasInst.Update(bvh));
    }

    [Fact]
    public void Update_OnDisposed_Throws()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);
        var blasInst = new BVHBlasInstance();
        blasInst.Dispose();
        Assert.Throws<ObjectDisposedException>(() => blasInst.Update(bvh));
    }

    [Fact]
    public void SetTransform_ValidMatrix_DoesNotThrow()
    {
        using var blasInst = new BVHBlasInstance();
        float[] identity = {
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        };
        blasInst.SetTransform(identity);
    }

    [Fact]
    public void SetTransform_ShortArray_Throws()
    {
        using var blasInst = new BVHBlasInstance();
        float[] tooShort = { 1, 0, 0, 0 };
        Assert.Throws<ArgumentException>(() => blasInst.SetTransform(tooShort));
    }

    [Fact]
    public void SetTransform_OnDisposed_Throws()
    {
        var blasInst = new BVHBlasInstance();
        blasInst.Dispose();
        float[] identity = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        Assert.Throws<ObjectDisposedException>(() => blasInst.SetTransform(identity));
    }

    [Fact]
    public void InvertTransform_DoesNotThrow()
    {
        using var blasInst = new BVHBlasInstance();
        float[] identity = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        blasInst.SetTransform(identity);
        blasInst.InvertTransform();
    }

    [Fact]
    public void InvertTransform_OnDisposed_Throws()
    {
        var blasInst = new BVHBlasInstance();
        blasInst.Dispose();
        Assert.Throws<ObjectDisposedException>(() => blasInst.InvertTransform());
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow()
    {
        var blasInst = new BVHBlasInstance();
        blasInst.Dispose();
        blasInst.Dispose();
    }

    [Fact]
    public void Dispose_CanCreateNewInstance()
    {
        var blasInst = new BVHBlasInstance();
        blasInst.Dispose();
        using var blasInst2 = new BVHBlasInstance(idx: 3);
        Assert.NotEqual(IntPtr.Zero, blasInst2.Handle);
    }
}
