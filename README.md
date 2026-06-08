# TinyBVHNet

.NET wrapper for the [TinyBVH](https://github.com/jbikker/tinybvh) library — a fast, lightweight BVH (Bounding Volume Hierarchy) construction and traversal library by [Jacco Bikker](https://github.com/jbikker).

Multi-targets `net48` and `net8.0`. Ships native binaries for **Windows x64/arm64** and **Linux x64/arm64**.

```csharp
using TinyBVHNet;
using System.Numerics;

// A single triangle with a ray from above
float[] vertices = { 0,0,0,1,  2,0,0,1,  0,2,0,1 };
using var bvh = new BVH();
bvh.Build(vertices, 1);

var hit = bvh.Intersect(
    new Vector3(0.5f, 0.5f, -1),
    new Vector3(0, 0, 1));

Console.WriteLine(hit.HasValue
    ? $"Hit at distance {hit.Value.Distance:F3}"
    : "Miss");
```

Available on NuGet: https://www.nuget.org/packages/TinyBVHNet/
