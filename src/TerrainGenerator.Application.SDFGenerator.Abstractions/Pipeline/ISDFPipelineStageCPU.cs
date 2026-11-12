namespace TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

public interface ISDFPipelineStageCPU
{
    string Function { get; }
    ISDFParametersCPU CreateParameters();
}
