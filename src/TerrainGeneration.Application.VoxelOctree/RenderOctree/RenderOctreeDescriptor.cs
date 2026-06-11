using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.TerrainGenerator.TerrainSpawns;

namespace TerrainGeneration.Application.VoxelOctree.RenderOctree;
public class RenderOctreeDescriptor
{
    public Vector3 Offset;
    public uint Size;
    public uint Lod;
    public int Depth;
    public int Hash;
    public ITerrainSpawnFactory? TerrainSpawnFactory;
}
