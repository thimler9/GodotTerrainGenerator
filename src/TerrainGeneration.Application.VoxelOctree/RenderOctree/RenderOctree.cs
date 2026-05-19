using Godot;
using Godot.Collections;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEvent;
using TerrainGeneration.Application.VoxelOctree.Abstractions.RenderOctree;
using TerrainGeneration.Application.VoxelOctree.OctreeEvents;

namespace TerrainGeneration.Application.VoxelOctree.RenderOctree;


internal class RenderOctree : IRenderOctree
{
    private uint Size;
    private uint MinChunkSize;
    private int LodArrayLength;
    private float BorderWidth;

    public RenderOctreeNode[] Chunks;
    public bool[] LeafHashes;
    private System.Collections.Generic.Dictionary<int, ChunkStateEvent> DeferredChunkIntents = new System.Collections.Generic.Dictionary<int, ChunkStateEvent>();

    // How we get the triangle for the terrain meshes
    private TransvoxelTerrainGenerator TransvoxelTerrainGenerator;

    // Shader constants for rendering mesh


    public RenderOctree(uint size, uint minChunkSize, int lodArrayLength, float borderWidth, TransvoxelTerrainGenerator transvoxelTerrainGenerator)
    {
        Size = size;
        MinChunkSize = minChunkSize;
        LodArrayLength = lodArrayLength;
        BorderWidth = borderWidth;

        int deepestDepth = GetDeepestDepth();
        Chunks = new RenderOctreeNode[((1 << ((deepestDepth + 2) * 3)) - 1) / 7];
        LeafHashes = new bool[Chunks.Length];
        TransvoxelTerrainGenerator = transvoxelTerrainGenerator;
    }

    private int GetDeepestDepth()
    {
        int deepestDepth = 0;
        uint currSize = Size;
        while (currSize >= MinChunkSize && deepestDepth < LodArrayLength)
        {
            deepestDepth += 1;
            currSize /= 2;
        }
        deepestDepth -= 1;
        return deepestDepth;
    }

    public void Render(Array<Plane> frustumPlanes, TerrainMeshRenderDescriptor terrainMeshRenderDescriptor)
    {
        if (Chunks[1] != null)
        {
            Chunks[1].Render(Chunks, terrainMeshRenderDescriptor, frustumPlanes);
        }
    }

    public void Dispose()
    {
        //terrainSpawnBatch.Dispose();
        Queue<int> updatedChunks = new Queue<int>();
        for (int i = 0; i < Chunks.Length; i++)
        {
            if (Chunks[i] != null)
            {
                Chunks[i].DisposeTerrainChunk(Chunks, LeafHashes, updatedChunks, TransvoxelTerrainGenerator);
            }
        }
        TransvoxelTerrainGenerator.Dispose();
    }

    public void MoveWorldCenterHashAndOffsets(List<(int oldHash, int newHash)> updatedHashes, Vector3 newWorldCenter)
    {
        RenderOctreeNode[] newChunks = new RenderOctreeNode[Chunks.Length];
        bool[] newLeafHashes = new bool[LeafHashes.Length];
        // Set new hashes
        for (int i = 0; i < updatedHashes.Count; i++)
        {
            if (Chunks[updatedHashes[i].oldHash] != null)
            {
                Chunks[updatedHashes[i].oldHash].UpdateHash(updatedHashes[i].newHash, Chunks, newChunks, newLeafHashes);
            }
        }

        // Set new offsets of first two depths
        newChunks[1] = Chunks[1];
        newChunks[1].SetOffset(newWorldCenter - new Vector3(Size / 2, Size / 2, Size / 2));
        for (int i = 0; i < 8; i++)
        {
            int x = i % 2;
            int y = i / 4;
            int z = i == 0 || i == 1 || i == 4 || i == 5 ? 0 : 1;

            Chunks[(1 << 3) | i].SetOffset(new Vector3((x - 1), (y - 1), (z - 1)) * (Size / 2) + newWorldCenter);
            newChunks[(1 << 3) | i] = Chunks[(1 << 3) | i];
        }

        Chunks = newChunks;
        LeafHashes = newLeafHashes;
    }


