using Godot;
using System;
using System.Collections.Generic;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline;

public sealed class SDFPipeline
{
    private IReadOnlyList<ISDFPipelineStage> Stages { get; }

    /// <summary>
    /// Maps function name (e.g. "SimplexNoise") to a shader path.
    /// </summary>
    private IReadOnlyDictionary<string, string> FunctionShaderMap { get; }


    private List<ISDFShader> SDFShaders = new List<ISDFShader>();

    public SDFPipeline(IReadOnlyList<ISDFPipelineStage> stages, IReadOnlyDictionary<string, string> functionShaderMap, RenderingDevice rd)
    {
        Stages = stages ?? throw new ArgumentNullException(nameof(stages));
        FunctionShaderMap = functionShaderMap ?? new Dictionary<string, string>();
        SetupSDFShaders(stages, functionShaderMap, rd);

        foreach (var stage in Stages)
        {

        }
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
}
