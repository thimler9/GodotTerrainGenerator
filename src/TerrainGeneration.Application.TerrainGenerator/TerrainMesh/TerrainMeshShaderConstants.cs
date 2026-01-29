using Godot;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel.NormalsShader;

namespace TerrainGeneration.Application.TerrainGenerator;


[StructLayout(LayoutKind.Explicit)]
public struct TerrainMeshShaderConstants
{
    [FieldOffset(0)]
    public float BorderWidth;

    [FieldOffset(4)]
    private Vector3 Padding;

    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is TerrainMeshShaderConstants))
        {
            return false;
        }

        TerrainMeshShaderConstants other = (TerrainMeshShaderConstants)obj;

        return
            BorderWidth == other.BorderWidth;
    }
    public static bool operator ==(TerrainMeshShaderConstants left, TerrainMeshShaderConstants right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TerrainMeshShaderConstants left, TerrainMeshShaderConstants right)
    {
        return !(left == right);
    }
}
