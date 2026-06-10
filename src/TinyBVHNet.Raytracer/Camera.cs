using System.Numerics;

namespace TinyBVHNet.Raytracer;

/// <summary>
/// Camera parameters for rendering. Pure data -- no precomputation.
/// </summary>
public readonly record struct Camera(Vector3 Pos, Vector3 LookAt, Vector3 Up, float Fov);
