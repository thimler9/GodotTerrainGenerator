using Godot;
using System;
using System.Runtime.InteropServices;

public partial class TestMapGenerator : Node3D
{
    public Vector3 ChunkOffset = new Vector3(128, 128, 128);
    public uint ChunkSize = 8;
    public uint Lod = 8;

    public override void _Ready()
    {
        RenderingDevice rd = RenderingServer.CreateLocalRenderingDevice();

        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Shaders/Compute/map_generator.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Rid shader = rd.ShaderCreateFromSpirV(shaderBytecode);

        Rid outputBuffer = rd.StorageBufferCreate(ChunkSize * ChunkSize * ChunkSize * sizeof(float));
        RDUniform outputBufferUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        outputBufferUniform.AddId(outputBuffer);


        Params input = new Params()
        {
            ChunkOffset = ChunkOffset,
            ChunkSize = ChunkSize,
            Lod = Lod
        };
        byte[] inputBytes = input.ToByteArray();

        Rid parametersBuffer = rd.UniformBufferCreate((uint)input.SizeOf(), inputBytes);
        RDUniform parametersUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        parametersUniform.AddId(parametersBuffer);

        Rid outputUniformSet = rd.UniformSetCreate([outputBufferUniform], shader, 0);
        Rid paramsUniformSet = rd.UniformSetCreate([parametersUniform], shader, 1); 

        // Create a compute pipeline
        Rid pipeline = rd.ComputePipelineCreate(shader);
        long computeList = rd.ComputeListBegin();
        rd.ComputeListBindComputePipeline(computeList, pipeline);
        rd.ComputeListBindUniformSet(computeList, outputUniformSet, 0);
        rd.ComputeListBindUniformSet(computeList, paramsUniformSet, 1);
        rd.ComputeListDispatch(computeList, xGroups: ChunkSize / 8, yGroups: ChunkSize / 8, zGroups: ChunkSize / 8);
        rd.ComputeListEnd();

        rd.Submit();
        rd.Sync();

        // Read back the data from the buffers
        var outputBytes = rd.BufferGetData(outputBuffer);
        var output = new float[ChunkSize];
        Buffer.BlockCopy(outputBytes, 0, output, 0, output.Length * sizeof(float));
        GD.Print("Output: ", string.Join(", ", output));

        rd.FreeRid(pipeline);
        rd.FreeRid(outputUniformSet);
        rd.FreeRid(paramsUniformSet);
        rd.FreeRid(parametersBuffer);
        rd.FreeRid(outputBuffer);
        rd.FreeRid(shader);
    }

    [StructLayout(LayoutKind.Explicit, Pack = 32)]
    private struct Params
    {
        [FieldOffset(0)]
        public Vector3 ChunkOffset;

        [FieldOffset(12)]
        private uint Padding;

        [FieldOffset(16)]
        public uint ChunkSize;

        [FieldOffset(24)]
        public uint Lod;

        [FieldOffset(28)]
        private Vector2 Padding2;

        public int SizeOf()
        {
            return Marshal.SizeOf<Params>();
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