using Godot;
using System.Numerics;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;
using TerrainGeneration.Application.SDFGenerator.CPUSDFs;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;
using TerrainGeneration.Utilities.Math.Extensions;

namespace TerrainGeneration.Application.SDFGenerator.CPU.Sphere;

public class SDF : ISDFFunctionCPU
{
    public string FunctionName => "Sphere";

    public void Dispatch(ISDFParametersCPU parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, float[] output)
    {
        if (parameters is not SphereParameters)
        {
            throw new ArgumentException($"Expected parameters of type '{nameof(SphereParameters)}'.", nameof(parameters));
        }

        // TODO
        DispatchSphereSIMD((SphereParameters)parameters, sdfParameters, biomeParameters, temperatureValues, output);
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
            Godot.Vector3 worldPosition = sdfParameters.ChunkOffset + new Godot.Vector3(x * sdfParameters.Lod, y * sdfParameters.Lod, z * sdfParameters.Lod);
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

    private void DispatchSphereSIMD(SphereParameters parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, float[] output)
    {
        uint width = (uint)Vector<float>.Count; // Get the width of the SIMD vector
        uint adjustedSize = sdfParameters.ChunkSize / sdfParameters.Lod + 2;

        Vector<float> chunkOffsetX = new Vector<float>(sdfParameters.ChunkOffset.X);
        Vector<float> chunkOffsetY = new Vector<float>(sdfParameters.ChunkOffset.Y);
        Vector<float> chunkOffsetZ = new Vector<float>(sdfParameters.ChunkOffset.Z);
        Vector<uint> lod = new Vector<uint>(sdfParameters.Lod);
        Vector<float> radius = new Vector<float>(parameters.Radius);
        Vector<float> centerX = new Vector<float>(parameters.Center.X);
        Vector<float> centerY = new Vector<float>(parameters.Center.Y);
        Vector<float> centerZ = new Vector<float>(parameters.Center.Z);

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

            // Get the world Position
            Vector<float> worldPositionX = chunkOffsetX + Vector.ConvertToSingle(xCoords * lod);
            Vector<float> worldPositionY = chunkOffsetY + Vector.ConvertToSingle(yCoords * lod);
            Vector<float> worldPositionZ = chunkOffsetZ + Vector.ConvertToSingle(zCoords * lod);

            // Calculate the distance from the sphere for each point in the clump
            Vector<float> dx = worldPositionX - centerX;
            Vector<float> dy = worldPositionY - centerY;
            Vector<float> dz = worldPositionZ - centerZ;
            Vector<float> sphereDistance = Vector.SquareRoot(dx * dx + dy * dy + dz * dz) - radius;

            // Do the operation based on the OperationType
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
            if (probability <= 0.0f)
            {
                continue;
            }

            float currentValue = output[i];
            float operatedValue = currentValue;
            Godot.Vector3 worldPosition = sdfParameters.ChunkOffset + new Godot.Vector3(x * sdfParameters.Lod, y * sdfParameters.Lod, z * sdfParameters.Lod);
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
