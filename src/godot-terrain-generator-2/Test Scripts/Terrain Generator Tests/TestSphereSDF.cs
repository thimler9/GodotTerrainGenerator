using Godot;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;
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
        SphereSDFBasicTest();

        // Add more tests
    }


	private void SphereSDFBasicTest()
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

        var sdf = new TerrainGeneration.Application.SDFGenerator.CPU.Sphere.SDF();
        // Create parameters for the sphere
        var sphereParameters = new SphereParameters(radius, center, operationTypeCPU);
        var sdfParametersCPU = new SDFParametersCPU(chunkOffset, chunkSize, lod);
        var biomeParametersCPU = new TerrainGeneration.Application.SDFGenerator.Pipeline.CPU.BiomeParametersCPU(0, 0, 0, 0, true);
        float[] outputCPU = new float[bufferSize];
        ComputeCPUSDF(sdf, sdfParametersCPU, sphereParameters, biomeParametersCPU, outputCPU);

        RenderingDevice rd = RenderingServer.GetRenderingDevice();
        var shader = new TerrainGeneration.Application.SDFGenerator.GPU.Sphere.SphereShader(rd, "res://Shaders/Compute/sphere.glsl");
        ComputeBuffer outputBuffer = new ComputeBuffer(rd, sizeof(float) * bufferSize, RenderingDevice.UniformType.StorageBuffer, 0);

        var parametersGPU = new SphereShaderParameters(new TerrainGeneration.Application.SDFGenerator.GPU.Sphere.SphereStage()
        {
            Center = center,
            OperationType = operationTypeGPU,
            Radius = radius
        });
        var sdfParametersGPU = new SDFParametersGPU(chunkOffset, chunkSize, lod);
        var biomeParametersGPU = new BiomeParametersGPU(0, 0, 0, 0, true);
        ComputeGPUSDF(rd, chunkSize, lod, shader, sdfParametersGPU, parametersGPU, biomeParametersGPU, outputBuffer);


        // TEST
        float[] outputGPU = outputBuffer.GetData<float>();

        for (int i = 0; i < outputCPU.Length; i++)
        {
            if (!outputCPU[i].FuzzyEquals(outputGPU[i], 1e-4f))
            {
                GD.PrintErr($"Mismatch at index {i}: CPU = {outputCPU[i]}, GPU = {outputGPU[i]}");
            }
        }
    }


	private void ComputeCPUSDF(ISDFFunctionCPU sdf, SDFParametersCPU sdfParametersCPU, SphereParameters sphereParameters, BiomeParametersCPU biomeParameters, float[] output)
	{
		uint adjustedSize = (sdfParametersCPU.ChunkSize / sdfParametersCPU.Lod) + 2;
		uint bufferSize = adjustedSize * adjustedSize * adjustedSize;

        float[] temperatureValues = new float[1];
        sdf.Dispatch(sphereParameters, sdfParametersCPU, biomeParameters, temperatureValues, output);
    }

	private void ComputeGPUSDF(RenderingDevice rd, uint chunkSize, uint lod, ISDFShader shader, SDFParametersGPU sDFParameters, ISDFShaderParameters sphereShaderParameters, BiomeParametersGPU biomeParameters, ComputeBuffer outputBuffer)
	{
        uint adjustedSize = (chunkSize / lod) + 2;
        uint bufferSize = adjustedSize * adjustedSize * adjustedSize;

        byte[] sdfParametersGPUBytes = StructHelpers.ToByteArray(sDFParameters);

        var biomeParametersGPU = new BiomeParametersGPU(0, 0, 0, 0, true);
        byte[] biomeParamtersGPUBytes = StructHelpers.ToByteArray<BiomeParametersGPU>(biomeParametersGPU);

        ComputeBuffer sdfParametersBuffer = new ComputeBuffer(rd, sizeof(float) * 8, RenderingDevice.UniformType.UniformBuffer, 0, sdfParametersGPUBytes);
        ComputeBuffer biomeParametersBuffer = new ComputeBuffer(rd, sizeof(float) * 8, RenderingDevice.UniformType.UniformBuffer, 0, biomeParamtersGPUBytes);
        ComputeBuffer temperatureBuffer = new ComputeBuffer(rd, sizeof(float), RenderingDevice.UniformType.StorageBuffer, 0);

        shader.Dispatch(sDFParameters.ChunkSize, sDFParameters.Lod, sphereShaderParameters, sdfParametersBuffer, biomeParametersBuffer, temperatureBuffer, outputBuffer);

		sdfParametersBuffer.Dispose();
		biomeParametersBuffer.Dispose();
		temperatureBuffer.Dispose();
    }
}
