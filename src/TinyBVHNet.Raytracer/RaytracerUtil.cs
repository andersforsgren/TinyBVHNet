using System;
using System.Numerics;

namespace TinyBVHNet.Raytracer;

/// <summary>
/// Utility functions for post-processing rendered HDR data.
/// </summary>
public static class RaytracerUtil
{
    /// <summary>
    /// Reinhard tonemap: maps raw HDR luminance to [0, 1] LDR.
    /// Call after averaging the accumulated buffer:
    ///   avg = accum / spp;  ldr = RaytracerUtil.TonemapReinhard(avg, exposure);
    /// </summary>
    public static float TonemapReinhard(float hdrValue, float exposure)
    {
        float mapped = hdrValue * exposure;
        return mapped / (1.0f + mapped);
    }

    /// <summary>
    /// Apply Reinhard tonemapping to an entire HDR luminance buffer in-place.
    /// </summary>
    /// <param name="hdr">HDR luminance values, one per pixel (not interleaved). Modified in-place.</param>
    /// <param name="invSpp">1.0f / samplesPerPixel for averaging.</param>
    /// <param name="exposure">Exposure multiplier.</param>
    public static void TonemapReinhard(float[] hdr, float invSpp, float exposure)
    {
        for (int i = 0; i < hdr.Length; i++)
        {
            float avg = hdr[i] * invSpp;
            hdr[i] = (avg * exposure) / (1.0f + avg * exposure);
        }
    }

    /// <summary>
    /// Convert a tonemapped [0, 1] buffer to 8-bit RGB bytes.
    /// The LDR buffer is consumed and replaced with mono RGB triplets.
    /// </summary>
    /// <param name="ldr">Tonemapped monochrome values (one per pixel). Consumed in-place.</param>
    /// <param name="pixelCount">Total number of pixels.</param>
    /// <returns>RGB interleaved byte array of length pixelCount * 3.</returns>
    public static byte[] LdrToRgbBytes(float[] ldr, int pixelCount)
    {
        var rgb = new byte[pixelCount * 3];
        for (int i = 0; i < pixelCount; i++)
        {
            byte b = (byte)Math.Clamp(ldr[i] * 255f + 0.5f, 0f, 255f);
            rgb[i * 3 + 0] = b;
            rgb[i * 3 + 1] = b;
            rgb[i * 3 + 2] = b;
        }
        return rgb;
    }
}
