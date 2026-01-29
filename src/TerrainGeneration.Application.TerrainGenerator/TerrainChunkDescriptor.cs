using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator;
public class TerrainChunkDescriptor
{
    public Vector3 ChunkOffset;
    public uint ChunkSize;
    public uint Lod;
    public int Depth;
    public float BorderWidth;
}
