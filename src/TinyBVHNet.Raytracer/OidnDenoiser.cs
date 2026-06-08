using System;
using System.Runtime.InteropServices;

namespace TinyBVHNet.Raytracer;

/// <summary>
/// Reusable OIDN denoiser for RGB float images.
/// Creates an OIDN CPU device and "RT" filter once, then denoises
/// repeatedly for the same image dimensions.
/// </summary>
public sealed class OidnDenoiser : IDisposable
{
    private readonly IntPtr _device;
    private readonly IntPtr _filter;
    private readonly int _width, _height;
    private bool _disposed;

    /// <summary>Create a denoiser for images of the given size.</summary>
    public OidnDenoiser(int width, int height)
    {
        _width = width;
        _height = height;

        _device = Oidn.NativeMethods.oidnNewDevice(Oidn.NativeMethods.OIDN_DEVICE_TYPE_CPU);
        if (_device == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create OIDN CPU device.");

        Oidn.NativeMethods.oidnCommitDevice(_device);

        _filter = Oidn.NativeMethods.oidnNewFilter(_device, "RT");
        if (_filter == IntPtr.Zero)
        {
            Oidn.NativeMethods.oidnReleaseDevice(_device);
            throw new InvalidOperationException("Failed to create OIDN 'RT' filter.");
        }

        Oidn.NativeMethods.oidnSetFilterBool(_filter, "hdr", true);
    }

    /// <summary>
    /// Denoise an RGB float image (interleaved R,G,B, length = width*height*3).
    /// The input is not modified; returns a new denoised array of the same layout.
    /// If OIDN fails, returns the original array unchanged.
    /// </summary>
    public float[] Denoise(float[] rgb)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OidnDenoiser));
        if (rgb.Length != _width * _height * 3)
            throw new ArgumentException($"Expected {_width * _height * 3} floats, got {rgb.Length}", nameof(rgb));

        float[] output = new float[rgb.Length];

        var w = (UIntPtr)_width;
        var h = (UIntPtr)_height;
        var zero = UIntPtr.Zero;

        GCHandle inHandle = GCHandle.Alloc(rgb, GCHandleType.Pinned);
        GCHandle outHandle = GCHandle.Alloc(output, GCHandleType.Pinned);

        try
        {
            Oidn.NativeMethods.oidnSetSharedFilterImage(
                _filter, "color", inHandle.AddrOfPinnedObject(),
                Oidn.NativeMethods.OIDN_FORMAT_FLOAT3, w, h, zero, zero, zero);

            Oidn.NativeMethods.oidnSetSharedFilterImage(
                _filter, "output", outHandle.AddrOfPinnedObject(),
                Oidn.NativeMethods.OIDN_FORMAT_FLOAT3, w, h, zero, zero, zero);

            Oidn.NativeMethods.oidnCommitFilter(_filter);
            Oidn.NativeMethods.oidnExecuteFilter(_filter);
        }
        finally
        {
            inHandle.Free();
            outHandle.Free();
        }

        return output;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_filter != IntPtr.Zero) Oidn.NativeMethods.oidnReleaseFilter(_filter);
        if (_device != IntPtr.Zero) Oidn.NativeMethods.oidnReleaseDevice(_device);
    }
}
