using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEvent;
using TerrainGeneration.Application.VoxelOctree.Abstractions.RenderOctree;

namespace TerrainGeneration.Application.VoxelOctree.OctreeEvents
{
    public class MoveWorldCenterEvent : IOctreeEvent
    {
        public MoveWorldCenterEvent(List<UpdatedHash> updatedHashes, Vector3 newWorldCenter)
        {
            UpdatedHashes = updatedHashes;
            NewWorldCenter = newWorldCenter;
        }

        public List<UpdatedHash> UpdatedHashes { get; set; }
        public Vector3 NewWorldCenter { get; set; }
    }
}
