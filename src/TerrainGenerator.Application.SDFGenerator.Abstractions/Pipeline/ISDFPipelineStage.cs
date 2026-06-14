namespace TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

public interface ISDFPipelineStage
{
    string Function { get; }
    IShaderParameters CreateShaderParameters();
}
