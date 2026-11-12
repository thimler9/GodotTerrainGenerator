using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPUSDFs;
using TerrainGeneration.Application.SDFGenerator.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.CPUSDFs.Sphere;

public sealed class SDF : ICpuSdfFunction
{
    public string FunctionName => "Sphere";

    public void Dispatch(ICpuSdfParameters parameters, CPUSDFParameters sdfParameters, CPUBiomeParameters biomeParameters, float[] temperatureValues, out float[] output)
    {
        if (parameters is not SphereParameters)
        {
            throw new ArgumentException($"Expected parameters of type '{nameof(SphereParameters)}'.", nameof(parameters));
        }

        // TODO
        output = new float[0];
    }
}
