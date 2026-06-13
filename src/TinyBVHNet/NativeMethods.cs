using System.Runtime.InteropServices;

namespace TinyBVHNet;

/// <summary>
/// P/Invoke declarations for the TinyBVH native library.
/// </summary>
internal static class NativeMethods
{
    private const string DllName = "TinyBVHNetNative";

    // -- Regular binary BVH (LAYOUT_BVH) ----------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TBVH_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_Destroy(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_Build(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_Intersect(IntPtr bvh,
        float* origin, float* direction,
        ref float t, out float u, out float v, out uint primIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_Save(IntPtr bvh, [MarshalAs(UnmanagedType.LPStr)] string filename);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_Load(IntPtr bvh,
        [MarshalAs(UnmanagedType.LPStr)] string filename,
        float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_Refit(IntPtr bvh, uint nodeIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_NodeCount(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_TriangleCount(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_IsOccluded(IntPtr bvh,
        float* origin, float* direction, float maxDistance);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float TBVH_SAHCost(IntPtr bvh, uint nodeIdx);

    // -- BVH extended build methods ----------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_BuildHQ(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_BuildIndexed(IntPtr bvh,
        float* vertices, uint* indices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_BuildAABB(IntPtr bvh, float* aabbs, uint primCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_LoadIndexed(IntPtr bvh,
        [MarshalAs(UnmanagedType.LPStr)] string filename,
        float* vertices, uint* indices, uint triCount);

    // -- BVH extended query / metrics --------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_LeafCount(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_PrimCount(IntPtr bvh, uint nodeIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float TBVH_EPOCost(IntPtr bvh, uint nodeIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_IntersectSphere(IntPtr bvh,
        float* center, float radius);

    // -- BVH optimization --------------------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_Optimize(IntPtr bvh,
        uint iterations, int extreme, int stochastic);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_Compact(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_SplitLeafs(IntPtr bvh, uint maxPrims);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_CombineLeafs(IntPtr bvh, uint nodeIdx);

    // -- GPU binary BVH (LAYOUT_BVH_GPU) --

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TBVH_GPU_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_GPU_Destroy(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_GPU_Build(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_GPU_Intersect(IntPtr bvh,
        float* origin, float* direction,
        ref float t, out float u, out float v, out uint primIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_GPU_GetNodeCount(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_GPU_GetTriangleCount(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_GPU_GetNodes(IntPtr bvh, float* nodeData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_GPU_GetPrimitiveIndices(IntPtr bvh, uint* primIndices);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_GPU_GetVertices(IntPtr bvh, float* vertexData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_GPU_IsOccluded(IntPtr bvh,
        float* origin, float* direction, float maxDistance);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float TBVH_GPU_SAHCost(IntPtr bvh, uint nodeIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_GPU_Optimize(IntPtr bvh, uint iterations, int extreme);

    // -- GPU BVH extended build methods ------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_GPU_BuildHQ(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_GPU_BuildIndexed(IntPtr bvh,
        float* vertices, uint* indices, uint triCount);

    // -- 4-wide GPU BVH (LAYOUT_BVH4_GPU) --

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TBVH_GPU4_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_GPU4_Destroy(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_GPU4_Build(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_GPU4_Intersect(IntPtr bvh,
        float* origin, float* direction,
        ref float t, out float u, out float v, out uint primIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_GPU4_GetNodeCount(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_GPU4_GetTriangleCount(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_GPU4_GetNodes(IntPtr bvh, float* nodeData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_GPU4_GetPrimitiveIndices(IntPtr bvh, uint* primIndices);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_GPU4_GetVertices(IntPtr bvh, float* vertexData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_GPU4_IsOccluded(IntPtr bvh,
        float* origin, float* direction, float maxDistance);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float TBVH_GPU4_SAHCost(IntPtr bvh, uint nodeIdx);

    // -- GPU4 extended build / query / optimization ------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_GPU4_BuildHQ(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_GPU4_BuildIndexed(IntPtr bvh,
        float* vertices, uint* indices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_GPU4_LeafCount(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_GPU4_Optimize(IntPtr bvh, uint iterations, int extreme);

    // -- VoxelSet -----------------------------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TBVH_VoxelSet_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_VoxelSet_Destroy(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_VoxelSet_Set(IntPtr bvh, uint x, uint y, uint z, uint v);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_VoxelSet_UpdateTopGrid(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_VoxelSet_Intersect(IntPtr bvh,
        float* origin, float* direction,
        ref float t, out float u, out float v, out uint primIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_VoxelSet_IsOccluded(IntPtr bvh,
        float* origin, float* direction, float maxDistance);

    // -- BLASInstance -------------------------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TBVH_BLASInstance_Create(uint idx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_BLASInstance_Destroy(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_BLASInstance_Update(IntPtr bvh, IntPtr blas);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_BLASInstance_InvertTransform(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_BLASInstance_SetTransform(IntPtr bvh,
        float* matrix4x4);

    // -- BVH_SoA (Structure of Arrays) --------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TBVH_SoA_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_SoA_Destroy(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_SoA_Build(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_SoA_ConvertFrom(IntPtr bvh, IntPtr source);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_SoA_Intersect(IntPtr bvh,
        float* origin, float* direction,
        ref float t, out float u, out float v, out uint primIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_SoA_IsOccluded(IntPtr bvh,
        float* origin, float* direction, float maxDistance);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float TBVH_SoA_SAHCost(IntPtr bvh, uint nodeIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_SoA_Optimize(IntPtr bvh, uint iterations, int extreme);

    // -- BVH_Verbose (debug/inspection BVH) --------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TBVH_Verbose_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_Verbose_Destroy(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_Verbose_ConvertFrom(IntPtr bvh, IntPtr source);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_Verbose_Build(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_Verbose_NodeCount(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float TBVH_Verbose_SAHCost(IntPtr bvh, uint nodeIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_Verbose_Optimize(IntPtr bvh, uint iterations, int extreme, int stochastic);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_Verbose_Refit(IntPtr bvh, uint nodeIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_Verbose_Compact(IntPtr bvh);

    // -- BVH4_CPU (4-wide, SSE) --------------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TBVH_4CPU_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_4CPU_Destroy(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_4CPU_Build(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_4CPU_BuildHQ(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_4CPU_ConvertFrom(IntPtr bvh, IntPtr source);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_4CPU_Intersect(IntPtr bvh,
        float* origin, float* direction,
        ref float t, out float u, out float v, out uint primIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_4CPU_IsOccluded(IntPtr bvh,
        float* origin, float* direction, float maxDistance);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float TBVH_4CPU_SAHCost(IntPtr bvh, uint nodeIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_4CPU_Optimize(IntPtr bvh, uint iterations, int extreme);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_4CPU_Refit(IntPtr bvh, uint nodeIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_4CPU_Save(IntPtr bvh,
        [MarshalAs(UnmanagedType.LPStr)] string filename);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_4CPU_Load(IntPtr bvh,
        [MarshalAs(UnmanagedType.LPStr)] string filename,
        float* vertices, uint triCount);

    // -- BVH8_CPU (8-wide, AVX) --------------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TBVH_8CPU_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_8CPU_Destroy(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_8CPU_Build(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_8CPU_BuildHQ(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_8CPU_ConvertFrom(IntPtr bvh, IntPtr source);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_8CPU_Intersect(IntPtr bvh,
        float* origin, float* direction,
        ref float t, out float u, out float v, out uint primIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_8CPU_IsOccluded(IntPtr bvh,
        float* origin, float* direction, float maxDistance);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float TBVH_8CPU_SAHCost(IntPtr bvh, uint nodeIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_8CPU_Optimize(IntPtr bvh, uint iterations, int extreme);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_8CPU_Refit(IntPtr bvh, uint nodeIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_8CPU_Save(IntPtr bvh,
        [MarshalAs(UnmanagedType.LPStr)] string filename);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_8CPU_Load(IntPtr bvh,
        [MarshalAs(UnmanagedType.LPStr)] string filename,
        float* vertices, uint triCount);

    // -- BVH8_CWBVH (compressed wide BVH) ----------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TBVH_8CWBVH_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_8CWBVH_Destroy(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_8CWBVH_Build(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_8CWBVH_BuildHQ(IntPtr bvh, float* vertices, uint triCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_8CWBVH_Intersect(IntPtr bvh,
        float* origin, float* direction,
        ref float t, out float u, out float v, out uint primIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_8CWBVH_IsOccluded(IntPtr bvh,
        float* origin, float* direction, float maxDistance);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float TBVH_8CWBVH_SAHCost(IntPtr bvh, uint nodeIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_8CWBVH_Optimize(IntPtr bvh, uint iterations, int extreme);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_8CWBVH_Save(IntPtr bvh,
        [MarshalAs(UnmanagedType.LPStr)] string filename);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_8CWBVH_Load(IntPtr bvh,
        [MarshalAs(UnmanagedType.LPStr)] string filename,
        float* vertices, uint triCount);

    // -- BVH_Double (64-bit double precision) -------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TBVH_Double_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_Double_Destroy(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void TBVH_Double_Build(IntPtr bvh, double* vertices, ulong primCount);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_Double_Intersect(IntPtr bvh,
        double* origin, double* direction,
        ref double t, out double u, out double v, out ulong primIdx);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe int TBVH_Double_IsOccluded(IntPtr bvh,
        double* origin, double* direction, double maxDistance);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern double TBVH_Double_SAHCost(IntPtr bvh, ulong nodeIdx);

    // -- JobSystem ----------------------------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TBVH_JobSystem_Create();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void TBVH_JobSystem_Destroy(IntPtr bvh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int TBVH_JobSystem_IsBusy(IntPtr bvh);
}
