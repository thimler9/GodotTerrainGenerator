using Godot;
using System;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.VoxelOctree.Abstractions.AbstractOctree;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEventQueue;
using TerrainGeneration.Application.VoxelOctree.Abstractions.RenderOctree;
using TerrainGeneration.Application.VoxelOctree.OctreeEvents;
using TerrainGeneration.Utilities.Math.Extensions;

namespace TerrainGeneration.Application.VoxelOctree.AbstractOctree
{
    public class AbstractOctree : IAbstractOctree
    {
        private TerrainLod[] LodArray;
        private int MinChunkSize;

        public AbstractOctreeNode[] Chunks;
        private Vector3 Center;
        private uint Size;

        private Vector3 OldPlayerPosition;
        private float PlayerPositionChangeThreshold;

        /// <summary>
        /// Creates an abstract voxel octree. This one always has the most up to data in the structure according to the player position. Does not hold any rendering data for the chunk.
        /// </summary>
        /// <param name="eventQueue">We put rendering events into this queue according to updates we make to this tree.</param>
        /// <param name="center">The center of the tree in the world</param>
        /// <param name="playerPosition">The starting player position</param>
        /// <param name="size">The length of one side of the box the tree contains.</param>
        /// <param name="minChunkSize">Will not split up to chunk sizes smaller than this.</param>
        /// <param name="lodArray">Used to determine what the lod is for the given depth of the current tree node.</param>
        /// <param name="playerPositionChangeThreshold">Won't update the tree unless the player has moved more than this.</param>
        public AbstractOctree(IOctreeEventQueue eventQueue, Vector3 center, Vector3 playerPosition, uint size, int minChunkSize, TerrainLod[] lodArray, float playerPositionChangeThreshold)
        {
            try
            {
                ValidateInputs(eventQueue, size, minChunkSize, lodArray, playerPositionChangeThreshold);

                this.LodArray = lodArray;
                this.MinChunkSize = minChunkSize;
                this.Center = center;
                this.Size = size;
                this.PlayerPositionChangeThreshold = playerPositionChangeThreshold;

                int deepestDepth = GetDeepestDepth();
                int maxNumChunks = ((1 << ((deepestDepth + 2) * 3)) - 1) / 7;
                this.Chunks = new AbstractOctreeNode[maxNumChunks];

                Vector3 firstOffset = this.Center - new Vector3(size / 2, size / 2, size / 2);

                Chunks[1] = new AbstractOctreeNode(Chunks, eventQueue, false, firstOffset, size, playerPosition, 1, 0, minChunkSize, lodArray);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// The lowest depth the tree will go. Based on min chunk size and lod array.
        /// </summary>
        /// <returns></returns>
        private int GetDeepestDepth()
        {
            int deepestDepth = 0;
            uint currSize = Size;
            while (currSize >= MinChunkSize && deepestDepth < LodArray.Length)
            {
                deepestDepth += 1;
                currSize /= 2;
            }
            deepestDepth -= 1;

            return deepestDepth;
        }
        
        /// <summary>
        /// Shifts the world center over. This pretty much shifts the entire tree over based on the second depth of the tree which is 4x4x4.
        /// Will shift by the chunksize of that depth of the tree. Pretty slow will add a bunch of nodes to the queue. Updates all of the leaf
        /// hashes for the tree.
        /// </summary>
        /// <param name="playerPosition">Current player position</param>
        /// <param name="newWorldCenter"></param>
        /// <param name="eventQueue"></param>
        public void MoveWorldCenter(Vector3 playerPosition, Vector3 newWorldCenter, IOctreeEventQueue eventQueue)
        {
            Vector3 diff = newWorldCenter - Center;
            AbstractOctreeNode[,,] children = new AbstractOctreeNode[4, 4, 4];
            // Adds the root children into an x,y,z grid
            for (int i = 0; i < 8; i++)
            {
                int x = (i % 2) * 2;
                int y = (i / 4) * 2;
                int z = i == 0 || i == 1 || i == 4 || i == 5 ? 0 : 2;

                if (Chunks[(1 << 3) | i].HasChildren(Chunks))
                {
                    children[x, y, z] = Chunks[((1 << 3) | i) << 3];
                    children[x + 1, y, z] = Chunks[(((1 << 3) | i) << 3) + 1];
                    children[x, y, z + 1] = Chunks[(((1 << 3) | i) << 3) + 2];
                    children[x + 1, y, z + 1] = Chunks[(((1 << 3) | i) << 3) + 3];
                    children[x, y + 1, z] = Chunks[(((1 << 3) | i) << 3) + 4];
                    children[x + 1, y + 1, z] = Chunks[(((1 << 3) | i) << 3) + 5];
                    children[x, y + 1, z + 1] = Chunks[(((1 << 3) | i) << 3) + 6];
                    children[x + 1, y + 1, z + 1] = Chunks[(((1 << 3) | i) << 3) + 7];
                }
            }

            // Shift left
            if (diff.X > 0)
            {
                // Dispose
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        children[0, y, z].CollapseChildren(Chunks, eventQueue);
                        eventQueue.AddEvent(new DisposeRenderNodeEvent(children[0, y, z].Hash, children[0, y, z].Offset, children[0, y, z].Size));
                    }
                }


                // Shift 
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        for (int z = 0; z < 4; z++)
                        {
                            children[x, y, z] = children[x + 1, y, z];
                        }
                    }
                }

