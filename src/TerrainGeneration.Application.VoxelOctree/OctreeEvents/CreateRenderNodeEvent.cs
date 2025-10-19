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
    public class CreateRenderNodeEvent : IOctreeEvent
    {
        public CreateRenderNodeEvent(int hash, Vector3 offset, int size, int lod, int depth)
        {
            Hash = hash;
            Offset = offset;
            Size = size;
            Lod = lod;
            Depth = depth;
        }

        public int Hash { get; set; }
        public Vector3 Offset { get; set; }
        public int Size { get; set; }
        public int Lod { get; set; }
        public int Depth { get; set; }
    }
}
