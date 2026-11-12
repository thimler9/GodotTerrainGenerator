using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPUSDFs;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;

namespace TerrainGeneration.Application.SDFGenerator.CPUSDFs.SimplexNoise;

public sealed class SDF : ICpuSdfFunction
{
    public string FunctionName => "SimplexNoise";

    public void Dispatch(ICpuSdfParameters parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, out float[] output)
    {
        if (parameters is not SimplexNoiseParameters)
        {
            throw new ArgumentException($"Expected parameters of type '{nameof(SimplexNoiseParameters)}'.", nameof(parameters));
        }

        // TODO NEED TO FIX OUTPUT
        output = new float[0];
    }
}
