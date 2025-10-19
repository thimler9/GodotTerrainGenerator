using Godot;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEvent;

namespace TerrainGeneration.Application.VoxelOctree.RenderOctree
{
    internal class RenderOctree
    {
        private int size;
        private int minChunkSize;
        private int lodArrayLength;

        public RenderOctreeNode[] chunks;
        public bool[] leafHashes;

        public RenderOctree(int size, int minChunkSize, int lodArrayLength)
        {
            this.size = size;
            this.minChunkSize = minChunkSize;
            this.lodArrayLength = lodArrayLength;

            int deepestDepth = GetDeepestDepth();
            this.chunks = new RenderOctreeNode[((1 << ((deepestDepth + 2) * 3)) - 1) / 7];
            this.leafHashes = new bool[chunks.Length];
        }

        private int GetDeepestDepth()
        {
            int deepestDepth = 0;
            int currSize = size;
            while (currSize >= minChunkSize && deepestDepth < lodArrayLength)
            {
                deepestDepth += 1;
                currSize /= 2;
            }
            deepestDepth -= 1;
            return deepestDepth;
        }

        public void Render(Vector3 playerPosition)
        {
            if (chunks[1] != null)
            {
                chunks[1].Render(chunks, playerPosition);
            }
        }

        public void Dispose()
        {
            //terrainSpawnBatch.Dispose();
            Queue<int> updatedChunks = new Queue<int>();
            for (int i = 0; i < chunks.Length; i++)
            {
                if (chunks[i] != null)
                {
                    chunks[i].DisposeTerrainChunk(chunks, leafHashes, updatedChunks);
                }
            }
            //MemoryManager.Instance.Dispose();
        }

        public void MoveWorldCenterHashAndOffsets(List<(int oldHash, int newHash)> updatedHashes, Vector3 newWorldCenter)
        {
            RenderOctreeNode[] newChunks = new RenderOctreeNode[chunks.Length];
            bool[] newLeafHashes = new bool[leafHashes.Length];
            // Set new hashes
            for (int i = 0; i < updatedHashes.Count; i++)
            {
                if (chunks[updatedHashes[i].oldHash] != null)
                {
                    chunks[updatedHashes[i].oldHash].UpdateHash(updatedHashes[i].newHash, chunks, newChunks, newLeafHashes);
                }
            }

            // Set new offsets of first two depths
            newChunks[1] = chunks[1];
            newChunks[1].SetOffset(newWorldCenter - new Vector3(size / 2, size / 2, size / 2));
            for (int i = 0; i < 8; i++)
            {
                int x = i % 2;
                int y = i / 4;
                int z = i == 0 || i == 1 || i == 4 || i == 5 ? 0 : 1;

                chunks[(1 << 3) | i].SetOffset(new Vector3((x - 1), (y - 1), (z - 1)) * (size / 2) + newWorldCenter);
                newChunks[(1 << 3) | i] = chunks[(1 << 3) | i];
            }

            chunks = newChunks;
            leafHashes = newLeafHashes;
        }


        public void UpdateBorders(Queue<int> updatedChunks)
        {
            HashSet<int> visited = new HashSet<int>();

            foreach (int updatedChunkHash in updatedChunks)
            {
                RenderOctreeNode currChunk = chunks[updatedChunkHash];
                if (currChunk != null)
                {
                    int[] adjacentChunks = currChunk.SetBorders(leafHashes); //We first need the borders of the new chunk.

                    Vector3 chunkoffset = currChunk.offset;
                    visited.Add(updatedChunkHash);

                    //Then we update the borders surrounding the new chunk
                    foreach (int adjacentChunkHash in adjacentChunks)
                    {
                        if (adjacentChunkHash != 0)
                        {
                            // It is the same size
                            if (leafHashes[adjacentChunkHash] && !visited.Contains(adjacentChunkHash))
                            {
                                chunks[adjacentChunkHash].SetBorders(leafHashes);
                                visited.Add(adjacentChunkHash);
                            }
                            // It is bigger
                            else if (leafHashes[adjacentChunkHash >> 3] && !visited.Contains(adjacentChunkHash >> 3))
                            {
                                chunks[adjacentChunkHash >> 3].SetBorders(leafHashes);
                                visited.Add(adjacentChunkHash >> 3);
                            }
                            // It is smaller (we only need to check 4 of these, but I just check all 8 since it's easier)
                            else
                            {
                                for (int i = 0; i < 8; i++)
                                {
                                    if (((adjacentChunkHash << 3) | i) < leafHashes.Length && leafHashes[(adjacentChunkHash << 3) | i] && !visited.Contains((adjacentChunkHash << 3) | i))
                                    {
                                        chunks[(adjacentChunkHash << 3) | i].SetBorders(leafHashes);
                                        visited.Add((adjacentChunkHash << 3) | i);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ProcessEvents(IOctreeEvent[] events)
        {
            Queue<int> updatedChunks = new Queue<int>();

            foreach (IOctreeEvent octreeEvent in events) 
            {
                // Process Events
            }

            if (updatedChunks.Count > 0)
            {
                UpdateBorders(updatedChunks);
            }
        }
    }
}
