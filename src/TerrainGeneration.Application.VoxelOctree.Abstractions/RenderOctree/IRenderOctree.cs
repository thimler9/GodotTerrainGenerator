using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEvent;

namespace TerrainGeneration.Application.VoxelOctree.Abstractions.RenderOctree
{
    public interface IRenderOctree
    {
        public void Render(Array<Plane> frustumPlanes, TerrainMeshRenderDescriptor terrainMeshRenderDescriptor);

        /// <summary>
        /// Clears up octree resources
        /// </summary>
        public void Dispose();

        public void ProcessEvents(OctreeEvent.OctreeEvent[] events);
    }
}
