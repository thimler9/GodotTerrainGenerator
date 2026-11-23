using System.Numerics;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;
using TerrainGeneration.Application.SDFGenerator.CPUSDFs;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;
using TerrainGeneration.Utilities.Math.Noise;
using static TerrainGeneration.Utilities.Math.Noise.Simplex;

namespace TerrainGeneration.Application.SDFGenerator.CPU.SimplexNoise;

public sealed class SDF : ISDFFunctionCPU
{
    public string FunctionName => "SimplexNoise";

    public void Dispatch(ISDFParametersCPU parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, float[] output)
    {
        if (parameters is not SimplexNoiseParameters)
        {
            throw new ArgumentException($"Expected parameters of type '{nameof(SimplexNoiseParameters)}'.", nameof(parameters));
        }

        DispatchSimplexNoise((SimplexNoiseParameters)parameters, sdfParameters, biomeParameters, temperatureValues, output);
    }

    private void DispatchSimplexNoise(SimplexNoiseParameters parameters, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues, float[] output)
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

            SimplexNoiseParams simplexNoiseParams = new SimplexNoiseParams
            {
                Seed = parameters.Seed,
                Scale = parameters.Scale,
                Strength = parameters.Strength,
                NumOctaves = parameters.NumOctaves,
                Frequency = parameters.Frequency,
                Amplitude = parameters.Amplitude,
                Lacunarity = parameters.Lacunarity,
                Gain = parameters.Gain
            };
            Vector3 chunkOffset = new Vector3(sdfParameters.ChunkOffset.X, sdfParameters.ChunkOffset.Y, sdfParameters.ChunkOffset.Z);

            float noiseValue = Simplex.SimplexNoise(simplexNoiseParams, new Simplex.UInt3 { X = x, Y = y, Z = z }, chunkOffset, sdfParameters.Lod);

            output[i] += noiseValue * probability;
        }
    }
}
