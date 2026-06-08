using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's BLASInstance — a bottom-level acceleration structure
/// instance with transform. Used in two-level BVH hierarchies (TLAS/BLAS).
/// BLASInstance does NOT have its own intersection methods; it is used as part of a TLAS.
/// </summary>
public class BVHBlasInstance : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BVHBlasInstance));
    }

    /// <param name="idx">Index of the BLAS this instance refers to.</param>
    public BVHBlasInstance(uint idx = 0)
    {
        _handle = NativeMethods.TBVH_BLASInstance_Create(idx);
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create native BLASInstance.");
    }

    /// <summary>Update the BLAS this instance points to.</summary>
    public void Update(BVH blas)
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_BLASInstance_Update(_handle, blas.Handle);
    }

    /// <summary>Invert the current transform.</summary>
    public void InvertTransform()
    {
        ThrowIfDisposed();
        NativeMethods.TBVH_BLASInstance_InvertTransform(_handle);
    }

    /// <summary>Set a 4x4 column-major transform matrix (16 floats).</summary>
    public void SetTransform(float[] matrix4x4)
    {
        ThrowIfDisposed();
        if (matrix4x4.Length < 16)
            throw new ArgumentException("Matrix must have 16 elements.", nameof(matrix4x4));
        NativeMethods.TBVH_BLASInstance_SetTransform(_handle, matrix4x4);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            NativeMethods.TBVH_BLASInstance_Destroy(_handle);
            _handle = IntPtr.Zero;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
