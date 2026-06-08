using System;

namespace TinyBVHNet.Raytracer;

/// <summary>
/// Common interface for scene renderers. The scene is loaded in the constructor;
/// <see cref="Render"/> can be called repeatedly with different cameras.
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>
    /// Render the pre-loaded scene from the given camera.
    /// Returns a monochrome HDR luminance buffer of length ImageWidth * ImageHeight.
    /// </summary>
    float[] Render(Camera camera, RenderOptions options);
}
