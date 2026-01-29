using Godot;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel.NormalsShader;

namespace TerrainGeneration.Application.TerrainGenerator;


[StructLayout(LayoutKind.Explicit)]
public struct TerrainMeshShaderParameters
{
    [FieldOffset(0)]
    public Vector4 ChunkOffset;

    [FieldOffset(16)]
    public uint ChunkSize;

    [FieldOffset(20)]
    public int ExpandBorders;

    [FieldOffset(24)]
    public int RetractBorders;

    [FieldOffset(28)]
    private uint Padding;
    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is TerrainMeshShaderParameters))
        {
            return false;
        }

        TerrainMeshShaderParameters other = (TerrainMeshShaderParameters)obj;

        return
            ChunkSize == other.ChunkSize &&
            ExpandBorders == other.ExpandBorders &&
            RetractBorders == other.RetractBorders &&
            ChunkOffset == other.ChunkOffset;
    }
    public static bool operator ==(TerrainMeshShaderParameters left, TerrainMeshShaderParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TerrainMeshShaderParameters left, TerrainMeshShaderParameters right)
    {
        return !(left == right);
    }
}
