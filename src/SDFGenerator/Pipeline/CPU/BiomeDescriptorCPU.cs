using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;
public class BiomeDescriptorCPU : BiomeDescriptor
{
    public IReadOnlyList<ISDFPipelineStageCPU> Sdfs { get; init; } = Array.Empty<ISDFPipelineStageCPU>();
}
