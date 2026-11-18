using Godot;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;
using TerrainGeneration.Application.SDFGenerator.CPUSDFs;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;

namespace TerrainGeneration.Application.SDFGenerator.CPU.Sphere;

public sealed class SDF : ISDFFunctionCPU
{
    public string FunctionName => "Sphere";

    public void Dispatch(ISDFParametersCPU parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, float[] output)
    {
        if (parameters is not SphereParameters)
        {
            throw new ArgumentException($"Expected parameters of type '{nameof(SphereParameters)}'.", nameof(parameters));
        }

        // TODO
        DispatchSphere((SphereParameters)parameters, sdfParameters, biomeParameters, temperatureValues, output);
    }

    private void DispatchSphere(SphereParameters parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, float[] output)
    {
        uint adjustedSize = sdfParameters.ChunkSize / sdfParameters.Lod + 2;
        // O(n^3) will hopefully add parallel processing later
        for (uint i = 0; i < adjustedSize * adjustedSize * adjustedSize; i++)
        {
            uint x = i % adjustedSize;
            uint y = (i / adjustedSize) % adjustedSize;
            uint z = i / (adjustedSize * adjustedSize);
            
            float depthValue = (float)(y * sdfParameters.Lod) + sdfParameters.ChunkOffset.Y;
            float probability = biomeParameters.IgnoreBiome 
                ? 1.0f 
                : BiomeHelpers.CalculateProbability(depthValue, biomeParameters.Depth, biomeParameters.DepthSpread);
            if (probability <= 0.0f)
            {
                continue;
            }

            float currentValue = output[i];
            float operatedValue = currentValue;
            Vector3 worldPosition = sdfParameters.ChunkOffset + new Vector3(x * sdfParameters.Lod, y * sdfParameters.Lod, z * sdfParameters.Lod);
            float sphereDistance = (worldPosition - parameters.Center).Length() - parameters.Radius;

            if (parameters.OperationType == OperationType.Add)
            {
                operatedValue += sphereDistance;
            }
            else if (parameters.OperationType == OperationType.Subtract)
            {
                operatedValue -= sphereDistance;
            }
            else if (parameters.OperationType == OperationType.Multiply)
            {
                operatedValue *= sphereDistance;
            }
            else if (parameters.OperationType == OperationType.Divide)
            {
                operatedValue /= sphereDistance;
            }
            else if (parameters.OperationType == OperationType.Set)
            {
                operatedValue = sphereDistance;
            }
            output[i] = Mathf.Lerp(currentValue, operatedValue, probability);
        }
    }
}
