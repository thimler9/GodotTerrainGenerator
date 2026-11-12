using System;
using Godot;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline;

public readonly struct CPUSDFParameters
{
    public Vector3 ChunkOffset { get; }
    public uint ChunkSize { get; }
    public uint Lod { get; }

    public CPUSDFParameters(Vector3 chunkOffset, uint chunkSize, uint lod)
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
