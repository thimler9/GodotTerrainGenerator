using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Godot;
using TerrainGeneration.Utilities.Math.Extensions;

namespace TerrainGeneration.Application.SDFGenerator.SimplexNoise;

[StructLayout(LayoutKind.Explicit)]
public struct SimplexNoiseShaderParameters
{
    [FieldOffset(0)]
    public readonly uint Seed;

    [FieldOffset(4)]
    public readonly float Scale;

    [FieldOffset(8)]
    public readonly float Strength;

    [FieldOffset(12)]
    public readonly uint NumOctaves;

    [FieldOffset(16)]
    public readonly float Frequency;

    [FieldOffset(20)]
    public readonly float Amplitude;

    [FieldOffset(24)]
    public readonly float Lacunarity;

    [FieldOffset(28)]
    public readonly float Gain;

    public SimplexNoiseShaderParameters(uint seed, float scale, float strength, uint numOctaves, float frequency, float amplitude, float lacunarity, float gain)
    {
        //TODO: Add Validation
        Seed = seed;
        Scale = scale;
        Strength = strength;
        NumOctaves = numOctaves;
        Frequency = frequency;
        Amplitude = amplitude;
        Lacunarity = lacunarity;
        Gain = gain;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is SimplexNoiseShaderParameters))
        {
            return false;
        }

        SimplexNoiseShaderParameters other = (SimplexNoiseShaderParameters)obj;

        return 
            Seed == other.Seed &&
            Scale.FuzzyEquals(other.Scale) &&
            Strength.FuzzyEquals(other.Strength) &&
            NumOctaves == other.NumOctaves &&
            Frequency.FuzzyEquals(other.Frequency) &&
            Amplitude.FuzzyEquals(other.Amplitude) &&
            Lacunarity.FuzzyEquals(other.Lacunarity) &&
            Gain.FuzzyEquals(other.Gain);
    }
    public static bool operator ==(SimplexNoiseShaderParameters left, SimplexNoiseShaderParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SimplexNoiseShaderParameters left, SimplexNoiseShaderParameters right)
    {
        return !(left == right);
    }
}
