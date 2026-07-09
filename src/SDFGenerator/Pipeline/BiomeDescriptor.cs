using Godot;
using TerrainGeneration.Utilities.EngineAbstractions;

namespace TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

public sealed class BiomeDescriptor
{
    public float Temperature { get; init; }
    public float TemperatureSpread { get; init; }
    public float Depth { get; init; }
    public float DepthSpread { get; init; }
    public bool IgnoreBiome { get; init; }
    public required ComputeBuffer BiomeParametersBuffer { get; set; }

    public IReadOnlyList<ISDFPipelineStage> Sdfs { get; init; } = Array.Empty<ISDFPipelineStage>();
}
