using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.CPUSDFs;

public interface ICpuSdfFunction
{
    string FunctionName { get; }

    void Dispatch(
        ICpuSdfParameters parameters,
        CPUSDFParameters sdfParameters,
        CPUBiomeParameters biomeParameters,
        float[] temperatureValues,
        out float[] output);
}
