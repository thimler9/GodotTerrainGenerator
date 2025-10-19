using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEvent;

namespace TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEventQueue
{
    public interface IOctreeEventQueue
    {
        public void AddEvent(IOctreeEvent octreeEvent);

        public void Process();
    }
}
