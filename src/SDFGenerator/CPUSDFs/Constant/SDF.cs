using Godot;
using System.Numerics;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;
using TerrainGeneration.Application.SDFGenerator.CPUSDFs;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;
using TerrainGeneration.Utilities.Math.Extensions;

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

        DispatchConstantSIMD((ConstantParameters)parameters, sdfParameters, biomeParameters, temperatureValues, output);
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
        uint width = (uint)Vector<float>.Count; // Get the width of the SIMD vector
        uint adjustedSize = sdfParameters.ChunkSize / sdfParameters.Lod + 2;

        Vector<float> chunkOffsetY = new Vector<float>(sdfParameters.ChunkOffset.Y);
        Vector<uint> lod = new Vector<uint>(sdfParameters.Lod);
        Vector<float> parameterValue = new Vector<float>(parameters.Value);

        uint i = 0;
        for (; i + width < (uint)output.Length; i += width)
        {
            Vector<float> currentValue = new Vector<float>(output, (int)i); // First clump of float values
            Vector<float> operatedValue = currentValue;

            Vector<uint> indices = Vector<uint>.Indices + new Vector<uint>(i); // Indices for the current clump of values
            Vector<uint> xCoords = indices.Mod(new Vector<uint>(adjustedSize));
            Vector<uint> yCoords = (indices / new Vector<uint>(adjustedSize)).Mod(new Vector<uint>(adjustedSize));
            Vector<uint> zCoords = indices / (new Vector<uint>(adjustedSize) * new Vector<uint>(adjustedSize));

            Vector<float> depthValues = Vector.ConvertToSingle(yCoords * lod) + chunkOffsetY;
            Vector<float> probabilities = biomeParameters.IgnoreBiome 
                ? new Vector<float>(1.0f) 
                : BiomeHelpers.CalculateProbability(depthValues, biomeParameters.Depth, biomeParameters.DepthSpread);

            // Do the operation based on the OperationType
            if (parameters.OperationType == OperationType.Add)
            {
                operatedValue += parameterValue;
            }
            else if (parameters.OperationType == OperationType.Subtract)
            {
                operatedValue -= parameterValue;
            }
            else if (parameters.OperationType == OperationType.Multiply)
            {
                operatedValue *= parameterValue;
            }
            else if (parameters.OperationType == OperationType.Divide)
            {
                operatedValue /= parameterValue;
            }
            else if (parameters.OperationType == OperationType.Set)
            {
                operatedValue = parameterValue;
            }

            Vector<float> finalValues = Vector.Lerp(currentValue, operatedValue, probabilities);
            finalValues.CopyTo(output, (int)i);
        }

        // Handle any remaining elements that don't fit into a full SIMD vector
        for (; i < output.Length; i++)
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
}
