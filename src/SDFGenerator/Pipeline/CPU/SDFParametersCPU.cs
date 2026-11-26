using System;
using Godot;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;

public readonly struct SDFParametersCPU
{
    public Vector3 ChunkOffset { get; }
    public uint ChunkSize { get; }
    public uint Lod { get; }

    public SDFParametersCPU(Vector3 chunkOffset, uint chunkSize, uint lod)
    {
        if (lod == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lod), "LOD must be greater than zero.");
        }

        ChunkOffset = chunkOffset;
        ChunkSize = chunkSize;
        Lod = lod;
    }
}
