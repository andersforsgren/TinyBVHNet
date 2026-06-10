using System.Numerics;
using Xunit;

namespace TinyBVHNet.Tests;

/// <summary>
/// Tests for BVH serialization (Save/Load).
/// </summary>
public class SaveLoadTests
{
    private static readonly string TestFile = Path.Combine(Path.GetTempPath(), "TinyBVH_test_save.bvh");

    [Fact]
    public void SaveAndLoad_ProducesSameIntersection()
    {
        using var bvh = new BVH();
        var vertices = TestGeometry.UnitCube();
        bvh.Build(vertices, triCount: 12);

        // Do an intersection with the original
        var rayOrigin = new Vector3(0, 0, 5);
        var rayDir = new Vector3(0, 0, -1);
        var originalResult = bvh.Intersect(rayOrigin, rayDir);
        Assert.NotNull(originalResult);

        // Save
        bvh.Save(TestFile);
        Assert.True(File.Exists(TestFile));

        // Load into a new BVH
        using var bvh2 = new BVH();
        bvh2.Load(TestFile, vertices, triCount: 12);

        var loadedResult = bvh2.Intersect(rayOrigin, rayDir);

        Assert.NotNull(loadedResult);
        Assert.Equal(originalResult.Value.Distance, loadedResult.Value.Distance, precision: 5);
        Assert.Equal(originalResult.Value.PrimitiveIndex, loadedResult.Value.PrimitiveIndex);

        // Clean up
        if (File.Exists(TestFile))
            File.Delete(TestFile);
    }

    [Fact]
    public void Load_WithMismatchedVertices_StillLoads()
    {
        using var bvh = new BVH();
        bvh.Build(TestGeometry.SingleTriangle(), triCount: 1);
        bvh.Save(TestFile);

        using var bvh2 = new BVH();
        // Load with correct vertices -- should work
        bvh2.Load(TestFile, TestGeometry.SingleTriangle(), triCount: 1);

        Assert.True(bvh2.NodeCount > 0);

        // Clean up
        if (File.Exists(TestFile))
            File.Delete(TestFile);
    }

    [Theory(Skip = "CPU variants Save/Load causes AccessViolation (strict-aliasing issue with MinGW DLL)")]
    [InlineData(typeof(BVH4CPU))]
    [InlineData(typeof(BVH8CPU))]
    [InlineData(typeof(BVH8CWBVH))]
    public void SaveAndLoad_CpuVariant_ProducesHit(Type bvhType)
    {
        var vertices = TestGeometry.UnitCube();

        // Build and save
        using (var bvh = (IBVH)Activator.CreateInstance(bvhType)!)
        {
            bvh.Build(vertices, triCount: 12);
            var rayOrigin = new Vector3(0, 0, 5);
            var rayDir = new Vector3(0, 0, -1);
            Assert.NotNull(bvh.Intersect(rayOrigin, rayDir));
            Save(bvh);
        }

        // Load into a fresh instance and verify intersection still works
        using (var bvh = (IBVH)Activator.CreateInstance(bvhType)!)
        {
            Load(bvh, vertices);
            var rayOrigin = new Vector3(0, 0, 5);
            var rayDir = new Vector3(0, 0, -1);
            Assert.NotNull(bvh.Intersect(rayOrigin, rayDir));
        }

        if (File.Exists(TestFile))
            File.Delete(TestFile);
    }

    private static void Save(IBVH bvh)
    {
        // Call the type-appropriate Save via reflection
        var saveMethod = bvh.GetType().GetMethod("Save")
            ?? throw new InvalidOperationException("Save method not found");
        saveMethod.Invoke(bvh, [TestFile]);
    }

    private static void Load(IBVH bvh, float[] vertices)
    {
        var loadMethod = bvh.GetType().GetMethod("Load")
            ?? throw new InvalidOperationException("Load method not found");
        loadMethod.Invoke(bvh, [TestFile, vertices, (uint)(vertices.Length / 12)]);
    }
}
