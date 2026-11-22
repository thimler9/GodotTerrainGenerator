using Godot;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.CPU.Sphere;
using TerrainGeneration.Application.SDFGenerator.GPU.Sphere;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;
using TerrainGeneration.Application.SDFGenerator.Pipeline.GPU;
using TerrainGeneration.Utilities.EngineAbstractions;
using TerrainGeneration.Utilities.Math.Extensions;
using TerrainGeneration.Utilities.Struct;

public partial class TestSphereSDF : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// SDFParameters
		Vector3 chunkOffset = new Vector3(0, 0, 0);
		uint chunkSize = 128;
		uint lod = 2;
		uint bufferSize = (chunkSize / lod + 2) * (chunkSize / lod + 2) * (chunkSize / lod + 2);

        // SDF Parameters
        float radius = 5.0f;
		Vector3 center = new Vector3(0, 0, 0);
		TerrainGeneration.Application.SDFGenerator.CPU.OperationType operationTypeCPU = TerrainGeneration.Application.SDFGenerator.CPU.OperationType.Set;
		TerrainGeneration.Application.SDFGenerator.GPU.OperationType operationTypeGPU = TerrainGeneration.Application.SDFGenerator.GPU.OperationType.Set;

        // ======== CPU =========

        // Create a new instance of the SDF class
        var sdf = new TerrainGeneration.Application.SDFGenerator.CPU.Sphere.SDF();
		// Create parameters for the sphere
		var sphereParameters = new SphereParameters(radius, center, operationTypeCPU);
		var sdfParametersCPU = new SDFParametersCPU(chunkOffset, chunkSize, lod);
		var biomeParametersCPU = new TerrainGeneration.Application.SDFGenerator.Pipeline.CPU.BiomeParametersCPU(0, 0, 0, 0, true);

		float[] temperatureValues = new float[1];
		float[] output = new float[bufferSize];
		sdf.Dispatch(sphereParameters, sdfParametersCPU, biomeParametersCPU, temperatureValues, output);

		// ======== GPU =========

		// Dispatch the SDF function on the GPU
		RenderingDevice rd = RenderingServer.GetRenderingDevice();
        var sdfGPU = new TerrainGeneration.Application.SDFGenerator.GPU.Sphere.SphereShader(rd, "res://Shaders/Compute/sphere.glsl");

		var parametersGPU = new SphereShaderParameters(new TerrainGeneration.Application.SDFGenerator.GPU.Sphere.SphereStage()
		{
			Center = center,
			OperationType = operationTypeGPU,
			Radius = radius
		});

		var sdfParametersGPU = new SDFParameters(chunkOffset, chunkSize, lod);
		byte[] sdfParametersGPUBytes = StructHelpers.ToByteArray(sdfParametersGPU);

		var biomeParametersGPU = new BiomeParameters(0, 0, 0, 0, true);
		byte[] biomeParamtersGPUBytes = StructHelpers.ToByteArray<BiomeParameters>(biomeParametersGPU);

		ComputeBuffer sdfParametersBuffer = new ComputeBuffer(rd, sizeof(float) * 8, RenderingDevice.UniformType.UniformBuffer, 0, sdfParametersGPUBytes);
        ComputeBuffer biomeParatersBuffer = new ComputeBuffer(rd, sizeof(float) * 8, RenderingDevice.UniformType.UniformBuffer, 0, biomeParamtersGPUBytes);
        ComputeBuffer temperatureBuffer = new ComputeBuffer(rd, sizeof(float), RenderingDevice.UniformType.StorageBuffer, 0);
        ComputeBuffer outputBuffer = new ComputeBuffer(rd, sizeof(float) * bufferSize, RenderingDevice.UniformType.StorageBuffer, 0);

        sdfGPU.Dispatch(chunkSize, lod, parametersGPU, sdfParametersBuffer, biomeParatersBuffer, temperatureBuffer, outputBuffer);


		float[] outputGPU = outputBuffer.GetData<float>();

		if (output.Length != bufferSize)
		{
			GD.PrintErr("Output lengths do not match between CPU and GPU results.");
		}

		for (int i = 0; i < output.Length; i++)
		{
			if (!output[i].FuzzyEquals(outputGPU[i], 1e-4f))
			{
				GD.PrintErr($"Mismatch at index {i}: CPU = {output[i]}, GPU = {outputGPU[i]}");
            }
        }
    }
}
