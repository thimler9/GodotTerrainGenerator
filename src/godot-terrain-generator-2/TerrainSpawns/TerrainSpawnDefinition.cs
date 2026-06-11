using Godot;

namespace GodotTerrainGenerator2.TerrainSpawns;

[GlobalClass]
public partial class TerrainSpawnDefinition : Resource
{
    [Export] public string SpawnId { get; set; } = "spawn";
    [Export] public Mesh Mesh { get; set; }
    [Export] public Material MaterialOverride { get; set; }

    [ExportGroup("Environment")]
    [Export] public float PreferredTemperature { get; set; } = 0.5f;
    [Export] public float TemperatureTolerance { get; set; } = 0.5f;
    [Export] public float PreferredUnderwaterDepth { get; set; } = 0.0f;
    [Export] public float UnderwaterDepthTolerance { get; set; } = 0.25f;
    [Export] public float PreferredLight { get; set; } = 1.0f;
    [Export] public float LightTolerance { get; set; } = 0.5f;
    [Export] public float MaxSlopeDegrees { get; set; } = 50.0f;
    [Export] public float BaseWeight { get; set; } = 1.0f;
    [Export] public Vector2 ScaleRange { get; set; } = new Vector2(0.85f, 1.25f);

    public TerrainSpawnGpuDefinition ToGpuDefinition(int index)
    {
        return new TerrainSpawnGpuDefinition
        {
            PreferredTemperature = PreferredTemperature,
            TemperatureTolerance = Mathf.Max(0.001f, TemperatureTolerance),
            PreferredUnderwaterDepth = PreferredUnderwaterDepth,
            UnderwaterDepthTolerance = Mathf.Max(0.001f, UnderwaterDepthTolerance),
            PreferredLight = PreferredLight,
            LightTolerance = Mathf.Max(0.001f, LightTolerance),
            MaxSlopeCosine = Mathf.Cos(Mathf.DegToRad(MaxSlopeDegrees)),
            BaseWeight = Mathf.Max(0.0f, BaseWeight),
            MinScale = ScaleRange.X,
            MaxScale = ScaleRange.Y,
            TypeIndex = (uint)index,
            Padding = 0,
        };
    }
}

public struct TerrainSpawnGpuDefinition
{
    public float PreferredTemperature;
    public float TemperatureTolerance;
    public float PreferredUnderwaterDepth;
    public float UnderwaterDepthTolerance;
    public float PreferredLight;
    public float LightTolerance;
    public float MaxSlopeCosine;
    public float BaseWeight;
    public float MinScale;
    public float MaxScale;
    public uint TypeIndex;
    public uint Padding;
}

public struct TerrainSpawnCandidate
{
    public Vector4 Position;
    public Vector4 NormalAndDepth;
    public Vector4 EnvironmentAndRandom;
}

public struct TerrainSpawnSelection
{
    public Vector4 PositionAndScale;
    public Vector4 NormalAndType;
}
