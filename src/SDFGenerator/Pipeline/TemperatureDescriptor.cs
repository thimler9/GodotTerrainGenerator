namespace TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

public sealed class TemperatureDescriptor
{
    public IReadOnlyList<ISDFPipelineStageGPU> Sdfs { get; init; } = Array.Empty<ISDFPipelineStageGPU>();
    public IReadOnlyList<ISDFPipelineStageCPU> CpuSdfs { get; init; } = Array.Empty<ISDFPipelineStageCPU>();
}
