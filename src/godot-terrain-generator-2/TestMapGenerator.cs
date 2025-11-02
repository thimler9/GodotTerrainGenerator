using Godot;
using System;
using System.Runtime.InteropServices;

public partial class TestMapGenerator : Node3D
{
    public Vector3 ChunkOffset = new Vector3(128, 128, 128);
    public uint ChunkSize = 512;
    public uint Lod = 8;

    public override void _Ready()
    {
        RenderingDevice rd = RenderingServer.CreateLocalRenderingDevice();

        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Shaders/Compute/map_generator.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Rid shader = rd.ShaderCreateFromSpirV(shaderBytecode);

        // Prepare our data. We use floats in the shader, so we need 32 bit.
        Params input = new Params()
        {
            ChunkOffset = ChunkOffset,
            ChunkSize = ChunkSize,
            Lod = Lod
        };

        // Create a storage buffer that can hold our float values.
        // Each float has 4 bytes (32 bit) so 10 x 4 = 40 bytes
        Rid buffer = rd.UniformBufferCreate((uint)input.SizeOf(), input.ToByteArray());

        Rid outputBuffer = rd.StorageBufferCreate(ChunkSize * ChunkSize * ChunkSize * sizeof(float));

        // Create a uniform to assign the buffer to the rendering device
        RDUniform uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        uniform.AddId(buffer);

        RDUniform uniform2 = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 1
        };

        Rid uniformSet = rd.UniformSetCreate([uniform, uniform2], shader, 0);

        // Create a compute pipeline
        Rid pipeline = rd.ComputePipelineCreate(shader);
        long computeList = rd.ComputeListBegin();
        rd.ComputeListBindComputePipeline(computeList, pipeline);
        rd.ComputeListBindUniformSet(computeList, uniformSet, 0);
        rd.ComputeListDispatch(computeList, xGroups: ChunkSize / 8, yGroups: ChunkSize / 8, zGroups: ChunkSize / 8);
        rd.ComputeListEnd();

        rd.Submit();
        rd.Sync();

        // Read back the data from the buffers
        var outputBytes = rd.BufferGetData(outputBuffer);
        var output = new float[10];
        Buffer.BlockCopy(outputBytes, 0, output, 0, outputBytes.Length);
        GD.Print("Input: ", string.Join(", ", input));
        GD.Print("Output: ", string.Join(", ", output));

        rd.FreeRid(pipeline);
        rd.FreeRid(uniformSet);
        rd.FreeRid(buffer);
        rd.FreeRid(outputBuffer);
        rd.FreeRid(shader);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Params
    {
        public Vector3 ChunkOffset;
        public uint ChunkSize;
        public uint Lod;
        public Vector3 Padding;

        public int SizeOf()
        {
            return 3 * sizeof(float) + sizeof(uint) + sizeof(uint) + 3 * sizeof(float);
        }

        public byte[] ToByteArray()
        {
            int size = SizeOf();
            byte[] arr = new byte[size];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(this, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return arr;
        }
    }
}
