using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline.GPU;

public sealed class TemperatureDescriptorGPU
{
    public IReadOnlyList<ISDFPipelineStageGPU> Sdfs { get; init; } = Array.Empty<ISDFPipelineStageGPU>();
}
