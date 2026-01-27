using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;
using TerrainGeneration.Application.VoxelOctree.AbstractOctree;

namespace TerrainGeneration.Application.VoxelOctree;
public class VoxelOctree
{
    private AbstractOctree.AbstractOctree AbstractOctree;
    private RenderOctree.RenderOctree RenderOctree;
    private OctreeEventQueue.OctreeEventQueue OctreeEventQueue;

    public VoxelOctree(VoxelOctreeDescriptor descriptor)
    {
        TransvoxelTerrainGenerator transvoxelTerrainGenerator = new TransvoxelTerrainGenerator(RenderingServer.GetRenderingDevice(), descriptor.TransvoxelTerrainGeneratorDescriptor);
        RenderOctree = new RenderOctree.RenderOctree(descriptor.Size, descriptor.MinChunkSize, descriptor.TerrainLods.Length, transvoxelTerrainGenerator);
        OctreeEventQueue = new OctreeEventQueue.OctreeEventQueue(RenderOctree, descriptor.EventQueueWorkBudget);
        AbstractOctree = new AbstractOctree.AbstractOctree(OctreeEventQueue, descriptor.Center, descriptor.PlayerPosition, descriptor.Size, descriptor.MinChunkSize, descriptor.TerrainLods, descriptor.PlayerPositionChangeThreshold);
    }
}