                // Add new
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        children[3, y, z] = null;
                    }
                }
            }
            // Shift Right
            else if (diff.X < 0)
            {
                // Dispose
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        children[3, y, z].CollapseChildren(Chunks, eventQueue);
                        eventQueue.AddEvent(new DisposeRenderNodeEvent(children[3, y, z].Hash, children[3, y, z].Offset, children[3, y, z].Size));
                    }
                }

                // Shift 
                for (int x = 3; x >= 1; x--)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        for (int z = 0; z < 4; z++)
                        {
                            children[x, y, z] = children[x - 1, y, z];
                        }
                    }
                }

                // Add new
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        children[0, y, z] = null;
                    }
                }
            }

            // Shift down
            if (diff.Y > 0)
            {
                // Dispose
                for (int x = 0; x < 4; x++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        if (children[x, 0, z] != null)
                        {
                            children[x, 0, z].CollapseChildren(Chunks, eventQueue);
                            eventQueue.AddEvent(new DisposeRenderNodeEvent(children[x, 0, z].Hash, children[x, 0, z].Offset, children[x, 0, z].Size));
                        }
                    }
                }

                // Shift 
                for (int x = 0; x < 4; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        for (int z = 0; z < 4; z++)
                        {
                            children[x, y, z] = children[x, y + 1, z];
                        }
                    }
                }

                // Add new
                for (int x = 0; x < 4; x++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        children[x, 3, z] = null;
                    }
                }
            }
            // Shift up
            else if (diff.Y < 0)
            {
                // Dispose
                for (int x = 0; x < 4; x++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        if (children[x, 3, z] != null)
                        {
                            children[x, 3, z].CollapseChildren(Chunks, eventQueue);
                            eventQueue.AddEvent(new DisposeRenderNodeEvent(children[x, 3, z].Hash, children[x, 3, z].Offset, children[x, 3, z].Size));
                        }
                    }
                }

                // Shift 
                for (int x = 0; x < 4; x++)
                {
                    for (int y = 3; y >= 1; y--)
                    {
                        for (int z = 0; z < 4; z++)
                        {
                            children[x, y, z] = children[x, y - 1, z];
                        }
                    }
                }

                // Add new
                for (int x = 0; x < 4; x++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        children[x, 0, z] = null;
                    }
                }
            }

            // Shift backwards
            if (diff.Z > 0)
            {
                // Dispose
                for (int x = 0; x < 4; x++)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        if (children[x, y, 0] != null)
                        {
                            children[x, y, 0].CollapseChildren(Chunks, eventQueue);
                            eventQueue.AddEvent(new DisposeRenderNodeEvent(children[x, y, 0].Hash, children[x, y, 0].Offset, children[x, y, 0].Size));
                        }
                    }
                }

                // Shift 
                for (int x = 0; x < 4; x++)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        for (int z = 0; z < 3; z++)
                        {
                            children[x, y, z] = children[x, y, z + 1];
                        }
                    }
                }

                // Add new
                for (int x = 0; x < 4; x++)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        children[x, y, 3] = null;
                    }
                }
            }
            // Shift forwards
            else if (diff.Z < 0)
            {
                // Dispose
                for (int x = 0; x < 4; x++)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        if (children[x, y, 3] != null)
                        {
                            children[x, y, 3].CollapseChildren(Chunks, eventQueue);
                            eventQueue.AddEvent(new DisposeRenderNodeEvent(children[x, y, 3].Hash, children[x, y, 3].Offset, children[x, y, 3].Size));
                        }
                    }
                }

                // Shift 
                for (int x = 0; x < 4; x++)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        for (int z = 3; z >= 1; z--)
                        {
                            children[x, y, z] = children[x, y, z - 1];
                        }
                    }
                }

                // Add new
                for (int x = 0; x < 4; x++)
                {
                    for (int y = 0; y < 4; y++)
                    {
                        children[x, y, 0] = null;
                    }
                }
            }

            // Create new chunks array
            AbstractOctreeNode[] newChunks = new AbstractOctreeNode[Chunks.Length];

            List<UpdatedHash> updatedHashes = new List<UpdatedHash>(64);
            List<(int hash, Vector3 offset)> newChunkHashAndOffset = new List<(int, Vector3)>(64);

            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        int hash_x = CoordinateToHashLookup.coordinateToHashLookup[x];
                        int hash_y = 4 * CoordinateToHashLookup.coordinateToHashLookup[y];
                        int hash_z = 2 * CoordinateToHashLookup.coordinateToHashLookup[z];
                        int hash = hash_x + hash_y + hash_z + (1 << 6); // 1 << 6 puts the leading one at the end of the hash

                        if (children[x, y, z] != null)
                        {
                            updatedHashes.Add(new UpdatedHash(children[x, y, z].Hash, hash));
                            children[x, y, z].UpdateHash(hash, Chunks, newChunks);
                        }
                        else
                        {
                            Vector3 offset = new Vector3((x - 2) * Size / 4, (y - 2) * Size / 4, (z - 2) * Size / 4) + newWorldCenter;
                            newChunkHashAndOffset.Add((hash, offset));
                        }
                    }
                }
            }

            // Set the first two depth nodes
            newChunks[1] = Chunks[1];
            newChunks[1].SetOffset(newWorldCenter - new Vector3(Size / 2, Size / 2, Size / 2));
            for (int i = 0; i < 8; i++)
            {
                int x = (i % 2);
                int y = (i / 4);
                int z = i == 0 || i == 1 || i == 4 || i == 5 ? 0 : 1;

                Chunks[(1 << 3) | i].SetOffset(new Vector3((x - 1), (y - 1), (z - 1)) * (Size / 2) + newWorldCenter);
                newChunks[(1 << 3) | i] = Chunks[(1 << 3) | i];
            }

            // Update Hashes Event
            eventQueue.AddEvent(new MoveWorldCenterEvent(updatedHashes, newWorldCenter));

            // Add Chunks Events
            for (int i = 0; i < newChunkHashAndOffset.Count; i++)
            {
                newChunks[newChunkHashAndOffset[i].hash] = new AbstractOctreeNode(newChunks, eventQueue, true, newChunkHashAndOffset[i].offset,
                    Size / 4, playerPosition, newChunkHashAndOffset[i].hash, 2, MinChunkSize, LodArray);
            }

            Chunks = newChunks;
            Center = newWorldCenter;
        }

        /// <summary>
        /// Updates all of the nodes necessary in the tree based on the new player position. Will split up or collapse parts of the tree
        /// based on the new player position.
        /// </summary>
        /// <param name="eventQueue"></param>
        /// <param name="newPlayerPosition"></param>
        public void Update(IOctreeEventQueue eventQueue, Vector3 newPlayerPosition)
        {
            if (newPlayerPosition.DistanceSquaredTo(OldPlayerPosition) > PlayerPositionChangeThreshold * PlayerPositionChangeThreshold)
            {
                Chunks[1].Update(Chunks, eventQueue, newPlayerPosition, MinChunkSize, LodArray);
                OldPlayerPosition = newPlayerPosition;

                Vector3 worldCenter = new Vector3(MathF.Round(newPlayerPosition.X / (Size / 4), MidpointRounding.ToPositiveInfinity) * (Size / 4),
                    MathF.Round(newPlayerPosition.Y / (Size / 4), MidpointRounding.ToPositiveInfinity) * (Size / 4), MathF.Round(newPlayerPosition.Z / (Size / 4), MidpointRounding.ToPositiveInfinity) * (Size / 4));

                // Move world if player has moved far enough
                if (!(Center.X == worldCenter.X && Center.Y == worldCenter.Y && Center.Z == worldCenter.Z))
                {
                    MoveWorldCenter(newPlayerPosition, worldCenter, eventQueue);
                }
            }
        }

        public void ValidateInputs(IOctreeEventQueue eventQueue, uint size, int minChunkSize, TerrainLod[] lodArray, float playerPositionChangeThreshold)
        {
            // Validate Size
            if (!(size > 0 && size.IsPowerOfTwo()))
            {
                throw new ArgumentException("The octree size must be a power of 2.");
            }

            // Validate lod array
            if (lodArray.Length <= 0)
            {
                throw new ArgumentException("The lod array must be at least size 1.");
            }

            float previousDistance = float.MaxValue;
            for (int i = 0; i < lodArray.Length; i++)
            {
                if (!(lodArray[i].lodDivider > 0 && lodArray[i].lodDivider.IsPowerOfTwo()))
                {
                    throw new ArgumentException("All the elements in the lod array must be powers of two.");
                }

                if (previousDistance <= lodArray[i].lodDistanceCutoff)
                {
                    throw new ArgumentException("All lod distance cutoffs of the lod array need to be smaller than the previous.");
                }
                previousDistance = lodArray[i].lodDistanceCutoff;
            }

            uint currSize = size;
            for (int i = 0; i < lodArray.Length; i++)
            {
                if (currSize / lodArray[i].lodDivider < 8)
                {
                    throw new ArgumentException("The size and the corresponding element in the lod array should not have a dividend of less than 8.");
                }
                currSize /= 2;
            }

            // Validate min chunk size
            if (minChunkSize <= 0)
            {
                throw new ArgumentException("The min chunk size must be positive.");
            }

            // Validate player position change threshold
            if (playerPositionChangeThreshold <= 0)
            {
                throw new ArgumentException("The player position threshold must be positive.");
            }

            if (eventQueue == null)
            {
                throw new ArgumentException("Event queue cannot be null");
            }
        }
    }
}
