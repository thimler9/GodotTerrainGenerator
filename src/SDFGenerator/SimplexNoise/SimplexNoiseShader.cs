using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Utilities.EngineAbstractions;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Application.SDFGenerator.SimplexNoise;
public class SimplexNoiseShader : ISDFShader
{
    private const int PARAMETERS_SHADER_SET = 0;
    private const int SDF_PARAMETERS_SHADER_SET = 1;
    private const int BIOME_PARAMETERS_SHADER_SET = 2;
    private const int TEMPERATURE_VALUES_SHADER_SET = 3;
    private const int OUTPUT_SHADER_SET = 4;

    private RenderingDevice Rd;
    private ComputeShader Shader;
    private ComputeBuffer ParametersBuffer;

    private SimplexNoiseShaderParameters? Parameters = null;

    /// <summary>
    /// Creates a SimplexNoiseShader. Used to take in the map buffer, and apply the inputted simplex noise to the map. 
    /// </summary>
    /// <param name="rd"></param>
    /// <param name="shaderPath"></param>
    /// <param name="parameters"></param>
    /// <param name="outputUniformSet"></param>
    public SimplexNoiseShader(RenderingDevice rd, string shaderPath)
    {
        if (string.IsNullOrWhiteSpace(shaderPath))
        {
            throw new ArgumentNullException(nameof(shaderPath), "Cannot be null or whitespace");
        }

        if (rd == null)
        {
            throw new ArgumentNullException(nameof(rd), "Cannot be null");
        }

        Rd = rd;
        Shader = new ComputeShader(rd, shaderPath);
        ParametersBuffer = new ComputeBuffer(rd, (uint)Marshal.SizeOf<SimplexNoiseShaderParameters>(), RenderingDevice.UniformType.UniformBuffer, 0);
    }

    /// <summary>
    /// Sets the parameters buffer to the new inputted parameters. Use somewhat sparingly, does CPU->GPU
    /// </summary>
    /// <param name="rd"></param>
    /// <param name="parameters"></param>
    private void SetParameters(SimplexNoiseShaderParameters parameters)
    {
        if (Parameters is not SimplexNoiseShaderParameters existing || !existing.Equals(parameters))
        {
            ParametersBuffer.SetData(0, (uint)Marshal.SizeOf<SimplexNoiseShaderParameters>(), StructHelpers.ToByteArray(parameters));
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Dispatches shader on the given compute list.
    /// </summary>
    /// <param name="computeList"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Dispatch(uint chunkSize, uint lod, IShaderParameters parameters, ComputeBuffer sdfParametersBuffer, ComputeBuffer biomeParamsBuffer, ComputeBuffer temperatureValuesBuffer, ComputeBuffer outputBuffer)
    {
        if (parameters is not SimplexNoiseShaderParameters typedParameters)
        {
            throw new ArgumentException($"Expected {nameof(SimplexNoiseShaderParameters)} for {nameof(SimplexNoiseShader)}.", nameof(parameters));
        }

        SetParameters((SimplexNoiseShaderParameters)parameters);

        if (chunkSize / (8 * lod) == 0)
        {
            throw new ArgumentException($"{nameof(chunkSize)} / (8 * {nameof(lod)} must be positive. {nameof(chunkSize)} = {chunkSize}, {nameof(lod)} = {lod}");
        }

        using ComputePass pass = Shader.GetComputePass();
        pass.BindComputeBuffer(ParametersBuffer, PARAMETERS_SHADER_SET);
        pass.BindComputeBuffer(sdfParametersBuffer, SDF_PARAMETERS_SHADER_SET);
        pass.BindComputeBuffer(biomeParamsBuffer, BIOME_PARAMETERS_SHADER_SET);
        pass.BindComputeBuffer(temperatureValuesBuffer, TEMPERATURE_VALUES_SHADER_SET);
        pass.BindComputeBuffer(outputBuffer, OUTPUT_SHADER_SET);
        pass.Dispatch(chunkSize / (8 * lod) + 2, chunkSize / (8 * lod) + 2, chunkSize / (8 * lod) + 2);
    }

    /// <summary>
    /// Disposes all necessary resources for the shader
    /// </summary>
    /// <param name="Rd"></param>
    public void Dispose()
    {
        this.Shader.Dispose();
        ParametersBuffer.Dispose();
    }
}
