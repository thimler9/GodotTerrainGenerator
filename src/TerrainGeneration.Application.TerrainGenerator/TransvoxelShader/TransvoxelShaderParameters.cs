using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.TerrainGenerator.NormalsShader;

namespace TerrainGeneration.Application.TerrainGenerator.TransvoxelShader;

[StructLayout(LayoutKind.Explicit)]
public struct TransvoxelShaderParameters
{
    [FieldOffset(0)]
    public uint ChunkSize;

    [FieldOffset(8)]
    public Vector3 ChunkOffset;

    [FieldOffset(4)]
    public uint Lod;

    [FieldOffset(20)]
    public float TransitionWidth;

    [FieldOffset(24)]
    public uint MaxNumVertices;

    [FieldOffset(28)]
    public uint Padding;

    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is TransvoxelShaderParameters))
        {
            return false;
        }

        TransvoxelShaderParameters other = (TransvoxelShaderParameters)obj;

        return
            ChunkSize == other.ChunkSize &&
            Lod == other.Lod;
    }
    public static bool operator ==(TransvoxelShaderParameters left, TransvoxelShaderParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TransvoxelShaderParameters left, TransvoxelShaderParameters right)
    {
        return !(left == right);
    }
}
