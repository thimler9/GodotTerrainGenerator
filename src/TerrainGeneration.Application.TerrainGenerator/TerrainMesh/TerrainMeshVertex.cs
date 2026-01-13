using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator;
public class TerrainMeshVertex
{
    public Vector3 Position { get; }
    public Vector3 Normal { get; }

    public TerrainMeshVertex(Vector3 position, Vector3 normal) 
    {
        Position = position;
        Normal = normal;
    }

    public override string ToString()
    {
        return $"({Position.ToString()}, {Normal.ToString()})";
    }
}