    public void UpdateBorders(Queue<int> updatedChunks)
    {
        HashSet<int> visited = new HashSet<int>();

        foreach (int updatedChunkHash in updatedChunks)
        {
            RenderOctreeNode currChunk = Chunks[updatedChunkHash];
            if (currChunk != null)
            {
                int[] adjacentChunks = currChunk.SetBorders(LeafHashes); //We first need the borders of the new chunk.

                Vector3 chunkoffset = currChunk.Offset;
                visited.Add(updatedChunkHash);

                //Then we update the borders surrounding the new chunk
                foreach (int adjacentChunkHash in adjacentChunks)
                {
                    if (adjacentChunkHash != 0)
                    {
                        // It is the same size
                        if (LeafHashes[adjacentChunkHash] && !visited.Contains(adjacentChunkHash))
                        {
                            Chunks[adjacentChunkHash].SetBorders(LeafHashes);
                            visited.Add(adjacentChunkHash);
                        }
                        // It is bigger
                        else if (LeafHashes[adjacentChunkHash >> 3] && !visited.Contains(adjacentChunkHash >> 3))
                        {
                            Chunks[adjacentChunkHash >> 3].SetBorders(LeafHashes);
                            visited.Add(adjacentChunkHash >> 3);
                        }
                        // It is smaller (we only need to check 4 of these, but I just check all 8 since it's easier)
                        else
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                if (((adjacentChunkHash << 3) | i) < LeafHashes.Length && LeafHashes[(adjacentChunkHash << 3) | i] && !visited.Contains((adjacentChunkHash << 3) | i))
                                {
                                    Chunks[(adjacentChunkHash << 3) | i].SetBorders(LeafHashes);
                                    visited.Add((adjacentChunkHash << 3) | i);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public void ProcessEvents(OctreeEvent[] events)
    {
        Queue<int> updatedChunks = new Queue<int>();

        foreach (var octreeEvent in events)
        {
            if (octreeEvent is ChunkStateEvent stateEvent)
            {
                ProcessChunkStateEvent(stateEvent, updatedChunks);
            }
        }

        if (updatedChunks.Count > 0)
        {
            UpdateBorders(updatedChunks);
        }
    }

    private void ProcessChunkStateEvent(ChunkStateEvent stateEvent, Queue<int> updatedChunks)
    {
        if (!IsValidHash(stateEvent.Hash))
        {
            return;
        }

        switch (stateEvent.State)
        {
            case ChunkState.Missing:
                // Leaf -> Missing, Internal -> Missing, dispose of terrain, remove it and children from tree
                if (Chunks[stateEvent.Hash] != null)
                {
                    Chunks[stateEvent.Hash].Dispose(Chunks, LeafHashes, TransvoxelTerrainGenerator);
                }
                break;
            case ChunkState.Internal:
                // Leaf -> Internal, dispose terrain, keep in tree
                if (Chunks[stateEvent.Hash] != null)
                {
                    Chunks[stateEvent.Hash].DisposeTerrainChunk(Chunks, LeafHashes, updatedChunks, TransvoxelTerrainGenerator);
                }
                // Missing -> Internal, add to tree, don't create terrain
                else
                {
                    RenderOctreeNodeDescriptor descriptor = new RenderOctreeNodeDescriptor()
                    {
                        Hash = stateEvent.Hash,
                        Depth = stateEvent.Depth,
                        Offset = stateEvent.Offset,
                        Lod = stateEvent.Lod,
                        Size = stateEvent.Size,
                    };
                    Chunks[stateEvent.Hash] = new RenderOctreeNode(Chunks, LeafHashes, updatedChunks, true, TransvoxelTerrainGenerator, descriptor, false);
                }
                break;
            case ChunkState.Leaf:
                // Missing -> Leaf, add to tree and create terrain
                if (Chunks[stateEvent.Hash] == null)
                {
                    RenderOctreeNodeDescriptor descriptor = new RenderOctreeNodeDescriptor()
                    {
                        Hash = stateEvent.Hash,
                        Depth = stateEvent.Depth,
                        Offset = stateEvent.Offset,
                        Lod = stateEvent.Lod,
                        Size = stateEvent.Size,
                    };
                    Chunks[stateEvent.Hash] = new RenderOctreeNode(Chunks, LeafHashes, updatedChunks, true, TransvoxelTerrainGenerator, descriptor);

                    // We can dispose of the parent's terrain if this is the last child to be filled
                    if (AllChildrenFilled(stateEvent.Hash >> 3))
                    {
                        Chunks[stateEvent.Hash >> 3]?.DisposeTerrainChunk(Chunks, LeafHashes, updatedChunks, TransvoxelTerrainGenerator);
                    }
                }
                // Internal -> Leaf, create terrain
                else
                {
                    Chunks[stateEvent.Hash].SetTerrainChunk(LeafHashes, updatedChunks, TransvoxelTerrainGenerator);
                }
                break;

        }
    }

    private bool IsValidHash(int hash)
    {
        return hash > 0 && hash < Chunks.Length;
    }

    private bool AllChildrenFilled(int hash)
    {
        for (int i = 0; i < 8; i++)
        {
            if (Chunks[(hash << 3) | i] == null)
            {
                return false;
            }
        }
        return true;
    }
}
