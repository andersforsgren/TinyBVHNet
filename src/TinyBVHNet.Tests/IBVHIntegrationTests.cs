using System.Numerics;
using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// Wrapper-logic tests: verifies the managed wrapper correctly manages
/// native lifetimes and calls into native code without crashing.
/// Does NOT test correctness of the underlying tinybvh library.
/// </summary>
public class IBVHIntegrationTests
{
    public static IEnumerable<object[]> AllIBVH()
    {
        yield return new object[] { typeof(BVH) };
        yield return new object[] { typeof(BVH4CPU) };
        yield return new object[] { typeof(BVH8CPU) };
        yield return new object[] { typeof(BVH8CWBVH) };
        yield return new object[] { typeof(BVHSoA) };
        yield return new object[] { typeof(BVH4GPU) };
        yield return new object[] { typeof(BVHGPU) };
    }

    private static IBVH CreateBvh(Type bvhType) =>
        (IBVH)Activator.CreateInstance(bvhType)!;

    [Theory]
    [MemberData(nameof(AllIBVH))]
    public void Create_HasValidHandle(Type bvhType)
    {
        using var bvh = CreateBvh(bvhType);
        Assert.NotEqual(IntPtr.Zero, bvh.Handle);
    }

    [Theory]
    [MemberData(nameof(AllIBVH))]
    public void Dispose_DoubleDispose_NoException(Type bvhType)
    {
        var bvh = CreateBvh(bvhType);
        bvh.Dispose();
        var ex = Record.Exception(bvh.Dispose);
        Assert.Null(ex);
    }

    [Theory]
    [MemberData(nameof(AllIBVH))]
    public void Intersect_DisposedBVH_Throws(Type bvhType)
    {
        var bvh = CreateBvh(bvhType);
        bvh.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1)));
    }

    [Theory]
    [MemberData(nameof(AllIBVH))]
    public void Build_DoesNotThrow(Type bvhType)
    {
        using var bvh = CreateBvh(bvhType);
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
    }

    [Theory]
    [MemberData(nameof(AllIBVH))]
    public void Intersect_Hit_ReturnsNonNull(Type bvhType)
    {
        using var bvh = CreateBvh(bvhType);
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        var result = bvh.Intersect(new Vector3(0, 0, 5), new Vector3(0, 0, -1));
        Assert.NotNull(result);
    }

    [Theory]
    [MemberData(nameof(AllIBVH))]
    public void Intersect_Miss_ReturnsNull(Type bvhType)
    {
        using var bvh = CreateBvh(bvhType);
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        var result = bvh.Intersect(new Vector3(10, 10, 10), new Vector3(0, 0, 1));
        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(AllIBVH))]
    public void IsOccluded_Hit_ReturnsTrue(Type bvhType)
    {
        using var bvh = CreateBvh(bvhType);
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.True(bvh.IsOccluded(new Vector3(0, 0, 5), new Vector3(0, 0, -1)));
    }

    [Theory]
    [MemberData(nameof(AllIBVH))]
    public void IsOccluded_Miss_ReturnsFalse(Type bvhType)
    {
        using var bvh = CreateBvh(bvhType);
        bvh.Build(TestGeometry.UnitCube(), triCount: 12);
        Assert.False(bvh.IsOccluded(new Vector3(0, 0, 5), new Vector3(0, 0, 1)));
    }
}
