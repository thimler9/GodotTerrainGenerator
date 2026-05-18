using Godot;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEvent;

namespace TerrainGeneration.Application.VoxelOctree.OctreeEvents
{
    public enum ChunkIntentState
    {
        Missing,
        Internal,
        Leaf
    }

    public class ChunkIntentEvent : IOctreeEvent
    {
        public ChunkIntentEvent(int hash, Vector3 offset, uint size, uint lod, int depth, ChunkIntentState state)
        {
            Hash = hash;
            Offset = offset;
            Size = size;
            Lod = lod;
            Depth = depth;
            State = state;
        }

        public int Hash { get; set; }
        public Vector3 Offset { get; set; }
        public uint Size { get; set; }
        public uint Lod { get; set; }
        public int Depth { get; set; }
        public ChunkIntentState State { get; set; }
    }
}
