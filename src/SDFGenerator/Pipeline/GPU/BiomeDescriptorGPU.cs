using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.Pipeline;
using TerrainGeneration.Utilities.EngineAbstractions;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline.GPU;

public sealed class BiomeDescriptorGPU : BiomeDescriptor
{
    public ComputeBuffer? BiomeParametersBuffer { get; set; }
    public IReadOnlyList<ISDFPipelineStageGPU> Sdfs { get; init; } = Array.Empty<ISDFPipelineStageGPU>();
}
