using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEventQueue;
using TerrainGeneration.Application.VoxelOctree.OctreeEvents;

namespace TerrainGeneration.Application.VoxelOctree.AbstractOctree
{
    public class AbstractOctreeNode
    {
        public readonly int size;
        public Vector3 offset;
        public int hash;
        public readonly int depth;
        public readonly int lod;

        public AbstractOctreeNode(AbstractOctreeNode[] chunks, IOctreeEventQueue eventQueue, bool updateBorders, Vector3 offset, int size, Vector3 playerPosition, int hash,
            int depth, int minChunkSize, TerrainLod[] lodArray)
        {

            this.offset = offset;
            this.size = size;
            this.hash = hash;
            this.depth = depth;
            this.lod = lodArray[depth].lodDivider;

            eventQueue.AddEvent(new CreateRenderNodeEvent(hash, offset, size, lod, depth));

            if (this.size / 2 > minChunkSize && this.depth < lodArray.Length - 1 && PlayerDistanceCheck(playerPosition, lodArray))
            {
                MakeChildren(chunks, eventQueue, updateBorders, playerPosition, minChunkSize, lodArray);
            }
        }

        private void MakeChildren(AbstractOctreeNode[] chunks, IOctreeEventQueue eventQueue, bool updateBorders, Vector3 playerPosition, int minChunkSize, TerrainLod[] lodArray)
        {
            int newSize = size / 2;
            chunks[hash << 3] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, offset, newSize, playerPosition, (hash << 3), depth + 1, minChunkSize, lodArray);
            chunks[(hash << 3) | 1] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, offset + new Vector3(newSize, 0, 0), newSize, playerPosition, (hash << 3) | 1, depth + 1, minChunkSize, lodArray);
            chunks[(hash << 3) | 2] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, offset + new Vector3(0, 0, newSize), newSize, playerPosition, (hash << 3) | 2, depth + 1, minChunkSize, lodArray);
            chunks[(hash << 3) | 3] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, offset + new Vector3(newSize, 0, newSize), newSize, playerPosition, (hash << 3) | 3, depth + 1, minChunkSize, lodArray);
            chunks[(hash << 3) | 4] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, offset + new Vector3(0, newSize, 0), newSize, playerPosition, (hash << 3) | 4, depth + 1, minChunkSize, lodArray);
            chunks[(hash << 3) | 5] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, offset + new Vector3(newSize, newSize, 0), newSize, playerPosition, (hash << 3) | 5, depth + 1, minChunkSize, lodArray);
            chunks[(hash << 3) | 6] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, offset + new Vector3(0, newSize, newSize), newSize, playerPosition, (hash << 3) | 6, depth + 1, minChunkSize, lodArray);
            chunks[(hash << 3) | 7] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, offset + new Vector3(newSize, newSize, newSize), newSize, playerPosition, (hash << 3) | 7, depth + 1, minChunkSize, lodArray);
            eventQueue.AddEvent(new DeleteRenderNodeGameDataEvent(hash, offset, size));
        }

        public void Update(AbstractOctreeNode[] chunks, IOctreeEventQueue eventQueue, Vector3 playerPosition, int minChunkSize, TerrainLod[] lodArray)
        {
            if (HasChildren(chunks))
            {
                for (int i = 0; i < 8; i++)
                {
                    AbstractOctreeNode child = chunks[(hash << 3) | i];
                    bool childPlayerDstCheck = child.PlayerDistanceCheck(playerPosition, lodArray);

                    bool childHasChildren = child.HasChildren(chunks);
                    // If child is close to player
                    if (childPlayerDstCheck)
                    {
                        // Divide child up if it has no children
                        if (!childHasChildren && (child.size / 2 > minChunkSize && child.depth < lodArray.Length - 1))
                        {
                            // Split up child
                            child.MakeChildren(chunks, eventQueue, false, playerPosition, minChunkSize, lodArray);
                        }
                        else
                        {
                            child.Update(chunks, eventQueue, playerPosition, minChunkSize, lodArray);
                        }
                    }
                    // Child is far from player and has children, we need to collapse
                    else if (childHasChildren)
                    {
                        // Collapse children of current node
                        child.CollapseChildren(chunks, eventQueue);
                    }
                }
            }
        }

        public void CollapseChildren(AbstractOctreeNode[] chunks, IOctreeEventQueue eventQueue)
        {
            if (HasChildren(chunks))
            {
                eventQueue.AddEvent(new GetRenderNodeGameDataEvent(hash, offset, size));
                for (int i = 0; i < 8; i++)
                {
                    AbstractOctreeNode child = chunks[(hash << 3) | i];
                    child.CollapseChildren(chunks, eventQueue);
                    // Remove parent's children
                    eventQueue.AddEvent(new DisposeRenderNodeEvent(child.hash, child.offset, child.size));
                    chunks[child.hash] = null;
                }
            }
        }

        public bool HasChildren(AbstractOctreeNode[] chunks)
        {
            return (hash << 3) < chunks.Length && chunks[(hash << 3)] != null;
        }

        public bool PlayerDistanceCheck(Vector3 playerPosition, TerrainLod[] lodArray)
        {
            Vector3 center = offset + new Vector3(size / 2, size / 2, size / 2);
            float sqDistFromPlayer = center.DistanceSquaredTo(playerPosition);
            int chosenLod = lodArray[0].lodDivider;
            for (int i = 1; i < lodArray.Length; i++)
            {
                float sqLodDistanceCutoff = lodArray[i].lodDistanceCutoff * lodArray[i].lodDistanceCutoff;
                if (sqLodDistanceCutoff > sqDistFromPlayer)
                {
                    chosenLod = lodArray[i].lodDivider;
                }
                else
                {
                    break;
                }
            }

            return chosenLod < lod;
        }

        public string GetOctalOfHash()
        {
            return Convert.ToString(hash, 8);
        }

        public void UpdateHash(int newHash, AbstractOctreeNode[] oldChunks, AbstractOctreeNode[] newChunks)
        {
            if (HasChildren(oldChunks))
            {
                for (int i = 0; i < 8; i++)
                {
                    oldChunks[(hash << 3) | i].UpdateHash((newHash << 3) | i, oldChunks, newChunks);
                }
            }

            hash = newHash;
            newChunks[hash] = this;
        }

        public void SetOffset(Vector3 offset)
        {
            this.offset = offset;
        }
    }
}
