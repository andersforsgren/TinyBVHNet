namespace TinyBVHNet.Tests;

/// <summary>
/// Provides simple triangle geometry for use in unit tests.
/// </summary>
internal static class TestGeometry
{
    /// <summary>
    /// A single triangle covering most of the XY plane at Z=0.
    /// Vertices: (0,0,0), (10,0,0), (0,10,0) — each as float4 (x,y,z,w).
    /// </summary>
    public static float[] SingleTriangle()
    {
        return
        [
            // Triangle 0: vertex 0
            0f, 0f, 0f, 1f,
            // Triangle 0: vertex 1
            10f, 0f, 0f, 1f,
            // Triangle 0: vertex 2
            0f, 10f, 0f, 1f,
        ];
    }

    /// <summary>
    /// A cube made of 12 triangles (2 per face), centered at origin, side length 2.
    /// </summary>
    public static float[] UnitCube()
    {
        return
        [
            // +X face
            1f, -1f, -1f, 1f,   1f, -1f,  1f, 1f,   1f,  1f,  1f, 1f,
            1f, -1f, -1f, 1f,   1f,  1f,  1f, 1f,   1f,  1f, -1f, 1f,
            // -X face
            -1f, -1f,  1f, 1f,  -1f, -1f, -1f, 1f,  -1f,  1f, -1f, 1f,
            -1f, -1f,  1f, 1f,  -1f,  1f, -1f, 1f,  -1f,  1f,  1f, 1f,
            // +Y face
            -1f,  1f, -1f, 1f,   1f,  1f, -1f, 1f,   1f,  1f,  1f, 1f,
            -1f,  1f, -1f, 1f,   1f,  1f,  1f, 1f,  -1f,  1f,  1f, 1f,
            // -Y face
            -1f, -1f,  1f, 1f,   1f, -1f,  1f, 1f,   1f, -1f, -1f, 1f,
            -1f, -1f,  1f, 1f,   1f, -1f, -1f, 1f,  -1f, -1f, -1f, 1f,
            // +Z face
            -1f, -1f,  1f, 1f,  -1f,  1f,  1f, 1f,   1f,  1f,  1f, 1f,
            -1f, -1f,  1f, 1f,   1f,  1f,  1f, 1f,   1f, -1f,  1f, 1f,
            // -Z face
             1f, -1f, -1f, 1f,  -1f,  1f, -1f, 1f,  -1f, -1f, -1f, 1f,
             1f, -1f, -1f, 1f,   1f,  1f, -1f, 1f,  -1f,  1f, -1f, 1f,
        ];
    }

    /// <summary>
    /// Double-precision: single triangle (3 doubles per vertex, 3 vertices per tri = 9 doubles).
    /// </summary>
    public static double[] SingleTriangleDouble()
    {
        return
        [
            0.0, 0.0, 0.0,
            10.0, 0.0, 0.0,
            0.0, 10.0, 0.0,
        ];
    }
}
