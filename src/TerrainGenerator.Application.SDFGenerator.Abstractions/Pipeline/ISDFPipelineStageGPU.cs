namespace TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

public interface ISDFPipelineStageGPU
{
    string Function { get; }
    ISDFShaderParameters CreateShaderParameters();
}
