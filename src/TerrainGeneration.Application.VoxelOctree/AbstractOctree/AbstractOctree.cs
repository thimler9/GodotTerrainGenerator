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
        private TerrainLod[] lodArray;
        private int minChunkSize;

        public AbstractOctreeNode[] chunks;
        private Vector3 center;
        private int size;

        private Vector3 oldPlayerPosition;
        private float playerPositionChangeThreshold;

        public AbstractOctree(IOctreeEventQueue eventQueue, Vector3 center, Vector3 playerPosition, int size, int minChunkSize, TerrainLod[] lodArray, float playerPositionChangeThreshold)
        {
            try
            {
                ValidateInputs(eventQueue, size, minChunkSize, lodArray, playerPositionChangeThreshold);

                this.lodArray = lodArray;
                this.minChunkSize = minChunkSize;
                this.center = center;
                this.size = size;
                this.playerPositionChangeThreshold = playerPositionChangeThreshold;

                int deepestDepth = GetDeepestDepth();
                int maxNumChunks = ((1 << ((deepestDepth + 2) * 3)) - 1) / 7;
                this.chunks = new AbstractOctreeNode[maxNumChunks];

                Vector3 firstOffset = this.center - new Vector3(size / 2, size / 2, size / 2);

                chunks[1] = new AbstractOctreeNode(chunks, eventQueue, false, firstOffset, size, playerPosition, 1, 0, minChunkSize, lodArray);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private int GetDeepestDepth()
        {
            int deepestDepth = 0;
            int currSize = size;
            while (currSize >= minChunkSize && deepestDepth < lodArray.Length)
            {
                deepestDepth += 1;
                currSize /= 2;
            }
            deepestDepth -= 1;

            return deepestDepth;
        }

        public void MoveWorldCenter(Vector3 playerPosition, Vector3 newWorldCenter, IOctreeEventQueue eventQueue)
        {
            Vector3 diff = newWorldCenter - center;
            AbstractOctreeNode[,,] children = new AbstractOctreeNode[4, 4, 4];
            // Adds the root children into an x,y,z grid
            for (int i = 0; i < 8; i++)
            {
                int x = (i % 2) * 2;
                int y = (i / 4) * 2;
                int z = i == 0 || i == 1 || i == 4 || i == 5 ? 0 : 2;

                if (chunks[(1 << 3) | i].HasChildren(chunks))
                {
                    children[x, y, z] = chunks[((1 << 3) | i) << 3];
                    children[x + 1, y, z] = chunks[(((1 << 3) | i) << 3) + 1];
                    children[x, y, z + 1] = chunks[(((1 << 3) | i) << 3) + 2];
                    children[x + 1, y, z + 1] = chunks[(((1 << 3) | i) << 3) + 3];
                    children[x, y + 1, z] = chunks[(((1 << 3) | i) << 3) + 4];
                    children[x + 1, y + 1, z] = chunks[(((1 << 3) | i) << 3) + 5];
                    children[x, y + 1, z + 1] = chunks[(((1 << 3) | i) << 3) + 6];
                    children[x + 1, y + 1, z + 1] = chunks[(((1 << 3) | i) << 3) + 7];
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
                        children[0, y, z].CollapseChildren(chunks, eventQueue);
                        eventQueue.AddEvent(new DisposeRenderNodeEvent(children[0, y, z].hash, children[0, y, z].offset, children[0, y, z].size));
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
                        children[3, y, z].CollapseChildren(chunks, eventQueue);
                        eventQueue.AddEvent(new DisposeRenderNodeEvent(children[3, y, z].hash, children[3, y, z].offset, children[3, y, z].size));
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
                            children[x, 0, z].CollapseChildren(chunks, eventQueue);
                            eventQueue.AddEvent(new DisposeRenderNodeEvent(children[x, 0, z].hash, children[x, 0, z].offset, children[x, 0, z].size));
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
                            children[x, 3, z].CollapseChildren(chunks, eventQueue);
                            eventQueue.AddEvent(new DisposeRenderNodeEvent(children[x, 3, z].hash, children[x, 3, z].offset, children[x, 3, z].size));
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
                            children[x, y, 0].CollapseChildren(chunks, eventQueue);
                            eventQueue.AddEvent(new DisposeRenderNodeEvent(children[x, y, 0].hash, children[x, y, 0].offset, children[x, y, 0].size));
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
                            children[x, y, 3].CollapseChildren(chunks, eventQueue);
                            eventQueue.AddEvent(new DisposeRenderNodeEvent(children[x, y, 3].hash, children[x, y, 3].offset, children[x, y, 3].size));
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
            AbstractOctreeNode[] newChunks = new AbstractOctreeNode[chunks.Length];

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
                            updatedHashes.Add(new UpdatedHash(children[x, y, z].hash, hash));
                            children[x, y, z].UpdateHash(hash, chunks, newChunks);
                        }
                        else
                        {
                            Vector3 offset = new Vector3((x - 2) * size / 4, (y - 2) * size / 4, (z - 2) * size / 4) + newWorldCenter;
                            newChunkHashAndOffset.Add((hash, offset));
                        }
                    }
                }
            }

            // Set the first two depth nodes
            newChunks[1] = chunks[1];
            newChunks[1].SetOffset(newWorldCenter - new Vector3(size / 2, size / 2, size / 2));
            for (int i = 0; i < 8; i++)
            {
                int x = (i % 2);
                int y = (i / 4);
                int z = i == 0 || i == 1 || i == 4 || i == 5 ? 0 : 1;

                chunks[(1 << 3) | i].SetOffset(new Vector3((x - 1), (y - 1), (z - 1)) * (size / 2) + newWorldCenter);
                newChunks[(1 << 3) | i] = chunks[(1 << 3) | i];
            }

            // Update Hashes Event
            eventQueue.AddEvent(new MoveWorldCenterEvent(updatedHashes, newWorldCenter));

            // Add Chunks Events
            for (int i = 0; i < newChunkHashAndOffset.Count; i++)
            {
                newChunks[newChunkHashAndOffset[i].hash] = new AbstractOctreeNode(newChunks, eventQueue, true, newChunkHashAndOffset[i].offset,
                    size / 4, playerPosition, newChunkHashAndOffset[i].hash, 2, minChunkSize, lodArray);
            }

            chunks = newChunks;
            center = newWorldCenter;
        }

        public void Update(IOctreeEventQueue eventQueue, Vector3 newPlayerPosition)
        {
            if (newPlayerPosition.DistanceSquaredTo(oldPlayerPosition) > playerPositionChangeThreshold * playerPositionChangeThreshold)
            {
                chunks[1].Update(chunks, eventQueue, newPlayerPosition, minChunkSize, lodArray);
                oldPlayerPosition = newPlayerPosition;

                Vector3 worldCenter = new Vector3(MathF.Round(newPlayerPosition.X / (size / 4), MidpointRounding.ToPositiveInfinity) * (size / 4),
                    MathF.Round(newPlayerPosition.Y / (size / 4), MidpointRounding.ToPositiveInfinity) * (size / 4), MathF.Round(newPlayerPosition.Z / (size / 4), MidpointRounding.ToPositiveInfinity) * (size / 4));

                // Move world if player has moved far enough
                if (!(center.X == worldCenter.X && center.Y == worldCenter.Y && center.Z == worldCenter.Z))
                {
                    MoveWorldCenter(newPlayerPosition, worldCenter, eventQueue);
                }
            }
        }

        public void ValidateInputs(IOctreeEventQueue eventQueue, int size, int minChunkSize, TerrainLod[] lodArray, float playerPositionChangeThreshold)
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

            int currSize = size;
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
