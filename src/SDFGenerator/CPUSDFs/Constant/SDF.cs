using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;

namespace TerrainGeneration.Application.SDFGenerator.CPU.Constant;

public sealed class SDF : ISDFFunctionCPU
{
    public string FunctionName => "Constant";

    public void Dispatch(ISDFParametersCPU parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, out float[] output)
    {
        if (parameters is not ConstantParameters)
        {
            throw new ArgumentException($"Expected parameters of type '{nameof(ConstantParameters)}'.", nameof(parameters));
        }

        // TODO
        output = new float[0];
    }
}
