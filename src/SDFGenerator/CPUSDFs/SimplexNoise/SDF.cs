using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;

namespace TerrainGeneration.Application.SDFGenerator.CPU.SimplexNoise;

public sealed class SDF : ISDFFunctionCPU
{
    public string FunctionName => "SimplexNoise";

    public void Dispatch(ISDFParametersCPU parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, out float[] output)
    {
        if (parameters is not SimplexNoiseParameters)
        {
            throw new ArgumentException($"Expected parameters of type '{nameof(SimplexNoiseParameters)}'.", nameof(parameters));
        }

        // TODO NEED TO FIX OUTPUT
        output = new float[0];
    }
}
