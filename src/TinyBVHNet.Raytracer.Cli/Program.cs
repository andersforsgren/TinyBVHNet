using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using TinyBVHNet.Raytracer;

namespace TinyBVHNet.Raytracer.Cli;

/// <summary>
/// Unified path tracer CLI: CPU (default) or GPU (--gpu) rendering.
/// Usage: TinyBVHNet.Raytracer.Cli --scene model.obj --out render.ppm --camera 0 2 -5
/// </summary>
public static class Program
{
    private static void PrintUsage()
    {
        Console.WriteLine("Usage: TinyBVHNet.Raytracer.Cli --scene <path.obj> --out <output.ppm> --camera <x> <y> <z> [options]");
        Console.WriteLine();
        Console.WriteLine("Monochrome multi-bounce path tracer with NEE lighting (CPU or GPU).");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --scene    Path to OBJ file (triangulated)");
        Console.WriteLine("  --out      Output PPM image file");
        Console.WriteLine("  --camera   Camera position (X Y Z), looks at origin by default");
        Console.WriteLine("  --cpu      Use CPU path tracer (default)");
        Console.WriteLine("  --gpu      Use GPU (Vulkan compute) path tracer");
        Console.WriteLine("  --lookat   Look-at point (X Y Z), default: 0 0 0");
        Console.WriteLine("  --fov      Vertical field of view in degrees, default: 60");
        Console.WriteLine("  --width    Image width in pixels, default: 800");
        Console.WriteLine("  --height   Image height in pixels, default: 600");
        Console.WriteLine("  --spp      Samples per pixel, default: 16");
        Console.WriteLine("  --bounces  Maximum indirect bounces, default: 4");
        Console.WriteLine("  --exposure Exposure multiplier for tonemapping, default: 1.0");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  CPU:  TinyBVHNet.Raytracer.Cli --scene cube.obj --out cpu.ppm --camera 0 1 -3 --spp 64");
        Console.WriteLine("  GPU:  TinyBVHNet.Raytracer.Cli --gpu --scene cube.obj --out gpu.ppm --camera 0 1 -3 --spp 64");
    }

    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            PrintUsage();
            return 0;
        }

        // Parse arguments
        string? scenePath = null;
        string? outPath = null;
        Vector3 cameraPos = new(0, 1, -3);
        Vector3 lookAt = new(0, 0, 0);
        float fov = 60f;
        int width = 800;
        int height = 600;
        int spp = 16;
        int maxBounces = 4;
        float exposure = 1f;
        bool useGpu = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--scene":
                    scenePath = args[++i];
                    break;
                case "--out":
                    outPath = args[++i];
                    break;
                case "--cpu":
                    useGpu = false;
                    break;
                case "--gpu":
                    useGpu = true;
                    break;
                case "--camera":
                    cameraPos = new Vector3(
                        float.Parse(args[++i], CultureInfo.InvariantCulture),
                        float.Parse(args[++i], CultureInfo.InvariantCulture),
                        float.Parse(args[++i], CultureInfo.InvariantCulture));
                    break;
                case "--lookat":
                    lookAt = new Vector3(
                        float.Parse(args[++i], CultureInfo.InvariantCulture),
                        float.Parse(args[++i], CultureInfo.InvariantCulture),
                        float.Parse(args[++i], CultureInfo.InvariantCulture));
                    break;
                case "--fov":
                    fov = float.Parse(args[++i], CultureInfo.InvariantCulture);
                    break;
                case "--width":
                    width = int.Parse(args[++i]);
                    break;
                case "--height":
                    height = int.Parse(args[++i]);
                    break;
                case "--spp":
                    spp = int.Parse(args[++i]);
                    break;
                case "--bounces":
                    maxBounces = int.Parse(args[++i]);
                    break;
                case "--exposure":
                    exposure = float.Parse(args[++i], CultureInfo.InvariantCulture);
                    break;
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[i]}");
                    return 1;
            }
        }

        // Validate
        if (scenePath is null)
        {
            Console.Error.WriteLine("Error: --scene is required.");
            return 1;
        }
        if (outPath is null)
        {
            Console.Error.WriteLine("Error: --out is required.");
            return 1;
        }
        if (!File.Exists(scenePath))
        {
            Console.Error.WriteLine($"Error: Scene file not found: {scenePath}");
            return 1;
        }

        try
        {
            // Load scene
            Console.WriteLine($"Loading OBJ: {scenePath}");
            var scene = ObjParser.Load(scenePath);
            Console.WriteLine($"  Triangles: {scene.TriangleCount}");

            if (useGpu)
            {
                RenderGpu(scene, scenePath, outPath, cameraPos, lookAt, fov, width, height, spp, maxBounces, exposure);
            }
            else
            {
                RenderCpu(scene, outPath, cameraPos, lookAt, fov, width, height, spp, maxBounces, exposure);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static void RenderCpu(
        ObjParser.Scene scene, string outPath,
        Vector3 cameraPos, Vector3 lookAt, float fov,
        int width, int height, int spp, int maxBounces, float exposure)
    {
        Console.WriteLine($"CPU path tracing {width}x{height} with {spp} spp, {maxBounces} bounces...");

        // Build BVH and create renderer
        Console.WriteLine("Building BVH...");
        using var renderer = new Cpu.CpuRenderer(scene, width, height);

        var camera = new Camera(cameraPos, lookAt, Vector3.UnitY, fov);
        var opts = new RenderOptions(maxBounces);

        // Progressive accumulation: each Render() call is 1spp raw HDR
        float[] accum = new float[width * height];
        var totalSw = System.Diagnostics.Stopwatch.StartNew();

        for (int s = 0; s < spp; s++)
        {
            float[] hdr = renderer.Render(camera, opts);
            for (int i = 0; i < accum.Length; i++)
                accum[i] += hdr[i];
        }

        totalSw.Stop();
        long totalRays = renderer.TotalRays;
        double mrays = totalRays / (totalSw.Elapsed.TotalSeconds * 1e6);
        Console.WriteLine($"  CPU total: {totalSw.Elapsed.TotalSeconds:F1}s | {mrays:F2} MRay/s");

        // Tonemap and convert to bytes
        Console.WriteLine("Tonemapping and writing output...");
        RaytracerUtil.TonemapReinhard(accum, 1f / spp, exposure);
        var rgb = RaytracerUtil.LdrToRgbBytes(accum, width * height);

        PpmWriter.Write(outPath, width, height, rgb);
        Console.WriteLine($"Done. Output: {outPath}");
    }

    private static void RenderGpu(
        ObjParser.Scene scene, string scenePath, string outPath,
        Vector3 cameraPos, Vector3 lookAt, float fov,
        int width, int height, int spp, int maxBounces, float exposure)
    {
        Console.WriteLine($"GPU path tracing {width}x{height} with {spp} spp, {maxBounces} bounces...");

        using var renderer = new Gpu.GpuRenderer(scene, width, height, msg => Console.WriteLine(msg));
        var camera = new Camera(cameraPos, lookAt, Vector3.UnitY, fov);
        var opts = new RenderOptions(maxBounces);

        // Progressive accumulation: each Render() call is 1spp raw HDR
        float[] accum = new float[width * height * 3];
        var totalSw = System.Diagnostics.Stopwatch.StartNew();

        for (int s = 0; s < spp; s++)
        {
            var hdr = renderer.Render(camera, opts);
            for (int i = 0; i < accum.Length; i++)
                accum[i] += hdr[i];
        }

        totalSw.Stop();
        TimeSpan totalGpu = totalSw.Elapsed;
        long estimatedRays = (long)width * height * spp * (1 + 2 * maxBounces);
        double mrays = estimatedRays / (totalGpu.TotalSeconds * 1e6);
        Console.WriteLine($"  GPU total: {totalGpu.TotalSeconds:F3}s | {mrays:F2} MRay/s");

        // Tonemap and convert to bytes (GPU returns interleaved RGB — extract green as mono)
        Console.WriteLine("Tonemapping and writing output...");
        var ldr = new float[width * height];
        for (int i = 0; i < width * height; i++)
        {
            float avg = accum[i * 3 + 0] / spp; // any channel works (monochrome)
            ldr[i] = RaytracerUtil.TonemapReinhard(avg, exposure);
        }
        var rgb = RaytracerUtil.LdrToRgbBytes(ldr, width * height);

        PpmWriter.Write(outPath, width, height, rgb);
        Console.WriteLine($"Done. Output: {outPath}");
    }
}
