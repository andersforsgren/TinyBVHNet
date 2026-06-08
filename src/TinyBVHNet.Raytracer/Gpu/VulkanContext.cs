using System.IO;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace TinyBVHNet.Raytracer.Gpu;

/// <summary>
/// Manages a Vulkan compute pipeline for GPU path tracing.
/// Creates instance, device, command pool, descriptor sets, compute pipeline,
/// manages SSBO upload/readback, holds a fence, and dispatches compute work.
/// </summary>
public unsafe class VulkanContext : System.IDisposable
{
    private static readonly Vk Vk = Vk.GetApi();

    private Instance _instance;
    private PhysicalDevice _physicalDevice;
    private Device _device;
    private Queue _computeQueue;
    private uint _queueFamilyIndex;
    private CommandPool _commandPool;
    private CommandBuffer _commandBuffer;
    private Fence _fence;
    private DescriptorPool _descriptorPool;
    private DescriptorSetLayout _layout;
    private PipelineLayout _pipelineLayout;
    private Pipeline _pipeline;
    private ShaderModule _shaderModule;

    // SSBOs
    private Silk.NET.Vulkan.Buffer _nodesBuffer;
    private DeviceMemory _nodesMemory;
    private Silk.NET.Vulkan.Buffer _primsBuffer;
    private DeviceMemory _primsMemory;
    private Silk.NET.Vulkan.Buffer _vertsBuffer;
    private DeviceMemory _vertsMemory;
    private Silk.NET.Vulkan.Buffer _triDataBuffer;
    private DeviceMemory _triDataMemory;
    private Silk.NET.Vulkan.Buffer _outputBuffer;
    private DeviceMemory _outputMemory;

    private DescriptorSet _descriptorSet;
    private int _width, _height;
    private bool _disposed;

    static VulkanContext()
    {
        if (Vk is null)
            throw new System.Exception("Failed to load Vulkan API – ensure a Vulkan driver is installed.");
    }

    // ═══════════════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════════════

    public void Initialize(int width, int height)
    {
        _width = width; _height = height;
        CreateInstance();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateCommandPool();
        CreateShaderModule();
        CreateDescriptorSetLayout();
        CreatePipeline();
        CreateOutputBuffer();
        CreateFence();
    }

    public void UploadBvhData(byte[] nodes, byte[] prims, byte[] verts, byte[] triData)
    {
        CreateDeviceBuffer((ulong)nodes.Length, BufferUsageFlags.StorageBufferBit, out _nodesBuffer, out _nodesMemory);
        UploadToBuffer(_nodesBuffer, _nodesMemory, nodes);

        CreateDeviceBuffer((ulong)prims.Length, BufferUsageFlags.StorageBufferBit, out _primsBuffer, out _primsMemory);
        UploadToBuffer(_primsBuffer, _primsMemory, prims);

        CreateDeviceBuffer((ulong)verts.Length, BufferUsageFlags.StorageBufferBit, out _vertsBuffer, out _vertsMemory);
        UploadToBuffer(_vertsBuffer, _vertsMemory, verts);

        CreateDeviceBuffer((ulong)triData.Length, BufferUsageFlags.StorageBufferBit, out _triDataBuffer, out _triDataMemory);
        UploadToBuffer(_triDataBuffer, _triDataMemory, triData);

        CreateDescriptorSet();
    }

    public void Dispatch(GpuParams gpuParams)
    {
        GpuParams* pParams = &gpuParams;
        {
            var descSet = _descriptorSet;
            var cmdBuf = _commandBuffer;
            var fence = _fence;

            var beginInfo = new CommandBufferBeginInfo
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit
            };
            Vk.BeginCommandBuffer(cmdBuf, &beginInfo);

            Vk.CmdBindPipeline(cmdBuf, PipelineBindPoint.Compute, _pipeline);
            Vk.CmdBindDescriptorSets(cmdBuf, PipelineBindPoint.Compute,
                _pipelineLayout, 0, 1, &descSet, 0, null);

            Vk.CmdPushConstants(cmdBuf, _pipelineLayout,
                ShaderStageFlags.ComputeBit, 0, (uint)sizeof(GpuParams), pParams);

            uint gx = (uint)((_width + 7) / 8);
            uint gy = (uint)((_height + 7) / 8);
            Vk.CmdDispatch(cmdBuf, gx, gy, 1);
            Vk.EndCommandBuffer(cmdBuf);

            var submitInfo = new SubmitInfo
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &cmdBuf
            };

