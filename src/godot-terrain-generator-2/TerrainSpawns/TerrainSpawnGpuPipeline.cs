using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GodotTerrainGenerator2.TerrainSpawns;

public sealed class TerrainSpawnGpuPipeline : IDisposable
{
    private readonly RenderingDevice _rd;
    private readonly Rid _rayMarchShader;
    private readonly Rid _rayMarchPipeline;
    private readonly Rid _selectShader;
    private readonly Rid _selectPipeline;

    private Rid _candidateBuffer;
    private Rid _candidateCounterBuffer;
    private Rid _selectionBuffer;
    private Rid _selectionCounterBuffer;
    private Rid _definitionBuffer;
    private Rid _rayParamsBuffer;
    private Rid _selectParamsBuffer;

    public uint MaxCandidates { get; }
    public uint MaxSelections { get; }

    public TerrainSpawnGpuPipeline(RenderingDevice rd, uint maxCandidates, uint maxSelections)
    {
        _rd = rd;
        MaxCandidates = Math.Max(1u, maxCandidates);
        MaxSelections = Math.Max(1u, maxSelections);

        _rayMarchShader = CreateShader("res://Shaders/Compute/terrain_spawn_ray_march.glsl");
        _rayMarchPipeline = _rd.ComputePipelineCreate(_rayMarchShader);
        _selectShader = CreateShader("res://Shaders/Compute/terrain_spawn_select.glsl");
        _selectPipeline = _rd.ComputePipelineCreate(_selectShader);

        _candidateBuffer = _rd.StorageBufferCreate(MaxCandidates * 48u);
        _candidateCounterBuffer = CreateCounterBuffer();
        _selectionBuffer = _rd.StorageBufferCreate(MaxSelections * 32u);
        _selectionCounterBuffer = CreateCounterBuffer();
    }

    public TerrainSpawnSelection[] Generate(TerrainSpawnGenerationRequest request)
    {
        if (!request.PointBuffer.IsValid || request.PointCount == 0 || request.SpawnDefinitions.Count == 0)
        {
            return [];
        }

        DispatchRayMarch(request);
        DispatchSelect(request);

        byte[] counterBytes = _rd.BufferGetData(_selectionCounterBuffer);
        uint[] counter = new uint[1];
        Buffer.BlockCopy(counterBytes, 0, counter, 0, sizeof(uint));

        int count = (int)Math.Min(counter[0], MaxSelections);
        if (count == 0)
        {
            return [];
        }

        byte[] selectionBytes = _rd.BufferGetData(_selectionBuffer, 0, (uint)(count * 32));
        TerrainSpawnSelection[] selections = new TerrainSpawnSelection[count];
        MemoryMarshal.Cast<byte, TerrainSpawnSelection>(selectionBytes).CopyTo(selections);
        return selections;
    }

    public void Dispose()
    {
        Free(_definitionBuffer);
        Free(_rayParamsBuffer);
        Free(_selectParamsBuffer);
        Free(_candidateBuffer);
        Free(_candidateCounterBuffer);
        Free(_selectionBuffer);
        Free(_selectionCounterBuffer);
        Free(_rayMarchShader);
        Free(_selectShader);
    }

