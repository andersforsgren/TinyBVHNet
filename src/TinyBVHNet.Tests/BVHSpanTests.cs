using System.Numerics;
using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for the <see cref="ReadOnlySpan{T}">ReadOnlySpan&lt;T&gt;</see> overloads
/// added to BVH Build/Load/SetTransform APIs. Verifies zero-copy construction,
/// validation, and slice-based building work identically to array overloads.
/// </summary>
public class BVHSpanTests
{
    // ── BVH (base) ────────────────────────────────────────────────────

    [Fact]
    public void BVH_BuildSpan_ProducesNodes()
    {
        using var bvh = new BVH();
        var data = TestGeometry.UnitCube();
        bvh.Build(data.AsSpan(), triCount: 12);
        Assert.True(bvh.NodeCount > 1);
    }

    [Fact]
    public void BVH_BuildSpan_SingleTriangle_ProducesNodes()
    {
        using var bvh = new BVH();
        var data = TestGeometry.SingleTriangle();
        bvh.Build(data.AsSpan(), triCount: 1);
        Assert.True(bvh.NodeCount >= 1);
    }

    [Fact]
    public void BVH_BuildSpan_ThrowsOnTooSmallSpan()
    {
        using var bvh = new BVH();
        var tooSmall = new float[3];
        Assert.Throws<ArgumentException>(() => bvh.Build(tooSmall.AsSpan(), triCount: 1));
    }

    [Fact]
    public void BVH_BuildSpan_FromSliceOfLargerArray()
    {
        using var bvh = new BVH();
        // Create an array with extra data before and after
        var padded = new float[1000];
        var cube = TestGeometry.UnitCube();
        cube.CopyTo(padded.AsSpan(100));
        bvh.Build(padded.AsSpan(100, cube.Length), triCount: 12);
        Assert.True(bvh.NodeCount > 1);
    }

    [Fact]
    public void BVH_BuildSpan_IntersectionMatchesArrayOverload()
    {
        using var bvhArray = new BVH();
        using var bvhSpan = new BVH();
        var verts = TestGeometry.UnitCube();

        bvhArray.Build(verts, triCount: 12);
        bvhSpan.Build(verts.AsSpan(), triCount: 12);

        var origin = new Vector3(0, 0, 5);
        var dir = new Vector3(0, 0, -1);

        var resultArray = bvhArray.Intersect(origin, dir);
        var resultSpan = bvhSpan.Intersect(origin, dir);

        Assert.NotNull(resultArray);
        Assert.NotNull(resultSpan);
        Assert.Equal(resultArray.Value.PrimitiveIndex, resultSpan.Value.PrimitiveIndex);
    }

    [Fact]
    public void BVH_BuildHQSpan_DoesNotThrow()
    {
        using var bvh = new BVH();
        var data = TestGeometry.UnitCube();
        bvh.BuildHQ(data.AsSpan(), triCount: 12);
    }

    [Fact]
    public void BVH_BuildIndexedSpan_DoesNotThrow()
    {
        using var bvh = new BVH();
        Span<float> verts = stackalloc float[] { 0, 0, 0, 1, 10, 0, 0, 1, 0, 10, 0, 1 };
        Span<uint> indices = stackalloc uint[] { 0, 1, 2 };
        bvh.BuildIndexed(verts, indices, triCount: 1);
    }

    [Fact]
    public void BVH_BuildAABBSpan_DoesNotThrow()
    {
        using var bvh = new BVH();
        Span<float> aabbs = stackalloc float[] { -1, -1, -1, 1, 1, 1 };
        bvh.BuildAABB(aabbs, primCount: 1);
    }

