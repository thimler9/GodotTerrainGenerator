using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace TerrainGeneration.Application.SDFGenerator.SimplexNoise;

[StructLayout(LayoutKind.Explicit)]
public struct SimplexNoiseParameters
{
    [FieldOffset(0)]
    public readonly Vector3 Offset;
    [FieldOffset(12)]
    public readonly uint Seed;

    [FieldOffset(16)]
    public readonly Vector3 Scale;

    [FieldOffset(28)]
    public readonly float Strength;

    [FieldOffset(32)]
    public readonly uint NumOctaves;

    [FieldOffset(36)]
    public readonly float Frequency;

    [FieldOffset(40)]
    public readonly float Amplitude;

    [FieldOffset(44)]
    public readonly float Lacunarity;

    [FieldOffset(48)]
    public readonly float Gain;

    [FieldOffset(52)]
    public readonly Vector3 Padding;

    public SimplexNoiseParameters(Vector3 offset, uint seed, Vector3 scale, float strength, uint numOctaves, float frequency, float amplitude, float lacunarity, float gain)
    {
        //TODO: Add Validation

        Offset = offset;
        Seed = seed;
        Scale = scale;
        Strength = strength;
        NumOctaves = numOctaves;
        Frequency = frequency;
        Amplitude = amplitude;
        Lacunarity = lacunarity;
        Gain = gain;
    }

    public byte[] ToByteArray()
    {
        int size = Marshal.SizeOf<SimplexNoiseParameters>();
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
}