    private void DispatchRayMarch(TerrainSpawnGenerationRequest request)
    {
        ResetCounter(_candidateCounterBuffer);

        RayMarchParams rayParams = new RayMarchParams
        {
            ChunkOffset = new Vector4(request.ChunkOffset.X, request.ChunkOffset.Y, request.ChunkOffset.Z, 0.0f),
            ChunkSize = request.ChunkSize,
            Lod = request.Lod,
            PointCount = request.PointCount,
            MaxCandidates = MaxCandidates,
            MaxHitsPerRay = request.MaxHitsPerRay,
            TopY = request.TopY,
            BottomY = request.BottomY,
            StepSize = request.StepSize,
            RefineSteps = request.RefineSteps,
            SeaLevel = request.SeaLevel,
            SunLight = request.SunLight,
            Seed = request.Seed,
            NoiseSeed = request.NoiseSeed,
            NoiseScale = request.NoiseScale,
            NoiseStrength = request.NoiseStrength,
            NoiseOctaves = request.NoiseOctaves,
            NoiseFrequency = request.NoiseFrequency,
            NoiseAmplitude = request.NoiseAmplitude,
            NoiseLacunarity = request.NoiseLacunarity,
            NoiseGain = request.NoiseGain,
        };
        ReplaceUniformBuffer(ref _rayParamsBuffer, rayParams);

        Rid pointSet = CreateStorageUniformSet(_rayMarchShader, 0, 0, request.PointBuffer);
        Rid candidateSet = CreateStorageUniformSet(_rayMarchShader, 1, 0, _candidateBuffer);
        Rid counterSet = CreateStorageUniformSet(_rayMarchShader, 2, 0, _candidateCounterBuffer);
        Rid paramsSet = CreateUniformBufferSet(_rayMarchShader, 3, 0, _rayParamsBuffer);

        long list = _rd.ComputeListBegin();
        _rd.ComputeListBindComputePipeline(list, _rayMarchPipeline);
        _rd.ComputeListBindUniformSet(list, pointSet, 0);
        _rd.ComputeListBindUniformSet(list, candidateSet, 1);
        _rd.ComputeListBindUniformSet(list, counterSet, 2);
        _rd.ComputeListBindUniformSet(list, paramsSet, 3);
        _rd.ComputeListDispatch(list, (uint)Mathf.CeilToInt(request.PointCount / 64.0f), 1, 1);
        _rd.ComputeListEnd();
        _rd.Submit();
        _rd.Sync();

        Free(pointSet);
        Free(candidateSet);
        Free(counterSet);
        Free(paramsSet);
    }

    private void DispatchSelect(TerrainSpawnGenerationRequest request)
    {
        ResetCounter(_selectionCounterBuffer);
        ReplaceDefinitionBuffer(request.SpawnDefinitions);

        SelectParams selectParams = new SelectParams
        {
            MaxCandidates = MaxCandidates,
            DefinitionCount = (uint)request.SpawnDefinitions.Count,
            MaxSelections = MaxSelections,
            Seed = request.Seed,
        };
        ReplaceUniformBuffer(ref _selectParamsBuffer, selectParams);

        Rid candidateSet = CreateStorageUniformSet(_selectShader, 0, 0, _candidateBuffer);
        Rid candidateCounterSet = CreateStorageUniformSet(_selectShader, 1, 0, _candidateCounterBuffer);
        Rid definitionSet = CreateStorageUniformSet(_selectShader, 2, 0, _definitionBuffer);
        Rid selectionSet = CreateStorageUniformSet(_selectShader, 3, 0, _selectionBuffer);
        Rid selectionCounterSet = CreateStorageUniformSet(_selectShader, 4, 0, _selectionCounterBuffer);
        Rid paramsSet = CreateUniformBufferSet(_selectShader, 5, 0, _selectParamsBuffer);

        long list = _rd.ComputeListBegin();
        _rd.ComputeListBindComputePipeline(list, _selectPipeline);
        _rd.ComputeListBindUniformSet(list, candidateSet, 0);
        _rd.ComputeListBindUniformSet(list, candidateCounterSet, 1);
        _rd.ComputeListBindUniformSet(list, definitionSet, 2);
        _rd.ComputeListBindUniformSet(list, selectionSet, 3);
        _rd.ComputeListBindUniformSet(list, selectionCounterSet, 4);
        _rd.ComputeListBindUniformSet(list, paramsSet, 5);
        _rd.ComputeListDispatch(list, (uint)Mathf.CeilToInt(MaxCandidates / 64.0f), 1, 1);
        _rd.ComputeListEnd();
        _rd.Submit();
        _rd.Sync();

        Free(candidateSet);
        Free(candidateCounterSet);
        Free(definitionSet);
        Free(selectionSet);
        Free(selectionCounterSet);
        Free(paramsSet);
    }

