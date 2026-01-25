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
    public readonly Vector4 ChunkOffset;

    [FieldOffset(16)]
    public readonly uint ChunkSize;

    [FieldOffset(20)]
    public readonly uint Lod;

    [FieldOffset(24)]
    public readonly Vector2 Padding;

    public SDFShaderParameters(Vector3 chunkOffset, uint chunkSize, uint lod)
    {
        // TODO: Add validation
        this.ChunkOffset = new Vector4(chunkOffset.X, chunkOffset.Y, chunkOffset.Z, 1.0f);
        this.ChunkSize = chunkSize;
        this.Lod = lod;
    }

    public byte[] ToByteArray()
    {
        int size = Marshal.SizeOf<SDFShaderParameters>();
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
        if (obj == null || !(obj is SDFShaderParameters))
        {
            return false;
        }

        SDFShaderParameters other = (SDFShaderParameters)obj;

        return ChunkSize == other.ChunkSize && Lod == other.Lod && ChunkOffset == other.ChunkOffset;
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
