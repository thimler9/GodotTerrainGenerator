using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;

namespace TerrainGeneration.Application.SDFGenerator.CPU.Sphere;

public sealed class SDF : ISDFFunctionCPU
{
    public string FunctionName => "Sphere";

    public void Dispatch(ISDFParametersCPU parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, out float[] output)
    {
        if (parameters is not SphereParameters)
        {
            throw new ArgumentException($"Expected parameters of type '{nameof(SphereParameters)}'.", nameof(parameters));
        }

        // TODO
        output = new float[0];
    }
}
