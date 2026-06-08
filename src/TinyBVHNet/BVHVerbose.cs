using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's Verbose BVH — a debugging/inspection variant
/// that stores detailed per-node statistics. Not intended for real-time ray tracing.
/// </summary>
public class BVHVerbose : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BVHVerbose));
    }

    public BVHVerbose()
    {
        _handle = NativeMethods.TBVH_Verbose_Create();
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create native BVH_Verbose instance.");
    }

    public void ConvertFrom(BVH source)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_Verbose_ConvertFrom(_handle, source.Handle);
    }

    /// <summary>Build from scratch using vertex data.</summary>
    public void Build(float[] vertices, uint triCount)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_Verbose_Build(_handle, vertices, triCount);
    }

    public int NodeCount
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.TBVH_Verbose_NodeCount(_handle);
        }
    }

    public float SAHCost(uint nodeIdx = 0)
    {
        ThrowIfDisposed();
        return NativeMethods.TBVH_Verbose_SAHCost(_handle, nodeIdx);
    }

    public void Optimize(uint iterations = 1, bool extreme = false, bool stochastic = false)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_Verbose_Optimize(_handle, iterations, extreme ? 1 : 0, stochastic ? 1 : 0);
    }

    public void Refit(uint nodeIdx = 0)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_Verbose_Refit(_handle, nodeIdx);
    }

    public void Compact()
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_Verbose_Compact(_handle);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            NativeMethods.TBVH_Verbose_Destroy(_handle);
            _handle = IntPtr.Zero;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