    private void ReplaceDefinitionBuffer(IReadOnlyList<TerrainSpawnDefinition> definitions)
    {
        Free(_definitionBuffer);

        TerrainSpawnGpuDefinition[] packed = new TerrainSpawnGpuDefinition[definitions.Count];
        for (int i = 0; i < definitions.Count; i++)
        {
            packed[i] = definitions[i].ToGpuDefinition(i);
        }

        byte[] bytes = MemoryMarshal.AsBytes<TerrainSpawnGpuDefinition>(packed).ToArray();
        _definitionBuffer = _rd.StorageBufferCreate((uint)bytes.Length, bytes);
    }

    private void ReplaceUniformBuffer<T>(ref Rid buffer, T value) where T : struct
    {
        Free(buffer);
        T[] values = [value];
        byte[] bytes = MemoryMarshal.AsBytes<T>(values).ToArray();
        buffer = _rd.UniformBufferCreate((uint)bytes.Length, bytes);
    }

    private Rid CreateShader(string path)
    {
        RDShaderFile shaderFile = GD.Load<RDShaderFile>(path);
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        return _rd.ShaderCreateFromSpirV(shaderBytecode);
    }

    private Rid CreateCounterBuffer()
    {
        byte[] bytes = new byte[sizeof(uint)];
        return _rd.StorageBufferCreate((uint)bytes.Length, bytes);
    }

    private void ResetCounter(Rid counterBuffer)
    {
        byte[] bytes = new byte[sizeof(uint)];
        _rd.BufferUpdate(counterBuffer, 0, (uint)bytes.Length, bytes);
    }

    private Rid CreateStorageUniformSet(Rid shader, uint set, int binding, Rid buffer)
    {
        RDUniform uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = binding,
        };
        uniform.AddId(buffer);
        return _rd.UniformSetCreate([uniform], shader, set);
    }

    private Rid CreateUniformBufferSet(Rid shader, uint set, int binding, Rid buffer)
    {
        RDUniform uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = binding,
        };
        uniform.AddId(buffer);
        return _rd.UniformSetCreate([uniform], shader, set);
    }

    private void Free(Rid rid)
    {
        if (rid.IsValid)
        {
            _rd.FreeRid(rid);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RayMarchParams
    {
        public Vector4 ChunkOffset;
        public uint ChunkSize;
        public uint Lod;
        public uint PointCount;
        public uint MaxCandidates;
        public uint MaxHitsPerRay;
        public float TopY;
        public float BottomY;
        public float StepSize;
        public uint RefineSteps;
        public float SeaLevel;
        public float SunLight;
        public uint Seed;
        public uint NoiseSeed;
        public float NoiseScale;
        public float NoiseStrength;
        public uint NoiseOctaves;
        public float NoiseFrequency;
        public float NoiseAmplitude;
        public float NoiseLacunarity;
        public float NoiseGain;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SelectParams
    {
        public uint MaxCandidates;
        public uint DefinitionCount;
        public uint MaxSelections;
        public uint Seed;
    }
}

public sealed class TerrainSpawnGenerationRequest
{
    public Rid PointBuffer { get; init; }
    public uint PointCount { get; init; }
    public Vector3 ChunkOffset { get; init; }
    public uint ChunkSize { get; init; }
    public uint Lod { get; init; } = 1;
    public uint Seed { get; init; } = 1;
    public uint MaxHitsPerRay { get; init; } = 4;
    public float TopY { get; init; } = 512.0f;
    public float BottomY { get; init; } = -512.0f;
    public float StepSize { get; init; } = 2.0f;
    public uint RefineSteps { get; init; } = 6;
    public float SeaLevel { get; init; } = 0.0f;
    public float SunLight { get; init; } = 1.0f;
    public uint NoiseSeed { get; init; } = 1;
    public float NoiseScale { get; init; } = 32.0f;
    public float NoiseStrength { get; init; } = 350.0f;
    public uint NoiseOctaves { get; init; } = 8;
    public float NoiseFrequency { get; init; } = 1.0f;
    public float NoiseAmplitude { get; init; } = 1.0f;
    public float NoiseLacunarity { get; init; } = 2.0f;
    public float NoiseGain { get; init; } = 0.4f;
    public IReadOnlyList<TerrainSpawnDefinition> SpawnDefinitions { get; init; } = [];
}
