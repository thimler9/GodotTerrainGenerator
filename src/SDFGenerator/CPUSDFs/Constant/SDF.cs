using TerrainGeneration.Application.SDFGenerator.Pipeline;

using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPUSDFs;
using TerrainGeneration.Application.SDFGenerator.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.CPUSDFs.Constant;

public sealed class SDF : ICpuSdfFunction
{
    public string FunctionName => "Constant";

    public void Dispatch(ICpuSdfParameters parameters, CPUSDFParameters sdfParameters, CPUBiomeParameters biomeParameters, float[] temperatureValues, out float[] output)
    {
        if (parameters is not ConstantParameters)
        {
            throw new ArgumentException($"Expected parameters of type '{nameof(ConstantParameters)}'.", nameof(parameters));
        }

        // TODO
        output = new float[0];
    }
}
