using System.Numerics;

namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's BLASInstance -- a bottom-level acceleration structure
/// instance with transform. Used in two-level BVH hierarchies (TLAS/BLAS).
/// BLASInstance does NOT have its own intersection methods; it is used as part of a TLAS.
/// </summary>
public class BVHBlasInstance : NativeObject
{
    /// <param name="idx">Index of the BLAS this instance refers to.</param>
    public BVHBlasInstance(uint idx = 0)
        : base(NativeMethods.TBVH_BLASInstance_Create(idx), NativeMethods.TBVH_BLASInstance_Destroy)
    {
    }

    /// <summary>Update the BLAS this instance points to.</summary>
    public void Update(BVH blas)
    {
        NativeMethods.TBVH_BLASInstance_Update(Handle, blas.Handle);
    }

    /// <summary>Invert the current transform.</summary>
    public void InvertTransform()
    {
        NativeMethods.TBVH_BLASInstance_InvertTransform(Handle);
    }

    /// <summary>Set a 4x4 column-major transform matrix (16 floats).</summary>
    public void SetTransform(float[] matrix4x4)
    {
        if (matrix4x4.Length < 16)
            throw new ArgumentException("Matrix must have 16 elements.", nameof(matrix4x4));
        NativeMethods.TBVH_BLASInstance_SetTransform(Handle, matrix4x4);
    }
}
