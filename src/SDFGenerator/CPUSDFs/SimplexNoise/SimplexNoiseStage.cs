using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.CPU.SimplexNoise;

public sealed class SimplexNoiseStage : ISDFPipelineStageCPU
{
    public const string FunctionName = "SimplexNoise";

    public string Function => FunctionName;

    public uint Seed { get; set; }
    public float Scale { get; set; }
    public float Strength { get; set; }
    public uint NumOctaves { get; set; }
    public float Frequency { get; set; }
    public float Amplitude { get; set; }
    public float Lacunarity { get; set; }
    public float Gain { get; set; }

    public ICpuSdfParameters CreateParameters()
    {
        return new SimplexNoiseParameters(Seed, Scale, Strength, NumOctaves, Frequency, Amplitude, Lacunarity, Gain);
    }
}