    [Fact]
    public void BVH_LoadSpan_ProducesSameIntersection()
    {
        var testFile = Path.Combine(Path.GetTempPath(), "TinyBVH_span_load.bvh");
        var verts = TestGeometry.UnitCube();
        var origin = new Vector3(0, 0, 5);
        var dir = new Vector3(0, 0, -1);

        // Build, save
        using (var bvh = new BVH())
        {
            bvh.Build(verts, triCount: 12);
            bvh.Save(testFile);
        }

        try
        {
            // Load via span overload
            using var bvh2 = new BVH();
            bvh2.Load(testFile, verts.AsSpan(), triCount: 12);
            var result = bvh2.Intersect(origin, dir);
            Assert.NotNull(result);
        }
        finally
        {
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public void BVH_LoadIndexedSpan_DoesNotThrow()
    {
        var testFile = Path.Combine(Path.GetTempPath(), "TinyBVH_span_load_indexed.bvh");
        var verts = new float[] { 0, 0, 0, 1, 10, 0, 0, 1, 0, 10, 0, 1 };
        var indices = new uint[] { 0, 1, 2 };

        // Build indexed and save
        using (var bvh = new BVH())
        {
            bvh.BuildIndexed(verts, indices, triCount: 1);
            bvh.Save(testFile);
        }

        try
        {
            using var bvh2 = new BVH();
            bvh2.LoadIndexed(testFile, verts.AsSpan(), indices.AsSpan(), triCount: 1);
        }
        finally
        {
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    // ── BVH4CPU ────────────────────────────────────────────────────────

    [Fact]
    public void BVH4CPU_BuildSpan_ProducesNodes()
    {
        using var bvh = new BVH4CPU();
        var data = TestGeometry.UnitCube();
        bvh.Build(data.AsSpan(), triCount: 12);
        Assert.True(bvh.SAHCost() > 0);
    }

    [Fact]
    public void BVH4CPU_BuildHQSpan_DoesNotThrow()
    {
        using var bvh = new BVH4CPU();
        bvh.BuildHQ(TestGeometry.UnitCube().AsSpan(), triCount: 12);
    }

    [Fact(Skip = "CPU variant Save/Load causes AccessViolation (known issue)")]
    public void BVH4CPU_LoadSpan_DoesNotThrow()
    {
        var testFile = Path.Combine(Path.GetTempPath(), "TinyBVH_4cpu_span_load.bvh");
        var verts = TestGeometry.UnitCube();
        using (var bvh = new BVH4CPU())
        {
            bvh.Build(verts, triCount: 12);
            bvh.Save(testFile);
        }
        try
        {
            using var bvh2 = new BVH4CPU();
            bvh2.Load(testFile, verts.AsSpan(), triCount: 12);
        }
        finally
        {
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    // ── BVH8CPU ────────────────────────────────────────────────────────

    [Fact]
    public void BVH8CPU_BuildSpan_ProducesNodes()
    {
        using var bvh = new BVH8CPU();
        var data = TestGeometry.UnitCube();
        bvh.Build(data.AsSpan(), triCount: 12);
        Assert.True(bvh.SAHCost() > 0);
    }

    [Fact]
    public void BVH8CPU_BuildHQSpan_DoesNotThrow()
    {
        using var bvh = new BVH8CPU();
        bvh.BuildHQ(TestGeometry.UnitCube().AsSpan(), triCount: 12);
    }

    // ── BVH8CWBVH ─────────────────────────────────────────────────────

    [Fact]
    public void BVH8CWBVH_BuildSpan_ProducesNodes()
    {
        using var bvh = new BVH8CWBVH();
        var data = TestGeometry.UnitCube();
        bvh.Build(data.AsSpan(), triCount: 12);
        Assert.True(bvh.SAHCost() > 0);
    }

    [Fact]
    public void BVH8CWBVH_BuildHQSpan_DoesNotThrow()
    {
        using var bvh = new BVH8CWBVH();
        bvh.BuildHQ(TestGeometry.UnitCube().AsSpan(), triCount: 12);
    }

    // ── BVHGPU ─────────────────────────────────────────────────────────

    [Fact]
    public void BVHGPU_BuildSpan_ProducesNodes()
    {
        using var bvh = new BVHGPU();
        var data = TestGeometry.UnitCube();
        bvh.Build(data.AsSpan(), triCount: 12);
        Assert.True(bvh.NodeCount > 0);
    }

    [Fact]
    public void BVHGPU_BuildHQSpan_DoesNotThrow()
    {
        using var bvh = new BVHGPU();
        bvh.BuildHQ(TestGeometry.UnitCube().AsSpan(), triCount: 12);
    }

    [Fact]
    public void BVHGPU_BuildIndexedSpan_DoesNotThrow()
    {
        using var bvh = new BVHGPU();
        Span<float> verts = stackalloc float[] { 0, 0, 0, 1, 10, 0, 0, 1, 0, 10, 0, 1 };
        Span<uint> indices = stackalloc uint[] { 0, 1, 2 };
        bvh.BuildIndexed(verts, indices, triCount: 1);
    }

    // ── BVH4GPU ────────────────────────────────────────────────────────

    [Fact]
    public void BVH4GPU_BuildSpan_ProducesNodes()
    {
        using var bvh = new BVH4GPU();
        var data = TestGeometry.UnitCube();
        bvh.Build(data.AsSpan(), triCount: 12);
        Assert.True(bvh.NodeCount > 0);
    }

    [Fact]
    public void BVH4GPU_BuildHQSpan_DoesNotThrow()
    {
        using var bvh = new BVH4GPU();
        bvh.BuildHQ(TestGeometry.UnitCube().AsSpan(), triCount: 12);
    }

    [Fact]
    public void BVH4GPU_BuildIndexedSpan_DoesNotThrow()
    {
        using var bvh = new BVH4GPU();
        Span<float> verts = stackalloc float[] { 0, 0, 0, 1, 10, 0, 0, 1, 0, 10, 0, 1 };
        Span<uint> indices = stackalloc uint[] { 0, 1, 2 };
        bvh.BuildIndexed(verts, indices, triCount: 1);
    }

    // ── BVHSoA ─────────────────────────────────────────────────────────

    [Fact]
    public void BVHSoA_BuildSpan_ProducesNodes()
    {
        using var bvh = new BVHSoA();
        var data = TestGeometry.UnitCube();
        bvh.Build(data.AsSpan(), triCount: 12);
        Assert.True(bvh.SAHCost() > 0);
    }

    // ── BVHVerbose ─────────────────────────────────────────────────────

    [Fact]
    public void BVHVerbose_BuildSpan_ProducesNodes()
    {
        using var bvh = new BVHVerbose();
        var data = TestGeometry.UnitCube();
        bvh.Build(data.AsSpan(), triCount: 12);
        Assert.True(bvh.NodeCount > 0);
    }

    // ── BVHBlasInstance ────────────────────────────────────────────────

    [Fact]
    public void BlasInstance_SetTransformSpan_DoesNotThrow()
    {
        using var blas = new BVH();
        blas.Build(TestGeometry.SingleTriangle(), triCount: 1);

        using var instance = new BVHBlasInstance(idx: 0);
        instance.Update(blas);

        var identity = new float[]
        {
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1,
        };
        instance.SetTransform(identity.AsSpan());
    }

    [Fact]
    public void BlasInstance_SetTransformSpan_ThrowsOnTooSmall()
    {
        using var instance = new BVHBlasInstance();
        var tooSmall = new float[8];
        Assert.Throws<ArgumentException>(() => instance.SetTransform(tooSmall.AsSpan()));
    }

    // ── BVHDouble ──────────────────────────────────────────────────────

    [Fact]
    public void BVHDouble_BuildSpan_DoesNotThrow()
    {
        using var bvh = new BVHDouble();
        var data = TestGeometry.SingleTriangleDouble();
        bvh.Build(data.AsSpan(), primCount: 1);
    }

    [Fact]
    public void BVHDouble_BuildSpan_ThrowsOnTooSmallSpan()
    {
        using var bvh = new BVHDouble();
        var tooSmall = new double[5];
        Assert.Throws<ArgumentException>(() => bvh.Build(tooSmall.AsSpan(), primCount: 1));
    }

    [Fact]
    public void BVHDouble_BuildSpan_IntersectionMatches()
    {
        using var bvhSpan = new BVHDouble();
        var data = TestGeometry.SingleTriangleDouble();
        bvhSpan.Build(data.AsSpan(), primCount: 1);

        var result = bvhSpan.Intersect(
            new Vector3(2f, 2f, -1f),
            new Vector3(0f, 0f, 1f),
            maxDistance: 10.0);
        Assert.NotNull(result);
    }

    // ── Edge cases ─────────────────────────────────────────────────────

    [Fact]
    public void StackAllocSpan_Build_DoesNotThrow()
    {
        using var bvh = new BVH();
        // Build from a stack-allocated span
        Span<float> verts = stackalloc float[]
        {
            0, 0, 0, 1,
            10, 0, 0, 1,
            0, 10, 0, 1,
        };
        bvh.Build(verts, triCount: 1);
        Assert.True(bvh.NodeCount >= 1);
    }
}
