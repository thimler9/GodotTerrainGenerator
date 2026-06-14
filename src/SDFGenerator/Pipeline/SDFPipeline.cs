using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline;

public sealed class SDFPipeline
{
    private RenderingDevice Rd;
    private IReadOnlyList<ISDFPipelineStage> Stages { get; }

    /// <summary>
    /// Maps function name (e.g. "SimplexNoise") to a shader path.
    /// </summary>
    private IReadOnlyDictionary<string, string> FunctionShaderMap { get; }
    private readonly Dictionary<string, ISDFShader> SDFShadersByFunction = new(StringComparer.OrdinalIgnoreCase);

    // Used in dispatching the shaders
    SDFShaderParameters SDFShaderParameters;
    RDUniform SDFParametersUniform;
    Rid SDFParametersBuffer;

    RDUniform OutputUniform;
    Rid OutputBuffer;

    public SDFPipeline(IReadOnlyList<ISDFPipelineStage> stages, IReadOnlyDictionary<string, string> functionShaderMap, RenderingDevice rd)
    {
        Rd = rd;
        Stages = stages ?? throw new ArgumentNullException(nameof(stages));
        FunctionShaderMap = functionShaderMap ?? new Dictionary<string, string>();
        SetupSDFShaders(stages, functionShaderMap, rd);
    }

    private void SetupSDFShaders(IReadOnlyList<ISDFPipelineStage> stages, IReadOnlyDictionary<string, string> functionShaderMap, RenderingDevice rd)
    {
        foreach (var kvp in functionShaderMap)
        {
            string functionName = kvp.Key;
            string shaderPath = kvp.Value;

            ISDFShader shader = functionName switch
            {
                SimplexNoiseStage.FunctionName => new SimplexNoiseShader(rd, shaderPath),
                _ => throw new NotSupportedException($"Unsupported shader function '{functionName}'.")
            };

            SDFShadersByFunction[functionName] = shader;
        }
    }

    public RDUniform GetSDF(SDFShaderParameters sdfShaderParameters)
    {
        SetSDFParameters(sdfShaderParameters);
        SetOutputBuffer(sdfShaderParameters.ChunkSize, sdfShaderParameters.Lod);

        foreach (ISDFPipelineStage stage in Stages)
        {
            if (!SDFShadersByFunction.TryGetValue(stage.Function, out ISDFShader sdfShader))
            {
                throw new InvalidOperationException($"No shader registered for pipeline function '{stage.Function}'.");
            }

            sdfShader.Dispatch(
                sdfShaderParameters.ChunkSize,
                sdfShaderParameters.Lod,
                stage.CreateShaderParameters(),
                SDFParametersUniform,
                OutputUniform);
        }

        return OutputUniform;
    }

    /// <summary>
    /// Sets the parameters for the sdf shader params buffer
    /// </summary>
    /// <param name="parameters"></param>
    private void SetSDFParameters(SDFShaderParameters parameters)
    {
        if (!this.SDFShaderParameters.Equals(parameters))
        {
            // If the buffer isn't valid, we need to create one
            if (!SDFParametersBuffer.IsValid)
            {
                SDFParametersBuffer = Rd.UniformBufferCreate((uint)Marshal.SizeOf<SDFShaderParameters>());
                SDFParametersUniform = new RDUniform()
                {
                    UniformType = RenderingDevice.UniformType.UniformBuffer,
                    Binding = 0
                };
                SDFParametersUniform.AddId(SDFParametersBuffer);
            }

            Rd.BufferUpdate(SDFParametersBuffer, 0, (uint)Marshal.SizeOf<SDFShaderParameters>(), parameters.ToByteArray());
            SDFShaderParameters = parameters;
        }
    }

    private void SetOutputBuffer(uint chunkSize, uint lod)
    {
        if (!OutputBuffer.IsValid)
        {
            uint chunkSizeToLodRatio = chunkSize / lod;
            Rid outputBuffer = Rd.StorageBufferCreate((chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * sizeof(float));
            OutputUniform = new RDUniform()
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 0
            };
            OutputUniform.AddId(outputBuffer);
            OutputBuffer = outputBuffer;
        }
    }

    public void Dispose()
    {
        Rd.FreeRid(SDFParametersBuffer);
        Rd.FreeRid(OutputBuffer);
        foreach (ISDFShader shader in SDFShadersByFunction.Values)
        {
            shader.Dispose();
        }
    }
}
