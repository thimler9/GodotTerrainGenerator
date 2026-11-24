using Godot;
using System.Numerics;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;
using TerrainGeneration.Application.SDFGenerator.CPUSDFs;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;

namespace TerrainGeneration.Application.SDFGenerator.CPU.Constant;

public sealed class SDF : ISDFFunctionCPU
{
    public string FunctionName => "Constant";

    public void Dispatch(ISDFParametersCPU parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, float[] output)
    {
        if (parameters is not ConstantParameters)
        {
            throw new ArgumentException($"Expected parameters of type '{nameof(ConstantParameters)}'.", nameof(parameters));
        }

        DispatchConstant((ConstantParameters)parameters, sdfParameters, biomeParameters, temperatureValues, output);
    }


    private void DispatchConstant(ConstantParameters parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, float[] output)
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

            float currentValue = output[i];
            float operatedValue = currentValue;
            if (parameters.OperationType == OperationType.Add)
            {
                operatedValue += parameters.Value;
            }
            else if (parameters.OperationType == OperationType.Subtract)
            {
                operatedValue -= parameters.Value;
            }
            else if (parameters.OperationType == OperationType.Multiply)
            {
                operatedValue *= parameters.Value;
            }
            else if (parameters.OperationType == OperationType.Divide)
            {
                operatedValue /= parameters.Value;
            }
            else if (parameters.OperationType == OperationType.Set)
            {
                operatedValue = parameters.Value;
            }

            output[i] = Mathf.Lerp(currentValue, operatedValue, probability);
        }
    }

    private void DispatchConstantSIMD(ConstantParameters parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, float[] output)
    {
        int width = Vector<float>.Count; // Get the width of the SIMD vector

        for (int i = 0; i < output.Length - width; i += width)
        {
            Vector<float> v = new Vector<float>(output, i); // First clump of float values

            // Calculate the probabilities of the current clump of values

            // Do the operation based on the OperationType
            if (parameters.OperationType == OperationType.Add)
            {
                v += new Vector<float>(parameters.Value);
            }
            else if (parameters.OperationType == OperationType.Subtract)
            {
                v -= new Vector<float>(parameters.Value);
            }
            else if (parameters.OperationType == OperationType.Multiply)
            {
                v *= new Vector<float>(parameters.Value);
            }
            else if (parameters.OperationType == OperationType.Divide)
            {
                v /= new Vector<float>(parameters.Value);
            }
            else if (parameters.OperationType == OperationType.Set)
            {
                v = new Vector<float>(parameters.Value);
            }

            // Write values back into the output array
            for (int j = 0 ; j < width; j++)
            {
                output[i] = v[i];
            }
        }
    }
}
