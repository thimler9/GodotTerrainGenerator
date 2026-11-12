namespace TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;

public readonly struct BiomeParametersCPU
{
    public float Temperature { get; }
    public float TemperatureSpread { get; }
    public float Depth { get; }
    public float DepthSpread { get; }
    public bool IgnoreBiome { get; }

    public BiomeParametersCPU(float temperature, float temperatureSpread, float depth, float depthSpread, bool ignoreBiome)
    {
        Temperature = temperature;
        TemperatureSpread = temperatureSpread;
        Depth = depth;
        DepthSpread = depthSpread;
        IgnoreBiome = ignoreBiome;
    }
}
