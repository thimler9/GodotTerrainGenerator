using Godot;

namespace TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

public sealed class BiomeDescriptor
{
    public float Temperature { get; init; }
    public float TemperatureSpread { get; init; }
    public float Depth { get; init; }
    public float DepthSpread { get; init; }
    public bool IgnoreBiome { get; init; }
    public RDUniform? BiomeParametersUniform { get; set; }
    public Rid BiomeParametersBuffer { get; set; }

    public IReadOnlyList<ISDFPipelineStage> Sdfs { get; init; } = Array.Empty<ISDFPipelineStage>();
}