            Vk.ResetFences(_device, 1, &fence);
            Vk.QueueSubmit(_computeQueue, 1, &submitInfo, fence).ThrowOnError("QueueSubmit");
        }
    }

    public void WaitIdle()
    {
        var fence = _fence;
        Vk.WaitForFences(_device, 1, &fence, Vk.True, ulong.MaxValue);
    }

    public float[] ReadOutput()
    {
        int floatCount = _width * _height * 4;
        float[] result = new float[floatCount];
        ulong size = (ulong)(floatCount * sizeof(float));

        void* mapped;
        Vk.MapMemory(_device, _outputMemory, 0, size, 0, &mapped).ThrowOnError("MapMemory");
        Marshal.Copy((nint)mapped, result, 0, floatCount);
        Vk.UnmapMemory(_device, _outputMemory);

        return result;
    }

    public ulong GetBufferSize(Silk.NET.Vulkan.Buffer buffer)
    {
        Vk.GetBufferMemoryRequirements(_device, buffer, out MemoryRequirements reqs);
        return reqs.Size;
    }

    // ═══════════════════════════════════════════════════════════
    //  Initialization Steps
    // ═══════════════════════════════════════════════════════════

    private void CreateInstance()
    {
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = null,
            ApplicationVersion = 0,
            PEngineName = null,
            EngineVersion = 0,
            ApiVersion = Vk.Version12
        };

        // Vulkan 1.2 includes all needed features; no extra extensions required
        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = 0,
            PpEnabledExtensionNames = null
        };

        Vk.CreateInstance(&createInfo, null, out _instance).ThrowOnError("CreateInstance");
    }

    private void PickPhysicalDevice()
    {
        uint count = 0;
        Vk.EnumeratePhysicalDevices(_instance, &count, null);
        if (count == 0)
            throw new System.Exception("No Vulkan-capable GPU found.");

        var devices = new PhysicalDevice[count];
        fixed (PhysicalDevice* p = devices)
            Vk.EnumeratePhysicalDevices(_instance, &count, p);

        for (int i = 0; i < count; i++)
        {
            uint qCount = 0;
            Vk.GetPhysicalDeviceQueueFamilyProperties(devices[i], &qCount, null);
            var qProps = new QueueFamilyProperties[qCount];
            fixed (QueueFamilyProperties* pQP = qProps)
                Vk.GetPhysicalDeviceQueueFamilyProperties(devices[i], &qCount, pQP);

            for (uint j = 0; j < qCount; j++)
            {
                if ((qProps[j].QueueFlags & QueueFlags.ComputeBit) != 0)
                {
                    _physicalDevice = devices[i];
                    _queueFamilyIndex = j;
                    return;
                }
            }
        }
        throw new System.Exception("No GPU with a compute queue found.");
    }

    private void CreateLogicalDevice()
    {
        float priority = 1.0f;
        var qInfo = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = _queueFamilyIndex,
            QueueCount = 1,
            PQueuePriorities = &priority
        };

        var dInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &qInfo
        };

        Vk.CreateDevice(_physicalDevice, &dInfo, null, out _device).ThrowOnError("CreateDevice");
        Vk.GetDeviceQueue(_device, _queueFamilyIndex, 0, out _computeQueue);
    }

    private void CreateCommandPool()
    {
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _queueFamilyIndex,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit
        };
        Vk.CreateCommandPool(_device, &poolInfo, null, out _commandPool).ThrowOnError("CreateCommandPool");

        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };
        Vk.AllocateCommandBuffers(_device, &allocInfo, out _commandBuffer).ThrowOnError("AllocateCommandBuffers");
    }

    private void CreateShaderModule()
    {
        // Look for pathtrace.spv relative to the assembly or working directory
        string spvPath = Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory, "shaders", "pathtrace.spv");
        if (!File.Exists(spvPath))
            spvPath = Path.Combine(Directory.GetCurrentDirectory(), "shaders", "pathtrace.spv");
        if (!File.Exists(spvPath))
            throw new FileNotFoundException("Cannot find pathtrace.spv. Compile the GLSL shader with glslc and place it in the shaders/ directory.", spvPath);

        byte[] spvBytes = File.ReadAllBytes(spvPath);

        fixed (byte* pCode = spvBytes)
        {
            var moduleInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)spvBytes.Length,
                PCode = (uint*)pCode
            };
            Vk.CreateShaderModule(_device, &moduleInfo, null, out _shaderModule).ThrowOnError("CreateShaderModule");
        }
    }

    private void CreateDescriptorSetLayout()
    {
        var bindings = new DescriptorSetLayoutBinding[5];
        for (int i = 0; i < 5; i++)
        {
            bindings[i] = new DescriptorSetLayoutBinding
            {
                Binding = (uint)i,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ComputeBit
            };
        }

        fixed (DescriptorSetLayoutBinding* p = bindings)
        {
            var info = new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = 5,
                PBindings = p
            };
            Vk.CreateDescriptorSetLayout(_device, &info, null, out _layout).ThrowOnError("CreateDescriptorSetLayout");
        }
    }

    private void CreatePipeline()
    {
        // Pipeline layout with push constant
        var pushRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.ComputeBit,
            Offset = 0,
            Size = (uint)sizeof(GpuParams)
        };

        var layout = _layout;
        var plInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &layout,
            PushConstantRangeCount = 1,
            PPushConstantRanges = &pushRange
        };
        Vk.CreatePipelineLayout(_device, &plInfo, null, out _pipelineLayout).ThrowOnError("CreatePipelineLayout");

        // Shader stage
        byte* pEntry = (byte*)SilkMarshal.StringToPtr("main");
        var stageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.ComputeBit,
            Module = _shaderModule,
            PName = pEntry
        };

        var cpInfo = new ComputePipelineCreateInfo
        {
            SType = StructureType.ComputePipelineCreateInfo,
            Stage = stageInfo,
            Layout = _pipelineLayout
        };

        Vk.CreateComputePipelines(_device, default, 1, &cpInfo, null, out _pipeline).ThrowOnError("CreateComputePipeline");
        SilkMarshal.FreeString((nint)stageInfo.PName);
    }

    private void CreateOutputBuffer()
    {
        ulong size = (ulong)(_width * _height * 4 * sizeof(float));
        CreateDeviceBuffer(size,
            BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferSrcBit,
            out _outputBuffer, out _outputMemory);
    }

    private void CreateFence()
    {
        var info = new FenceCreateInfo { SType = StructureType.FenceCreateInfo };
        Vk.CreateFence(_device, &info, null, out _fence).ThrowOnError("CreateFence");
    }

    // ═══════════════════════════════════════════════════════════
    //  Descriptor Sets & Buffers
    // ═══════════════════════════════════════════════════════════

    private void CreateDescriptorSet()
    {
        var poolSize = new DescriptorPoolSize
        {
            Type = DescriptorType.StorageBuffer,
            DescriptorCount = 5
        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            MaxSets = 1,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize
        };
        Vk.CreateDescriptorPool(_device, &poolInfo, null, out _descriptorPool).ThrowOnError("CreateDescriptorPool");

        var layout2 = _layout;
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &layout2
        };
        Vk.AllocateDescriptorSets(_device, &allocInfo, out _descriptorSet).ThrowOnError("AllocateDescriptorSets");

        // Write all 5 bindings
        var bufferInfos = new DescriptorBufferInfo[5];
        var writes = new WriteDescriptorSet[5];

        for (int i = 0; i < 5; i++)
        {
            Silk.NET.Vulkan.Buffer buf = i switch
            {
                0 => _nodesBuffer,
                1 => _primsBuffer,
                2 => _vertsBuffer,
                3 => _outputBuffer,
                _ => _triDataBuffer  // binding 4
            };

            bufferInfos[i] = new DescriptorBufferInfo
            {
                Buffer = buf,
                Offset = 0,
                Range = Vk.WholeSize
            };

            writes[i] = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = _descriptorSet,
                DstBinding = (uint)i,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.StorageBuffer
            };
        }

        for (int i = 0; i < 5; i++)
        {
            var bi = bufferInfos[i];
            var write = writes[i];
            write.PBufferInfo = &bi;
            Vk.UpdateDescriptorSets(_device, 1, &write, 0, null);
        }
    }

    private void CreateDeviceBuffer(ulong size, BufferUsageFlags usage,
        out Silk.NET.Vulkan.Buffer buffer, out DeviceMemory memory)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };
        Vk.CreateBuffer(_device, &bufferInfo, null, out buffer).ThrowOnError("CreateBuffer");

        Vk.GetBufferMemoryRequirements(_device, buffer, out MemoryRequirements memReqs);

        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = FindMemoryType(memReqs.MemoryTypeBits,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit)
        };
        Vk.AllocateMemory(_device, &allocInfo, null, out memory).ThrowOnError("AllocateMemory");
        Vk.BindBufferMemory(_device, buffer, memory, 0).ThrowOnError("BindBufferMemory");
    }

    private uint FindMemoryType(uint typeBits, MemoryPropertyFlags properties)
    {
        Vk.GetPhysicalDeviceMemoryProperties(_physicalDevice, out PhysicalDeviceMemoryProperties memProps);
        for (uint i = 0; i < memProps.MemoryTypeCount; i++)
        {
            if ((typeBits & (1u << (int)i)) != 0 &&
                (memProps.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
                return i;
        }
        throw new System.Exception("Failed to find suitable Vulkan memory type.");
    }

    private void UploadToBuffer(Silk.NET.Vulkan.Buffer buffer, DeviceMemory memory, byte[] data)
    {
        void* mapped;
        Vk.MapMemory(_device, memory, 0, (ulong)data.Length, 0, &mapped).ThrowOnError("MapMemory");
        fixed (byte* pSrc = data)
            System.Buffer.MemoryCopy(pSrc, mapped, data.Length, data.Length);
        Vk.UnmapMemory(_device, memory);
    }

    // ═══════════════════════════════════════════════════════════
    //  Cleanup
    // ═══════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_device.Handle != 0)
        {
            Vk.DeviceWaitIdle(_device);
            Vk.DestroyFence(_device, _fence, null);
            Vk.DestroyPipeline(_device, _pipeline, null);
            Vk.DestroyPipelineLayout(_device, _pipelineLayout, null);
            Vk.DestroyDescriptorSetLayout(_device, _layout, null);
            Vk.DestroyDescriptorPool(_device, _descriptorPool, null);
            Vk.DestroyShaderModule(_device, _shaderModule, null);
            DestroyDeviceBuffer(_nodesBuffer, _nodesMemory);
            DestroyDeviceBuffer(_primsBuffer, _primsMemory);
            DestroyDeviceBuffer(_vertsBuffer, _vertsMemory);
            DestroyDeviceBuffer(_triDataBuffer, _triDataMemory);
            DestroyDeviceBuffer(_outputBuffer, _outputMemory);
            Vk.DestroyCommandPool(_device, _commandPool, null);
            Vk.DestroyDevice(_device, null);
        }

        if (_instance.Handle != 0)
            Vk.DestroyInstance(_instance, null);

        Vk.Dispose();
    }

    private void DestroyDeviceBuffer(Silk.NET.Vulkan.Buffer buffer, DeviceMemory memory)
    {
        if (buffer.Handle != 0) Vk.DestroyBuffer(_device, buffer, null);
        if (memory.Handle != 0) Vk.FreeMemory(_device, memory, null);
    }

    // ═══════════════════════════════════════════════════════════
    //  Push Constant Layout (matches shader)
    // ═══════════════════════════════════════════════════════════

    [StructLayout(LayoutKind.Sequential)]
    public struct GpuParams
    {
        public uint ImageWidth;
        public uint ImageHeight;
        public uint MaxBounces;
        public uint FrameSeed;
        public float CameraPosX, CameraPosY, CameraPosZ;
        public float CameraUX, CameraUY, CameraUZ;
        public float CameraVX, CameraVY, CameraVZ;
        public float CameraWX, CameraWY, CameraWZ;
        public float HalfWidth, HalfHeight;
        public float SunDirX, SunDirY, SunDirZ;
    }
}

internal static class VulkanResultExtensions
{
    public static void ThrowOnError(this Result result, string operation)
    {
        if (result != Result.Success)
            throw new System.Exception($"Vulkan {operation} failed: {result}");
    }
}
