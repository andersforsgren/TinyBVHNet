using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace TinyBVHNet.Raytracer;

/// <summary>
/// Parses a subset of Wavefront OBJ: vertex positions (v) and triangle faces (f).
/// Converts to interleaved float4 vertex data for the BVH.
/// </summary>
public static class ObjParser
{
    public readonly struct Scene
    {
        public float[] Vertices { get; init; }
        public uint TriangleCount { get; init; }
        /// <summary>Array of triangle data, 3 Vector3 per triangle (for normal computation etc).</summary>
        public Vector3[][] Triangles { get; init; }
    }

    /// <summary>
    /// Parse an OBJ file and return interleaved float4 vertex data plus triangle count.
    /// </summary>
    public static Scene Load(string path)
    {
        var positions = new List<Vector3>();
        var faceTriplets = new List<(int a, int b, int c)>();

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#')
                continue;

            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            switch (parts[0])
            {
                case "v":
                    positions.Add(new Vector3(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)));
                    break;

                case "f":
                    // Only handle triangles (3 vertices per face)
                    if (parts.Length >= 4)
                    {
                        var i0 = ParseVertexIndex(parts[1]);
                        var i1 = ParseVertexIndex(parts[2]);
                        var i2 = ParseVertexIndex(parts[3]);
                        faceTriplets.Add((i0, i1, i2));
                        // If it's a quad with 4 vertices, triangulate as fan
                        if (parts.Length >= 5)
                        {
                            var i3 = ParseVertexIndex(parts[4]);
                            faceTriplets.Add((i0, i2, i3));
                        }
                    }
                    break;
            }
        }

        uint triCount = (uint)faceTriplets.Count;
        // Interleaved float4: each triangle has 3 vertices, each vertex is 4 floats (x,y,z,w)
        var vertices = new float[triCount * 3 * 4];
        var triangles = new Vector3[triCount][];

        for (int i = 0; i < triCount; i++)
        {
            var (a, b, c) = faceTriplets[i];
            var v0 = positions[a];
            var v1 = positions[b];
            var v2 = positions[c];

            int offset = i * 12; // 3 vertices * 4 floats
            vertices[offset + 0] = v0.X; vertices[offset + 1] = v0.Y; vertices[offset + 2] = v0.Z; vertices[offset + 3] = 1f;
            vertices[offset + 4] = v1.X; vertices[offset + 5] = v1.Y; vertices[offset + 6] = v1.Z; vertices[offset + 7] = 1f;
            vertices[offset + 8] = v2.X; vertices[offset + 9] = v2.Y; vertices[offset + 10] = v2.Z; vertices[offset + 11] = 1f;

            triangles[i] = new[] { v0, v1, v2 };
        }

        return new Scene { Vertices = vertices, TriangleCount = triCount, Triangles = triangles };
    }

    /// <summary>
    /// Parse a face vertex index (1-based from OBJ, may include texture/normal indices after slashes).
    /// </summary>
    private static int ParseVertexIndex(string token)
    {
        int slashPos = token.IndexOf('/');
        if (slashPos >= 0)
            token = token[..slashPos];
        return int.Parse(token, CultureInfo.InvariantCulture) - 1; // OBJ is 1-based
    }
}
