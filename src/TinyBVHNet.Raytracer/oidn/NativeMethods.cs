using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TinyBVHNet.Raytracer.Oidn;

/// <summary>
/// P/Invoke declarations for Intel Open Image Denoise (OIDN) 2.5.0.
/// </summary>
internal static class NativeMethods
{
    private const string DllName = "OpenImageDenoise";

    static NativeMethods()
    {
        // Resolve OpenImageDenoise.dll from the oidn\win-x64\ subdirectory
        // relative to the assembly. Dependencies (OpenImageDenoise_core.dll,
        // OpenImageDenoise_device_cpu.dll, tbb12.dll) are found automatically
        // because NativeLibrary.Load with a full path uses LOAD_WITH_ALTERED_SEARCH_PATH.
        NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ResolveOidn);
    }

    private static IntPtr ResolveOidn(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != DllName)
            return IntPtr.Zero;

        // Assembly location, e.g.:  ...\bin\Debug\net8.0\TinyBVHNet.Raytracer.dll
        string? asmDir = Path.GetDirectoryName(assembly.Location);
        if (asmDir == null)
            return IntPtr.Zero;

        string dllPath = Path.Combine(asmDir, "oidn", "win-x64", $"{DllName}.dll");
        if (File.Exists(dllPath))
            return NativeLibrary.Load(dllPath);

        return IntPtr.Zero;
    }

    // ── Types ────────────────────────────────────────────────
    public const int OIDN_DEVICE_TYPE_DEFAULT = 0;
    public const int OIDN_DEVICE_TYPE_CPU = 1;

    public const int OIDN_ERROR_NONE = 0;

    public const int OIDN_FORMAT_FLOAT3 = 3;

    // ── Device ───────────────────────────────────────────────
    [DllImport(DllName)] public static extern IntPtr oidnNewDevice(int deviceType);
    [DllImport(DllName)] public static extern void oidnCommitDevice(IntPtr device);
    [DllImport(DllName)] public static extern void oidnReleaseDevice(IntPtr device);

    // ── Filter ───────────────────────────────────────────────
    [DllImport(DllName)] public static extern IntPtr oidnNewFilter(IntPtr device, string type);
    [DllImport(DllName)] public static extern void oidnReleaseFilter(IntPtr filter);

    [DllImport(DllName)] public static extern void oidnSetSharedFilterImage(
        IntPtr filter, string name,
        IntPtr ptr, int format,
        UIntPtr width, UIntPtr height,
        UIntPtr byteOffset,
        UIntPtr pixelByteStride, UIntPtr rowByteStride);

    [DllImport(DllName)] public static extern void oidnSetFilterBool(IntPtr filter, string name, bool value);
    [DllImport(DllName)] public static extern void oidnCommitFilter(IntPtr filter);
    [DllImport(DllName)] public static extern void oidnExecuteFilter(IntPtr filter);
}
