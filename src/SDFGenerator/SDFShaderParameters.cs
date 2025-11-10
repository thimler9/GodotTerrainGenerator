using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

namespace TerrainGeneration.Application.SDFGenerator;

[StructLayout(LayoutKind.Explicit)]
public struct SDFShaderParameters
{
    [FieldOffset(0)]
    public readonly uint ChunkSize;

    [FieldOffset(4)]
    public readonly uint Lod;

    [FieldOffset(8)]
    public readonly Vector2 Padding;

    public SDFShaderParameters(uint ChunkSize, uint Lod)
    {
        // TODO: Add validation

        this.ChunkSize = ChunkSize;
        this.Lod = Lod;
    }

    public byte[] ToByteArray()
    {
        int size = Marshal.SizeOf<SimplexNoiseShaderParameters>();
        byte[] arr = new byte[size];

        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return arr;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is SimplexNoiseShaderParameters))
        {
            return false;
        }

        SDFShaderParameters other = (SDFShaderParameters)obj;

        return ChunkSize == other.ChunkSize && Lod == other.Lod;
    }
    public static bool operator ==(SDFShaderParameters left, SDFShaderParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SDFShaderParameters left, SDFShaderParameters right)
    {
        return !(left == right);
    }
}
