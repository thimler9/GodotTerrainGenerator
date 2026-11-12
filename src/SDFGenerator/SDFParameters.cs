using Godot;
using System.Runtime.InteropServices;

namespace TerrainGeneration.Application.SDFGenerator;

[StructLayout(LayoutKind.Explicit)]
public struct SDFParameters
{
    [FieldOffset(0)]
    public readonly Vector4 ChunkOffset;

    [FieldOffset(16)]
    public readonly uint ChunkSize;

    [FieldOffset(20)]
    public readonly uint Lod;

    [FieldOffset(24)]
    private readonly Vector2 Padding;

    public SDFParameters(Vector3 chunkOffset, uint chunkSize, uint lod)
    {
        // TODO: Add validation
        this.ChunkOffset = new Vector4(chunkOffset.X, chunkOffset.Y, chunkOffset.Z, 1.0f);
        this.ChunkSize = chunkSize;
        this.Lod = lod;
    }

    public byte[] ToByteArray()
    {
        int size = Marshal.SizeOf<SDFParameters>();
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
        if (obj == null || !(obj is SDFParameters))
        {
            return false;
        }

        SDFParameters other = (SDFParameters)obj;

        return ChunkSize == other.ChunkSize && Lod == other.Lod && ChunkOffset == other.ChunkOffset;
    }
    public static bool operator ==(SDFParameters left, SDFParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SDFParameters left, SDFParameters right)
    {
        return !(left == right);
    }
}
