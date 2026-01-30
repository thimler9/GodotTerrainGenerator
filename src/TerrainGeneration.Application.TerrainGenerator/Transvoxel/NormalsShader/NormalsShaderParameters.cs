using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel.NormalsShader;

[StructLayout(LayoutKind.Explicit)]
public struct NormalsShaderParameters
{
    [FieldOffset(0)]
    public Vector4 ChunkOffset;

    [FieldOffset(16)]
    public uint ChunkSize;
    
    [FieldOffset(20)]
    public uint Lod;

    [FieldOffset(24)]
    readonly Vector2 Padding;

    public NormalsShaderParameters(Vector3 chunkOffset, uint chunkSize, uint lod)
    {
        ChunkOffset = new Vector4(chunkOffset.X, chunkOffset.Y, chunkOffset.Z, 0.0f);
        ChunkSize = chunkSize;
        Lod = lod;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is NormalsShaderParameters))
        {
            return false;
        }

        NormalsShaderParameters other = (NormalsShaderParameters)obj;

        return
            ChunkOffset == other.ChunkOffset &&
            ChunkSize == other.ChunkSize &&
            Lod == other.Lod;
    }
    public static bool operator ==(NormalsShaderParameters left, NormalsShaderParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NormalsShaderParameters left, NormalsShaderParameters right)
    {
        return !(left == right);
    }
}
