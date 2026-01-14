using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator;

[StructLayout(LayoutKind.Explicit)]
public struct TerrainMeshVertex
{
    [FieldOffset(0)]
    public Vector3 Position;

    [FieldOffset(12)]
    private readonly uint Padding;

    [FieldOffset(16)]
    public Vector3 Normal;

    [FieldOffset(28)]
    public readonly uint Padding2;

    public TerrainMeshVertex(Vector3 position, Vector3 normal) 
    {
        Position = position;
        Normal = normal;
    }

    public override string ToString()
    {
        return $"(Position: {Position.ToString()} Normal: {Normal.ToString()})\n";
    }
}
