using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEvent;

namespace TerrainGeneration.Application.VoxelOctree.OctreeEvents
{
    public class GetRenderNodeGameDataEvent : IOctreeEvent
    {
        public GetRenderNodeGameDataEvent(int hash, Vector3 offset, int size)
        {
            Hash = hash;
            Offset = offset;
            Size = size;
        }

        public int Hash { get; set; }
        public Vector3 Offset { get; set; }
        public int Size { get; set; }
    }
}
