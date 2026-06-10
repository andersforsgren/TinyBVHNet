namespace TinyBVHNet;


public interface INativeObject
{
    public IntPtr Handle { get; }
}

public class NativeObject : IEquatable<NativeObject>, IDisposable
{
    private readonly Action<IntPtr> _destroy;
    private bool _isDisposed;
    private IntPtr _handle;

    /// <summary>
    /// Gets the native handle. Accessing this property after disposal throws
    /// <see cref="ObjectDisposedException"/>, so methods that use Handle
    /// get disposal checks automatically.
    /// </summary>
    public IntPtr Handle
    {
        get
        {
            ThrowIfDisposed();
            return _handle;
        }
    }

    protected NativeObject(IntPtr handle, Action<IntPtr> destroy)
    {
        _handle = handle;
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create native instance");
        this._destroy = destroy;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;
        _destroy(_handle);
        _isDisposed = true;
    }

    protected void ThrowIfDisposed()
    {
        if (_isDisposed) 
            throw new ObjectDisposedException(GetType().Name);
    }

    ~NativeObject()
    {
        Dispose(false);
    }

    public bool Equals(NativeObject? other)
    {
        if (other is null)
            return false;
        return _handle == other._handle;
    }

    public override bool Equals(object? obj) => Equals(obj as NativeObject);

    public override int GetHashCode() => _handle.GetHashCode();
}
