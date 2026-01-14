using Godot;
using System.Runtime.InteropServices;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel;

[StructLayout(LayoutKind.Explicit)]
public struct TransvoxelShaderParameters
{
    [FieldOffset(0)]
    public uint ChunkSize;

    [FieldOffset(4)]
    public uint Lod;

    [FieldOffset(8)]
    public float TransitionWidth;

    [FieldOffset(12)]
    public uint MaxNumVertices;

    [FieldOffset(16)]
    public Vector3 ChunkOffset;

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
