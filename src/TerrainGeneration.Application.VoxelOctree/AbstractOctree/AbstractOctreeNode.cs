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
        public readonly uint Size;
        public Vector3 Offset;
        public int Hash;
        public readonly int Depth;
        public readonly uint Lod;

        /// <summary>
        /// Creates a new abstract octree node. 
        /// </summary>
        /// <param name="chunks">List of the current chunks. Used to add new chunks to the array</param>
        /// <param name="eventQueue"></param>
        /// <param name="updateBorders"></param>
        /// <param name="offset">The location of the (0, 0, 0) corner of the chunk.</param>
        /// <param name="size"></param>
        /// <param name="playerPosition"></param>
        /// <param name="hash">The has of the abstract, used for indexing in the chunks array. Every layer of the octree is 8 bits of the hash. The next depth is hash << 3.</param>
        /// <param name="depth">The depth of the current node</param>
        /// <param name="minChunkSize"></param>
        /// <param name="lodArray"></param>
        public AbstractOctreeNode(AbstractOctreeNode?[] chunks, IOctreeEventQueue eventQueue, bool updateBorders, Vector3 offset, uint size, Vector3 playerPosition, int hash,
            int depth, uint minChunkSize, TerrainLod[] lodArray)
        {

            this.Offset = offset;
            this.Size = size;
            this.Hash = hash;
            this.Depth = depth;
            this.Lod = lodArray[depth].LodDivider;

            eventQueue.AddEvent(new CreateRenderNodeEvent(hash, offset, size, Lod, depth));

            // If the chunk can be split up because: it's not too small, there is a small lod, and the player is close enough
            if (this.Size / 2 > minChunkSize && this.Depth < lodArray.Length - 1 && PlayerDistanceCheck(playerPosition, lodArray))
            {
                MakeChildren(chunks, eventQueue, updateBorders, playerPosition, minChunkSize, lodArray);
            }
        }

        /// <summary>
        /// Splits up the chunk and makes 8 children.
        /// </summary>
        /// <param name="chunks"></param>
        /// <param name="eventQueue"></param>
        /// <param name="updateBorders"></param>
        /// <param name="playerPosition"></param>
        /// <param name="minChunkSize"></param>
        /// <param name="lodArray"></param>
        private void MakeChildren(AbstractOctreeNode?[] chunks, IOctreeEventQueue eventQueue, bool updateBorders, Vector3 playerPosition, uint minChunkSize, TerrainLod[] lodArray)
        {
            uint newSize = Size / 2;
            chunks[Hash << 3] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, Offset, newSize, playerPosition, (Hash << 3), Depth + 1, minChunkSize, lodArray);
            chunks[(Hash << 3) | 1] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, Offset + new Vector3(newSize, 0, 0), newSize, playerPosition, (Hash << 3) | 1, Depth + 1, minChunkSize, lodArray);
            chunks[(Hash << 3) | 2] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, Offset + new Vector3(0, 0, newSize), newSize, playerPosition, (Hash << 3) | 2, Depth + 1, minChunkSize, lodArray);
            chunks[(Hash << 3) | 3] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, Offset + new Vector3(newSize, 0, newSize), newSize, playerPosition, (Hash << 3) | 3, Depth + 1, minChunkSize, lodArray);
            chunks[(Hash << 3) | 4] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, Offset + new Vector3(0, newSize, 0), newSize, playerPosition, (Hash << 3) | 4, Depth + 1, minChunkSize, lodArray);
            chunks[(Hash << 3) | 5] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, Offset + new Vector3(newSize, newSize, 0), newSize, playerPosition, (Hash << 3) | 5, Depth + 1, minChunkSize, lodArray);
            chunks[(Hash << 3) | 6] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, Offset + new Vector3(0, newSize, newSize), newSize, playerPosition, (Hash << 3) | 6, Depth + 1, minChunkSize, lodArray);
            chunks[(Hash << 3) | 7] = new AbstractOctreeNode(chunks, eventQueue, updateBorders, Offset + new Vector3(newSize, newSize, newSize), newSize, playerPosition, (Hash << 3) | 7, Depth + 1, minChunkSize, lodArray);
            eventQueue.AddEvent(new DeleteRenderNodeTerrainChunkEvent(Hash, Offset, Size));
        }

        /// <summary>
        /// Traverses the tree finding what chunks need to be collapsed or split up based on the player's position.
        /// </summary>
        /// <param name="chunks"></param>
        /// <param name="eventQueue"></param>
        /// <param name="playerPosition"></param>
        /// <param name="minChunkSize"></param>
        /// <param name="lodArray"></param>
        public void Update(AbstractOctreeNode?[] chunks, IOctreeEventQueue eventQueue, Vector3 playerPosition, uint minChunkSize, TerrainLod[] lodArray)
        {
            if (HasChildren(chunks))
            {
                for (int i = 0; i < 8; i++)
                {
                    AbstractOctreeNode? child = chunks[(Hash << 3) | i];
                    bool childPlayerDstCheck = child.PlayerDistanceCheck(playerPosition, lodArray);

                    bool childHasChildren = child.HasChildren(chunks);
                    // If child is close to player
                    if (childPlayerDstCheck)
                    {
                        // Divide child up if it has no children
                        if (!childHasChildren && (child.Size / 2 > minChunkSize && child.Depth < lodArray.Length - 1))
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

        /// <summary>
        /// Removes all children in this node's subtree.
        /// </summary>
        /// <param name="chunks"></param>
        /// <param name="eventQueue"></param>
        public void CollapseChildren(AbstractOctreeNode?[] chunks, IOctreeEventQueue eventQueue)
        {
            if (HasChildren(chunks))
            {
                eventQueue.AddEvent(new GetRenderNodeTerrainChunkEvent(Hash, Offset, Size));
                for (int i = 0; i < 8; i++)
                {
                    AbstractOctreeNode? child = chunks[(Hash << 3) | i];
                    child.CollapseChildren(chunks, eventQueue);
                    // Remove parent's children
                    eventQueue.AddEvent(new DisposeRenderNodeEvent(child.Hash, child.Offset, child.Size));
                    chunks[child.Hash] = null;
                }
            }
        }

        /// <summary>
        /// Checks if chunk has children
        /// </summary>
        /// <param name="chunks"></param>
        /// <returns></returns>
        public bool HasChildren(AbstractOctreeNode?[] chunks)
        {
            return (Hash << 3) < chunks.Length && chunks[(Hash << 3)] != null;
        }

        /// <summary>
        /// Checks if person is close enough to chunk.
        /// </summary>
        /// <param name="playerPosition"></param>
        /// <param name="lodArray"></param>
        /// <returns></returns>
        public bool PlayerDistanceCheck(Vector3 playerPosition, TerrainLod[] lodArray)
        {
            Vector3 center = Offset + new Vector3(Size / 2, Size / 2, Size / 2);
            float sqDistFromPlayer = center.DistanceSquaredTo(playerPosition);
            uint chosenLod = lodArray[0].LodDivider;
            for (int i = 1; i < lodArray.Length; i++)
            {
                float sqLodDistanceCutoff = lodArray[i].LodDistanceCutoff * lodArray[i].LodDistanceCutoff;
                if (sqLodDistanceCutoff > sqDistFromPlayer)
                {
                    chosenLod = lodArray[i].LodDivider;
                }
                else
                {
                    break;
                }
            }

            return chosenLod < Lod;
        }

        /// <summary>
        /// Used for debugging purposes. Prints the node hash in octal.
        /// </summary>
        /// <returns></returns>
        public string GetOctalOfHash()
        {
            return Convert.ToString(Hash, 8);
        }

        /// <summary>
        /// Updates the hash of this node and all nodes in the subtree
        /// </summary>
        /// <param name="newHash"></param>
        /// <param name="oldChunks"></param>
        /// <param name="newChunks"></param>
        public void UpdateHash(int newHash, AbstractOctreeNode?[] oldChunks, AbstractOctreeNode?[] newChunks)
        {
            if (HasChildren(oldChunks))
            {
                for (int i = 0; i < 8; i++)
                {
                    oldChunks[(Hash << 3) | i].UpdateHash((newHash << 3) | i, oldChunks, newChunks);
                }
            }

            Hash = newHash;
            newChunks[Hash] = this;
        }

        /// <summary>
        /// Sets the offset's value.
        /// </summary>
        /// <param name="offset"></param>
        public void SetOffset(Vector3 offset)
        {
            this.Offset = offset;
        }
    }
}
