namespace TinyBVHNet;

/// <summary>
/// Managed wrapper around TinyBVH's JobSystem -- a simple thread pool
/// for parallel BVH building tasks.
/// </summary>
public class BVHJobSystem : NativeObject
{
    public BVHJobSystem()
        : base(NativeMethods.TBVH_JobSystem_Create(), NativeMethods.TBVH_JobSystem_Destroy)
    {
    }

    /// <summary>Returns true if any job is still running.</summary>
    public bool IsBusy
    {
        get
        {
            return NativeMethods.TBVH_JobSystem_IsBusy(Handle) != 0;
        }
    }
}
