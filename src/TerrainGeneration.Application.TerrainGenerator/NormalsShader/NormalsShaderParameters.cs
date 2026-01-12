using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

namespace TerrainGeneration.Application.TerrainGenerator.NormalsShader;

[StructLayout(LayoutKind.Explicit)]
public struct NormalsShaderParameters
{
    [FieldOffset(0)]
    public uint ChunkSize;
    
    [FieldOffset(4)]
    public uint Lod;

    [FieldOffset(8)]
    readonly Vector2 Padding;

    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is NormalsShaderParameters))
        {
            return false;
        }

        NormalsShaderParameters other = (NormalsShaderParameters)obj;

        return
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
