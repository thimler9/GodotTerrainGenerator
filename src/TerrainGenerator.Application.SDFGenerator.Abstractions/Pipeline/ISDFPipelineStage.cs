namespace TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

public interface ISDFPipelineStage
{
    int ShaderIndex { get; set; }
    string Function { get; }
}
