using Godot;
using System;
using System.Collections.Generic;
using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.TerrainGenerator.TerrainSpawns;

namespace GodotTerrainGenerator2.TerrainSpawns;

public sealed class GodotTerrainSpawnFactory : ITerrainSpawnFactory, IDisposable
{
    private readonly RenderingDevice _rd;
    private readonly Node _parent;
    private readonly IReadOnlyList<TerrainSpawnDefinition> _definitions;
    private readonly Settings _settings;

    private PoissonDiscQuadtree2D _quadtree;
    private TerrainSpawnGpuPipeline _pipeline;

    public GodotTerrainSpawnFactory(RenderingDevice rd, Node parent, IReadOnlyList<TerrainSpawnDefinition> definitions, Settings settings)
    {
        _rd = rd;
        _parent = parent;
        _definitions = definitions;
        _settings = settings;
        _quadtree = new PoissonDiscQuadtree2D(_rd, settings.WorldSize, settings.Seed, settings.MaxQuadtreeLevel, settings.BasePdsRadius, settings.RadiusFalloff);
        _pipeline = new TerrainSpawnGpuPipeline(_rd, settings.MaxCandidates, settings.MaxSelections);
    }

    public ITerrainSpawns CreateTerrainSpawns(TerrainChunkDescriptor descriptor)
    {
        if (_definitions.Count == 0)
        {
            return null;
        }

        PoissonDiscQuadtreeNode node = _quadtree.GetNodeForChunk(new Vector2(descriptor.ChunkOffset.X, descriptor.ChunkOffset.Z), descriptor.ChunkSize, (int)descriptor.Lod);
        TerrainSpawnSelection[] selections = _pipeline.Generate(new TerrainSpawnGenerationRequest
        {
            PointBuffer = node.GpuPointBuffer,
            PointCount = (uint)node.Points.Count,
            ChunkOffset = descriptor.ChunkOffset,
            ChunkSize = descriptor.ChunkSize,
            Lod = descriptor.Lod,
            Seed = _settings.Seed,
            MaxHitsPerRay = _settings.MaxHitsPerRay,
            TopY = _settings.TopY,
            BottomY = _settings.BottomY,
            StepSize = _settings.StepSize,
            RefineSteps = _settings.RefineSteps,
            SeaLevel = _settings.SeaLevel,
            SunLight = _settings.SunLight,
            NoiseSeed = _settings.NoiseSeed,
            NoiseScale = _settings.NoiseScale,
            NoiseStrength = _settings.NoiseStrength,
            NoiseOctaves = _settings.NoiseOctaves,
            NoiseFrequency = _settings.NoiseFrequency,
            NoiseAmplitude = _settings.NoiseAmplitude,
            NoiseLacunarity = _settings.NoiseLacunarity,
            NoiseGain = _settings.NoiseGain,
            SpawnDefinitions = _definitions,
        });

        TerrainSpawnRenderer renderer = new TerrainSpawnRenderer
        {
            Name = $"TerrainSpawns_{descriptor.ChunkOffset.X}_{descriptor.ChunkOffset.Y}_{descriptor.ChunkOffset.Z}_{descriptor.Lod}",
        };
        _parent.AddChild(renderer);
        renderer.RenderSelections(_definitions, selections);
        return new GodotTerrainSpawns(renderer);
    }

    public void Dispose()
    {
        _pipeline?.Dispose();
        _pipeline = null;
        _quadtree?.Dispose();
        _quadtree = null;
    }

    public sealed class Settings
    {
        public uint Seed { get; init; } = 1234;
        public float WorldSize { get; init; } = 4096.0f;
        public int MaxQuadtreeLevel { get; init; } = 4;
        public float BasePdsRadius { get; init; } = 64.0f;
        public float RadiusFalloff { get; init; } = 0.5f;
        public float TopY { get; init; } = 512.0f;
        public float BottomY { get; init; } = -512.0f;
        public float StepSize { get; init; } = 2.0f;
        public uint RefineSteps { get; init; } = 6;
        public uint MaxHitsPerRay { get; init; } = 4;
        public float SeaLevel { get; init; } = 0.0f;
        public float SunLight { get; init; } = 1.0f;
        public uint MaxCandidates { get; init; } = 65536;
        public uint MaxSelections { get; init; } = 65536;
        public uint NoiseSeed { get; init; } = 1234;
        public float NoiseScale { get; init; } = 32.0f;
        public float NoiseStrength { get; init; } = 350.0f;
        public uint NoiseOctaves { get; init; } = 8;
        public float NoiseFrequency { get; init; } = 1.0f;
        public float NoiseAmplitude { get; init; } = 1.0f;
        public float NoiseLacunarity { get; init; } = 2.0f;
        public float NoiseGain { get; init; } = 0.4f;
    }

    private sealed class GodotTerrainSpawns : ITerrainSpawns
    {
        private TerrainSpawnRenderer _renderer;

        public GodotTerrainSpawns(TerrainSpawnRenderer renderer)
        {
            _renderer = renderer;
        }

        public void Render()
        {
            if (GodotObject.IsInstanceValid(_renderer))
            {
                _renderer.Visible = true;
            }
        }

        public void Dispose()
        {
            if (GodotObject.IsInstanceValid(_renderer))
            {
                _renderer.Clear();
                _renderer.QueueFree();
            }

            _renderer = null;
        }
    }
}
