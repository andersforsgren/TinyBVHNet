namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's JobSystem — a simple thread pool
/// for parallel BVH building tasks.
/// </summary>
public class BVHJobSystem : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BVHJobSystem));
    }

    public BVHJobSystem()
    {
        _handle = NativeMethods.TBVH_JobSystem_Create();
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create native JobSystem instance.");
    }

    /// <summary>Returns true if any job is still running.</summary>
    public bool IsBusy
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.TBVH_JobSystem_IsBusy(_handle) != 0;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            NativeMethods.TBVH_JobSystem_Destroy(_handle);
            _handle = IntPtr.Zero;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
