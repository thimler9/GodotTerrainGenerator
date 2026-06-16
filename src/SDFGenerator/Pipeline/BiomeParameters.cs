using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Godot;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Utilities.Math.Extensions;

namespace TerrainGeneration.Application.SDFGenerator.SimplexNoise;

public enum IgnoreBiomeEnum
{
    Ignore = -1,
    Include = 0
}

[StructLayout(LayoutKind.Explicit)]
public struct BiomeParameters
{
    [FieldOffset(0)]
    public readonly float Temperature;

    [FieldOffset(4)]
    public readonly float TemperatureSpread;

    [FieldOffset(8)]
    public readonly float Depth;

    [FieldOffset(12)]
    public readonly float DepthSpread;

    // -1 if we ignore the biome
    [FieldOffset(16)]
    public readonly int IgnoreBiome;

    [FieldOffset(20)]
    private readonly Vector3 Padding;


    public BiomeParameters(float temperature, float temperatureSpread, float depth, float depthSpread, bool ignoreBiome)
    {
        //TODO: Add Validation
        Temperature = temperature;
        TemperatureSpread = temperatureSpread;
        Depth = depth;
        DepthSpread = depthSpread;
        IgnoreBiome = (int)(ignoreBiome ? IgnoreBiomeEnum.Ignore : IgnoreBiomeEnum.Include);
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is BiomeParameters))
        {
            return false;
        }

        BiomeParameters other = (BiomeParameters)obj;

        return
            Temperature.FuzzyEquals(other.Temperature) &&
            TemperatureSpread.FuzzyEquals(other.TemperatureSpread) &&
            Depth.FuzzyEquals(other.TemperatureSpread) &&
            DepthSpread.FuzzyEquals(other.TemperatureSpread);
    }
    public static bool operator ==(BiomeParameters left, BiomeParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BiomeParameters left, BiomeParameters right)
    {
        return !(left == right);
    }
}
