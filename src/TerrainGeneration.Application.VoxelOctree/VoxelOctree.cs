using Godot;
using Godot.Collections;
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
        RenderOctree = new RenderOctree.RenderOctree(descriptor.Size, descriptor.MinChunkSize, descriptor.TerrainLods.Length, descriptor.BorderWidth, transvoxelTerrainGenerator);
        OctreeEventQueue = new OctreeEventQueue.OctreeEventQueue(RenderOctree, descriptor.EventQueueWorkBudget);
        AbstractOctree = new AbstractOctree.AbstractOctree(OctreeEventQueue, descriptor.Center, descriptor.PlayerPosition, descriptor.Size, descriptor.MinChunkSize, descriptor.TerrainLods, descriptor.PlayerPositionChangeThreshold);
    }

    public void UpdateAbstractTree(Vector3 playerPosition)
    {
        //AbstractOctree.Update(OctreeEventQueue, playerPosition);
    }

    public void ProcessEventQueue()
    {
        OctreeEventQueue.Process();
    }

    public void Render(Array<Plane> frustumPlanes, TerrainMeshRenderDescriptor terrainMeshRenderDescriptor)
    {
        RenderOctree.Render(frustumPlanes, terrainMeshRenderDescriptor);
    }
}
