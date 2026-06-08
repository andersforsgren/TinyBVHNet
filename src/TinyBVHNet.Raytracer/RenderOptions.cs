namespace TinyBVHNet.Raytracer;

/// <summary>
/// Per-render configuration. Each <see cref="IRenderer.Render"/> call produces
/// exactly 1 sample per pixel of raw HDR data; the caller accumulates and tonemaps.
/// Exposure is a tonemapping concern and belongs on the caller side.
/// </summary>
public readonly record struct RenderOptions(int MaxBounces);
