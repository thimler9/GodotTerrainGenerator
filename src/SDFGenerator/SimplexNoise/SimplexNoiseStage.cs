using Godot;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

namespace TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

public sealed class SimplexNoiseStage : ISDFPipelineStage
{
    public const string FunctionName = "SimplexNoise";

    public string Function => FunctionName;

    public uint Seed { get; init; }
    public float Scale { get; init; }
    public float Strength { get; init; }
    public uint NumOctaves { get; init; }
    public float Frequency { get; init; }
    public float Amplitude { get; init; }
    public float Lacunarity { get; init; }
    public float Gain { get; init; }

    public IShaderParameters CreateShaderParameters()
    {
        return new SimplexNoiseShaderParameters(this);
    }
}
