using Godot;
using System;

public partial class Node3d : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RenderingDevice rd = RenderingServer.CreateLocalRenderingDevice();

		RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Shaders/Compute/compute_example.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Rid shader = rd.ShaderCreateFromSpirV(shaderBytecode);

        // Prepare our data. We use floats in the shader, so we need 32 bit.
        float[] input = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        byte[] inputBytes = new byte[input.Length * sizeof(float)];
        Buffer.BlockCopy(input, 0, inputBytes, 0, inputBytes.Length);

        // Create a storage buffer that can hold our float values.
        // Each float has 4 bytes (32 bit) so 10 x 4 = 40 bytes
        Rid buffer = rd.StorageBufferCreate((uint)inputBytes.Length, inputBytes);

        // Create a uniform to assign the buffer to the rendering device
        RDUniform uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        uniform.AddId(buffer);
        Rid uniformSet = rd.UniformSetCreate([uniform], shader, 0);

        // Create a compute pipeline
        Rid pipeline = rd.ComputePipelineCreate(shader);
        long computeList = rd.ComputeListBegin();
        rd.ComputeListBindComputePipeline(computeList, pipeline);
        rd.ComputeListBindUniformSet(computeList, uniformSet, 0);
        rd.ComputeListDispatch(computeList, xGroups: 5, yGroups: 1, zGroups: 1);
        rd.ComputeListEnd();

        rd.Submit();
        rd.Sync();

        // Read back the data from the buffers
        var outputBytes = rd.BufferGetData(buffer);
        var output = new float[input.Length];
        Buffer.BlockCopy(outputBytes, 0, output, 0, outputBytes.Length);
        GD.Print("Input: ", string.Join(", ", input));
        GD.Print("Output: ", string.Join(", ", output));

        rd.FreeRid(pipeline);
        rd.FreeRid(uniformSet);
        rd.FreeRid(buffer);
        rd.FreeRid(shader);
    }
}
