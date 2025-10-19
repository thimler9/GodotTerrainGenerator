using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEvent;

namespace TerrainGeneration.Application.VoxelOctree.Abstractions.RenderOctree
{
    public interface IRenderOctree
    {
        public void Render(Vector3 playerPosition);

        /// <summary>
        /// Clears up octree resources
        /// </summary>
        public void Dispose();

        public void ProcessEvents(IOctreeEvent[] events);
    }
}
