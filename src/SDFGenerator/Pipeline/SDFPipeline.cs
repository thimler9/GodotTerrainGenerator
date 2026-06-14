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
    private List<ISDFShader> SDFShaders = new List<ISDFShader>();

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

            switch (functionName)
            {
                case "SimplexNoise":
                    SDFShaders.Add(new SimplexNoiseShader(rd, shaderPath));
                    for (int i = 0; i < stages.Count; i++)
                    {
                        if (stages[i].Function == functionName)
                        {
                            stages[i].ShaderIndex = SDFShaders.Count - 1;
                        }
                    }
                    break;
            }
        }
    }

    public RDUniform GetSDF(SDFShaderParameters sdfShaderParameters)
    {
        SetSDFParameters(sdfShaderParameters);
        SetOutputBuffer(sdfShaderParameters.ChunkSize, sdfShaderParameters.Lod);

        foreach (ISDFPipelineStage stage in Stages)
        {
            ISDFShader sdfShader = SDFShaders[stage.ShaderIndex];
            sdfShader.Dispatch(sdfShaderParameters.ChunkSize, sdfShaderParameters.Lod, SDFParametersUniform, OutputUniform);
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
        foreach (ISDFShader stage in Stages)
        {
            stage.Dispose();
        }
    }
}
