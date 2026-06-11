using Godot;
using Godot.Collections;
using System.Collections.Generic;

namespace GodotTerrainGenerator2.TerrainSpawns;

[Tool]
[GlobalClass]
public partial class TerrainSpawnChunk : Node3D
{
    private static readonly System.Collections.Generic.Dictionary<QuadtreeCacheKey, CachedQuadtree> QuadtreeCache = [];

    [ExportGroup("Chunk")]
    [Export] public uint ChunkSize { get; set; } = 32;
    [Export] public uint Lod { get; set; } = 1;
    [Export] public Vector3 ChunkOffset { get; set; } = Vector3.Zero;

    [ExportGroup("Sampling")]
    [Export] public uint Seed { get; set; } = 1234;
    [Export] public float WorldSize { get; set; } = 4096.0f;
    [Export] public int MaxQuadtreeLevel { get; set; } = 4;
    [Export] public float BasePdsRadius { get; set; } = 64.0f;
    [Export] public float RadiusFalloff { get; set; } = 0.5f;

    [ExportGroup("Ray March")]
    [Export] public float TopY { get; set; } = 512.0f;
    [Export] public float BottomY { get; set; } = -512.0f;
    [Export] public float StepSize { get; set; } = 2.0f;
    [Export] public uint RefineSteps { get; set; } = 6;
    [Export] public uint MaxHitsPerRay { get; set; } = 4;
    [Export] public float SeaLevel { get; set; } = 0.0f;
    [Export] public float SunLight { get; set; } = 1.0f;
    [Export] public uint MaxCandidates { get; set; } = 65536;
    [Export] public uint MaxSelections { get; set; } = 65536;

    [ExportGroup("Noise")]
    [Export] public uint NoiseSeed { get; set; } = 1234;
    [Export] public float NoiseScale { get; set; } = 32.0f;
    [Export] public float NoiseStrength { get; set; } = 350.0f;
    [Export] public uint NoiseOctaves { get; set; } = 8;
    [Export] public float NoiseFrequency { get; set; } = 1.0f;
    [Export] public float NoiseAmplitude { get; set; } = 1.0f;
    [Export] public float NoiseLacunarity { get; set; } = 2.0f;
    [Export] public float NoiseGain { get; set; } = 0.4f;

    [ExportGroup("Spawn Types")]
    [Export] public Array<TerrainSpawnDefinition> SpawnDefinitions { get; set; } = [];

    private RenderingDevice _rd;
    private PoissonDiscQuadtree2D _quadtree;
    private QuadtreeCacheKey _quadtreeCacheKey;
    private bool _hasQuadtreeCacheKey;
    private TerrainSpawnGpuPipeline _pipeline;
    private TerrainSpawnRenderer _renderer;

    public override void _Ready()
    {
        Generate();
    }

    public override void _ExitTree()
    {
        _pipeline?.Dispose();
        _pipeline = null;
        ReleaseQuadtree();
        _quadtree = null;
    }

    [ExportToolButton("Regenerate Spawns")]
    public Callable RegenerateButton => Callable.From(Generate);

    public void Generate()
    {
        if (SpawnDefinitions.Count == 0)
        {
            GD.PushWarning("TerrainSpawnChunk has no spawn definitions.");
            return;
        }

        _rd ??= RenderingServer.GetRenderingDevice();
        if (_rd == null)
        {
            GD.PushError("Terrain spawns require the main RenderingDevice.");
            return;
        }

        AcquireQuadtree();
        _pipeline ??= new TerrainSpawnGpuPipeline(_rd, MaxCandidates, MaxSelections);
        _renderer ??= EnsureRenderer();

        PoissonDiscQuadtreeNode node = _quadtree.GetNodeForChunk(new Vector2(ChunkOffset.X, ChunkOffset.Z), ChunkSize, (int)Lod);
        IReadOnlyList<TerrainSpawnDefinition> definitions = BuildDefinitionList();
        TerrainSpawnSelection[] selections = _pipeline.Generate(new TerrainSpawnGenerationRequest
        {
            PointBuffer = node.GpuPointBuffer,
            PointCount = (uint)node.Points.Count,
            ChunkOffset = ChunkOffset,
            ChunkSize = ChunkSize,
            Lod = Lod,
            Seed = Seed,
            MaxHitsPerRay = MaxHitsPerRay,
            TopY = TopY,
            BottomY = BottomY,
            StepSize = StepSize,
            RefineSteps = RefineSteps,
            SeaLevel = SeaLevel,
            SunLight = SunLight,
            NoiseSeed = NoiseSeed,
            NoiseScale = NoiseScale,
            NoiseStrength = NoiseStrength,
            NoiseOctaves = NoiseOctaves,
            NoiseFrequency = NoiseFrequency,
            NoiseAmplitude = NoiseAmplitude,
            NoiseLacunarity = NoiseLacunarity,
            NoiseGain = NoiseGain,
            SpawnDefinitions = definitions,
        });

        _renderer.RenderSelections(definitions, selections);
        GD.Print($"Generated {selections.Length} terrain spawn selections from {node.Points.Count} PDS points.");
    }

    private TerrainSpawnRenderer EnsureRenderer()
    {
        TerrainSpawnRenderer renderer = GetNodeOrNull<TerrainSpawnRenderer>("TerrainSpawnRenderer");
        if (renderer != null)
        {
            return renderer;
        }

        renderer = new TerrainSpawnRenderer
        {
            Name = "TerrainSpawnRenderer",
        };
        AddChild(renderer);
        return renderer;
    }

    private IReadOnlyList<TerrainSpawnDefinition> BuildDefinitionList()
    {
        List<TerrainSpawnDefinition> definitions = [];
        foreach (TerrainSpawnDefinition definition in SpawnDefinitions)
        {
            if (definition != null)
            {
                definitions.Add(definition);
            }
        }

        return definitions;
    }

    private void AcquireQuadtree()
    {
        QuadtreeCacheKey key = new QuadtreeCacheKey(WorldSize, Seed, MaxQuadtreeLevel, BasePdsRadius, RadiusFalloff);
        if (_hasQuadtreeCacheKey && _quadtreeCacheKey.Equals(key) && _quadtree != null)
        {
            return;
        }

        ReleaseQuadtree();
        if (!QuadtreeCache.TryGetValue(key, out CachedQuadtree cached))
        {
            cached = new CachedQuadtree(new PoissonDiscQuadtree2D(_rd, WorldSize, Seed, MaxQuadtreeLevel, BasePdsRadius, RadiusFalloff));
            QuadtreeCache[key] = cached;
        }

        cached.RefCount++;
        _quadtree = cached.Quadtree;
        _quadtreeCacheKey = key;
        _hasQuadtreeCacheKey = true;
    }

    private void ReleaseQuadtree()
    {
        if (!_hasQuadtreeCacheKey)
        {
            return;
        }

        if (QuadtreeCache.TryGetValue(_quadtreeCacheKey, out CachedQuadtree cached))
        {
            cached.RefCount--;
            if (cached.RefCount <= 0)
            {
                cached.Quadtree.Dispose();
                QuadtreeCache.Remove(_quadtreeCacheKey);
            }
        }

        _hasQuadtreeCacheKey = false;
    }

    private readonly record struct QuadtreeCacheKey(float WorldSize, uint Seed, int MaxLevel, float BaseRadius, float RadiusFalloff);

    private sealed class CachedQuadtree
    {
        public CachedQuadtree(PoissonDiscQuadtree2D quadtree)
        {
            Quadtree = quadtree;
        }

        public PoissonDiscQuadtree2D Quadtree { get; }
        public int RefCount { get; set; }
    }
}
