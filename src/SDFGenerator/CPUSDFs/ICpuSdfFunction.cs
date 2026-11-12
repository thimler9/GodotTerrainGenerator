using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;

namespace TerrainGeneration.Application.SDFGenerator.CPU;

public interface ICpuSdfFunction
{
    string FunctionName { get; }

    void Dispatch(
        ICpuSdfParameters parameters,
        SDFParametersCPU sdfParameters,
        BiomeParametersCPU biomeParameters,
        float[] temperatureValues,
        out float[] output);
}
