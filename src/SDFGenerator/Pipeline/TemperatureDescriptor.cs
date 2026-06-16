namespace TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

public sealed class TemperatureDescriptor
{
    public IReadOnlyList<ISDFPipelineStage> Sdfs { get; init; } = Array.Empty<ISDFPipelineStage>();
}
