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
    public readonly Vector3 Offset;
    [FieldOffset(12)]
    public readonly uint Seed;

    [FieldOffset(16)]
    public readonly float Scale;

    [FieldOffset(20)]
    public readonly float Strength;

    [FieldOffset(24)]
    public readonly uint NumOctaves;

    [FieldOffset(28)]
    public readonly float Frequency;

    [FieldOffset(32)]
    public readonly float Amplitude;

    [FieldOffset(36)]
    public readonly float Lacunarity;

    [FieldOffset(40)]
    public readonly float Gain;

    [FieldOffset(44)]
    public readonly uint Padding;

    public SimplexNoiseShaderParameters(Vector3 offset, uint seed, float scale, float strength, uint numOctaves, float frequency, float amplitude, float lacunarity, float gain)
    {
        //TODO: Add Validation

        Offset = offset;
        Seed = seed;
        Scale = scale;
        Strength = strength;
        NumOctaves = numOctaves;
        Frequency = frequency;
        Amplitude = amplitude;
        Lacunarity = lacunarity;
        Gain = gain;
    }

    public byte[] ToByteArray()
    {
        int size = Marshal.SizeOf<SimplexNoiseShaderParameters>();
        byte[] arr = new byte[size];

        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return arr;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is SimplexNoiseShaderParameters))
        {
            return false;
        }

        SimplexNoiseShaderParameters other = (SimplexNoiseShaderParameters)obj;

        return 
            Offset.FuzzyEquals(other.Offset) &&
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
