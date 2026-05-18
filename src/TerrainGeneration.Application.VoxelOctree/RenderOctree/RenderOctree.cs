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
    private System.Collections.Generic.Dictionary<int, ChunkIntentEvent> DeferredChunkIntents = new System.Collections.Generic.Dictionary<int, ChunkIntentEvent>();

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

        DisposeChunksNotMovedToNewCenter(newChunks);
        Chunks = newChunks;
        LeafHashes = newLeafHashes;
    }

    private void DisposeChunksNotMovedToNewCenter(RenderOctreeNode?[] newChunks)
    {
        HashSet<RenderOctreeNode> retainedChunks = newChunks
            .Where(chunk => chunk != null)
            .Select(chunk => chunk!)
            .ToHashSet();

        for (int i = 0; i < Chunks.Length; i++)
        {
            RenderOctreeNode? chunk = Chunks[i];
            if (chunk != null && !retainedChunks.Contains(chunk))
            {
                chunk.Dispose(Chunks, LeafHashes, TransvoxelTerrainGenerator);
            }
        }
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

    public void ProcessEvents(IOctreeEvent[] events)
    {
        Queue<int> updatedChunks = new Queue<int>();
        System.Collections.Generic.Dictionary<int, ChunkIntentEvent> chunkIntents = new System.Collections.Generic.Dictionary<int, ChunkIntentEvent>(DeferredChunkIntents);
        DeferredChunkIntents.Clear();

        foreach (IOctreeEvent octreeEvent in events) 
        {
            if (octreeEvent != null)
            {
                if (octreeEvent is ChunkIntentEvent intentEvent)
                {
                    chunkIntents[intentEvent.Hash] = intentEvent;
                    continue;
                }
            }

        }

        HashSet<int> removedSubtrees = new HashSet<int>();
        foreach (ChunkIntentEvent intent in SortChunkIntents(chunkIntents.Values))
        {
            if (HasAncestorInSet(intent.Hash, removedSubtrees))
            {
                continue;
            }

            if (!ReconcileChunkIntent(intent, updatedChunks))
            {
                DeferredChunkIntents[intent.Hash] = intent;
            }
            else if (intent.State == ChunkIntentState.Missing)
            {
                removedSubtrees.Add(intent.Hash);
            }
        }

        if (updatedChunks.Count > 0)
        {
            UpdateBorders(updatedChunks);
        }
    }

    private IEnumerable<ChunkIntentEvent> SortChunkIntents(IEnumerable<ChunkIntentEvent> intents)
    {
        return intents
            .OrderBy(intent => intent.State == ChunkIntentState.Missing ? 0 : intent.State == ChunkIntentState.Internal ? 1 : 2)
            .ThenBy(intent => intent.State == ChunkIntentState.Missing ? -intent.Depth : intent.Depth)
            .ToArray();
    }

    private bool ReconcileChunkIntent(ChunkIntentEvent intent, Queue<int> updatedChunks)
    {
        if (!IsValidHash(intent.Hash))
        {
            return true;
        }

        switch (intent.State)
        {
            case ChunkIntentState.Missing:
                if (Chunks[intent.Hash] != null)
                {
                    if (!CanSafelyRemoveNode(intent.Hash))
                    {
                        return false;
                    }

                    Chunks[intent.Hash].Dispose(Chunks, LeafHashes, TransvoxelTerrainGenerator);
                    updatedChunks.Enqueue(intent.Hash >> 3);
                }
                return true;

            case ChunkIntentState.Internal:
                if (!HasParent(intent.Hash))
                {
                    return false;
                }

                CreateRenderNode(intent, updatedChunks);
                if (Chunks[intent.Hash] == null)
                {
                    return false;
                }

                if (!ChildrenHaveCompleteCoverage(intent.Hash))
                {
                    Chunks[intent.Hash].SetTerrainChunk(LeafHashes, updatedChunks, TransvoxelTerrainGenerator);
                    return false;
                }

                Chunks[intent.Hash].DisposeTerrainChunk(Chunks, LeafHashes, updatedChunks, TransvoxelTerrainGenerator);
                return true;

            case ChunkIntentState.Leaf:
                if (!HasParent(intent.Hash))
                {
                    return false;
                }

                CreateRenderNode(intent, updatedChunks);
                Chunks[intent.Hash]?.SetTerrainChunk(LeafHashes, updatedChunks, TransvoxelTerrainGenerator);
                return Chunks[intent.Hash] != null && Chunks[intent.Hash].HasTerrain;
        }

        return true;
    }

    private bool ChildrenHaveCompleteCoverage(int hash)
    {
        if (((hash << 3) | 7) >= Chunks.Length)
        {
            return true;
        }

        for (int i = 0; i < 8; i++)
        {
            RenderOctreeNode? child = Chunks[(hash << 3) | i];
            if (child == null || !child.HasCompleteRenderCoverage(Chunks))
            {
                return false;
            }
        }

        return true;
    }

    private bool CanSafelyRemoveNode(int hash)
    {
        if (hash == 1)
        {
            return true;
        }

        int parentHash = hash >> 3;
        if (!IsValidHash(parentHash))
        {
            return true;
        }

        RenderOctreeNode? parent = Chunks[parentHash];
        return parent == null || parent.HasTerrain;
    }

    private bool HasParent(int hash)
    {
        if (hash == 1)
        {
            return true;
        }

        int parentHash = hash >> 3;
        return IsValidHash(parentHash) && Chunks[parentHash] != null;
    }

    private static bool HasAncestorInSet(int hash, HashSet<int> ancestors)
    {
        while (hash > 1)
        {
            hash >>= 3;
            if (ancestors.Contains(hash))
            {
                return true;
            }
        }

        return false;
    }

    private void CreateRenderNode(ChunkIntentEvent intent, Queue<int> updatedChunks)
    {
        if (!IsValidHash(intent.Hash) || Chunks[intent.Hash] != null || intent.Lod == 0)
        {
            return;
        }

        Chunks[intent.Hash] = new RenderOctreeNode(Chunks, LeafHashes, updatedChunks, true, TransvoxelTerrainGenerator, new RenderOctreeDescriptor()
        {
            Depth = intent.Depth,
            Hash = intent.Hash,
            Lod = intent.Lod,
            Offset = intent.Offset,
            Size = intent.Size,
        }, false);
    }

    private bool IsValidHash(int hash)
    {
        return hash > 0 && hash < Chunks.Length;
    }
}
