using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;
using TerrainGeneration.Application.TerrainGenerator.TerrainSpawns;
using TerrainGeneration.Application.VoxelOctree.AbstractOctree;

namespace TerrainGeneration.Application.VoxelOctree;
public class VoxelOctreeDescriptor
{
    public required Vector3 Center;
    public required uint Size;
    public required uint MinChunkSize;
    public required TerrainLod[] TerrainLods;
    public required float BorderWidth;
    public required Vector3 PlayerPosition;
    public required float PlayerPositionChangeThreshold;
    public required uint EventQueueWorkBudget;

    public required TransvoxelTerrainGeneratorDescriptor TransvoxelTerrainGeneratorDescriptor;
    public ITerrainSpawnFactory? TerrainSpawnFactory;
}
