using Godot;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel.NormalsShader;

namespace TerrainGeneration.Application.TerrainGenerator;


[StructLayout(LayoutKind.Explicit)]
public struct TerrainMeshParameters
{
    [FieldOffset(0)]
    public uint ChunkSize;

    [FieldOffset(4)]
    public float BorderWidth;

    [FieldOffset(8)]
    public int ExpandBorders;

    [FieldOffset(12)]
    public int RetractBorders;

    [FieldOffset(16)]
    public Vector4 ChunkOffset;

    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is TerrainMeshParameters))
        {
            return false;
        }

        TerrainMeshParameters other = (TerrainMeshParameters)obj;

        return
            ChunkSize == other.ChunkSize &&
            BorderWidth == other.BorderWidth;
    }
    public static bool operator ==(TerrainMeshParameters left, TerrainMeshParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TerrainMeshParameters left, TerrainMeshParameters right)
    {
        return !(left == right);
    }
}
