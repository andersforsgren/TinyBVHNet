using Xunit;

namespace TinyBVHNet.Tests
{
    public class BVH4GPUTests
    {
        [Fact]
        public void Build_UnitCube_BuildsSuccessfully()
        {
            using var bvh = new BVH4GPU();
            var vertices = TestGeometry.UnitCube();
            bvh.Build(vertices, triCount: 12);
            Assert.True(bvh.IsBuilt);
        }

        [Fact]
        public void Intersect_Miss_ReturnsNull()
        {
            using var bvh = new BVH4GPU();
            var vertices = TestGeometry.UnitCube();
            bvh.Build(vertices, triCount: 12);

            // Shoot straight up away from cube
            var origin = new float[] { 10f, 10f, 10f };
            var dir = new float[] { 0f, 0f, 1f };
            var result = bvh.Intersect(origin, dir);
            Assert.Null(result);
        }

        [Fact]
        public void Intersect_Hit_ReturnsIntersection()
        {
            using var bvh = new BVH4GPU();
            var vertices = TestGeometry.UnitCube();
            bvh.Build(vertices, triCount: 12);

            var origin = new float[] { 0f, 0f, 5f };
            var dir = new float[] { 0f, 0f, -1f };
            var result = bvh.Intersect(origin, dir);
            Assert.NotNull(result);
            Assert.True(result!.Value.Distance > 0f);
            Assert.True(result.Value.Distance < 10f);
        }

        [Fact]
        public void Intersect_Miss_MaxDistanceTooShort()
        {
            using var bvh = new BVH4GPU();
            var vertices = TestGeometry.UnitCube();
            bvh.Build(vertices, triCount: 12);

            var origin = new float[] { 0f, 0f, 5f };
            var dir = new float[] { 0f, 0f, -1f };
            var result = bvh.Intersect(origin, dir, 1f);
            Assert.Null(result);
        }

        [Fact]
        public void Intersect_SingleTriangle_Hit()
        {
            using var bvh = new BVH4GPU();
            var vertices = TestGeometry.SingleTriangle();
            bvh.Build(vertices, triCount: 1);

            var origin = new float[] { 0.33f, 0.33f, 1f };
            var dir = new float[] { 0f, 0f, -1f };
            var result = bvh.Intersect(origin, dir);
            Assert.NotNull(result);
            Assert.True(result!.Value.Distance > 0f);
        }

        [Fact]
        public void Intersect_SingleTriangle_Miss()
        {
            using var bvh = new BVH4GPU();
            var vertices = TestGeometry.SingleTriangle();
            bvh.Build(vertices, triCount: 1);

            var origin = new float[] { 0.33f, 0.33f, 1f };
            var dir = new float[] { 1f, 0f, 0f };
            var result = bvh.Intersect(origin, dir);
            Assert.Null(result);
        }

        [Fact]
        public void Dispose_DoubleDispose_NoException()
        {
            var bvh = new BVH4GPU();
            bvh.Dispose();
            var ex = Record.Exception(() => bvh.Dispose());
            Assert.Null(ex);
        }
    }
}