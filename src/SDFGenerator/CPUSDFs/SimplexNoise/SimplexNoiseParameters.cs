using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.CPU.SimplexNoise;

public sealed class SimplexNoiseParameters : ICpuSdfParameters
{
    public uint Seed { get; }
    public float Scale { get; }
    public float Strength { get; }
    public uint NumOctaves { get; }
    public float Frequency { get; }
    public float Amplitude { get; }
    public float Lacunarity { get; }
    public float Gain { get; }

    public SimplexNoiseParameters(uint seed, float scale, float strength, uint numOctaves, float frequency, float amplitude, float lacunarity, float gain)
    {
        Seed = seed;
        Scale = scale;
        Strength = strength;
        NumOctaves = numOctaves;
        Frequency = frequency;
        Amplitude = amplitude;
        Lacunarity = lacunarity;
        Gain = gain;
    }
}
