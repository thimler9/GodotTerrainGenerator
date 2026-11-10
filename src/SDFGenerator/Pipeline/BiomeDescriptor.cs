using Godot;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;
using TerrainGeneration.Utilities.EngineAbstractions;

namespace TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

public sealed class BiomeDescriptor
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public float Temperature { get; init; }
    public float TemperatureSpread { get; init; }
    public float Depth { get; init; }
    public float DepthSpread { get; init; }
    public bool IgnoreBiome { get; init; }
    public ComputeBuffer? BiomeParametersBuffer { get; set; }
    public IReadOnlyList<ISDFPipelineStageGPU> Sdfs { get; init; } = Array.Empty<ISDFPipelineStageGPU>();
}
