using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEventQueue;

namespace TerrainGeneration.Application.VoxelOctree.Abstractions.AbstractOctree
{
    public interface IAbstractOctree
    {
        public void Update(IOctreeEventQueue eventQueue, Vector3 newPlayerPosition);

        public void MoveWorldCenter(Vector3 playerPosition, Vector3 newWorldCenter, IOctreeEventQueue eventQueue);

    }
}
