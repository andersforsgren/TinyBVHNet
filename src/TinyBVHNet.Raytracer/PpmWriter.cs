using System.IO;

namespace TinyBVHNet.Raytracer;

/// <summary>
/// Writes images in Netpbm PPM (P6 binary) format -- no external dependencies needed.
/// </summary>
public static class PpmWriter
{
    public static void Write(string path, int width, int height, byte[] rgb)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var sw = new StreamWriter(fs, System.Text.Encoding.ASCII) { NewLine = "\n" };
        sw.Write($"P6\n{width} {height}\n255\n");
        sw.Flush();
        fs.Write(rgb, 0, rgb.Length);
    }
}
