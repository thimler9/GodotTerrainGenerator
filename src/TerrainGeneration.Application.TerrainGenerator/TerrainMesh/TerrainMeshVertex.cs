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
    public Vector4 Position;

    [FieldOffset(16)]
    public Vector4 Normal;

    public TerrainMeshVertex(Vector4 position, Vector4 normal) 
    {
        Position = position;
        Normal = normal;
    }

    public override string ToString()
    {
        return $"(Position: {Position.ToString()} Normal: {Normal.ToString()})\n";
    }
}
