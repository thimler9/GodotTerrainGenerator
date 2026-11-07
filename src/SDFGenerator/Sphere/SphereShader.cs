using Godot;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Utilities.EngineAbstractions;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Application.SDFGenerator.Sphere;

public sealed class SphereShader : ISDFShader
{
    private const int PARAMETERS_SHADER_SET = 0;
    private const int SDF_PARAMETERS_SHADER_SET = 1;
    private const int BIOME_PARAMETERS_SHADER_SET = 2;
    private const int TEMPERATURE_VALUES_SHADER_SET = 3;
    private const int OUTPUT_SHADER_SET = 4;

    private readonly ComputeShader Shader;
    private readonly ComputeBuffer ParametersBuffer;
    private SphereShaderParameters? Parameters;

    public SphereShader(RenderingDevice rd, string shaderPath)
    {
        if (string.IsNullOrWhiteSpace(shaderPath))
        {
            throw new ArgumentNullException(nameof(shaderPath), "Cannot be null or whitespace");
        }

        if (rd == null)
        {
            throw new ArgumentNullException(nameof(rd), "Cannot be null");
        }

        Shader = new ComputeShader(rd, shaderPath);
        ParametersBuffer = new ComputeBuffer(rd, (uint)Marshal.SizeOf<SphereShaderParameters>(), RenderingDevice.UniformType.UniformBuffer, 0);
    }

    private void SetParameters(SphereShaderParameters parameters)
    {
        if (Parameters is not SphereShaderParameters existing || !existing.Equals(parameters))
        {
            ParametersBuffer.SetData(0, (uint)Marshal.SizeOf<SphereShaderParameters>(), StructHelpers.ToByteArray(parameters));
            Parameters = parameters;
        }
    }

    public void Dispatch(uint chunkSize, uint lod, IShaderParameters parameters, ComputeBuffer sdfParametersBuffer, ComputeBuffer biomeParamsBuffer, ComputeBuffer temperatureValuesBuffer, ComputeBuffer outputBuffer)
    {
        if (parameters is not SphereShaderParameters typedParameters)
        {
            throw new ArgumentException($"Expected {nameof(SphereShaderParameters)}.", nameof(parameters));
        }

        SetParameters(typedParameters);

        if (chunkSize / (8 * lod) == 0)
        {
            throw new ArgumentException($"{nameof(chunkSize)} / (8 * {nameof(lod)}) must be positive.");
        }

        using ComputePass pass = Shader.GetComputePass();
        pass.BindComputeBuffer(ParametersBuffer, PARAMETERS_SHADER_SET);
        pass.BindComputeBuffer(sdfParametersBuffer, SDF_PARAMETERS_SHADER_SET);
        pass.BindComputeBuffer(biomeParamsBuffer, BIOME_PARAMETERS_SHADER_SET);
        pass.BindComputeBuffer(temperatureValuesBuffer, TEMPERATURE_VALUES_SHADER_SET);
        pass.BindComputeBuffer(outputBuffer, OUTPUT_SHADER_SET);
        pass.Dispatch(chunkSize / (8 * lod) + 2, chunkSize / (8 * lod) + 2, chunkSize / (8 * lod) + 2);
    }

    public void Dispose()
    {
        Shader.Dispose();
        ParametersBuffer.Dispose();
    }
}
