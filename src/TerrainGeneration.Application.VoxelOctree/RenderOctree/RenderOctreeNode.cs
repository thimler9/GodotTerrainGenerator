using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;
using TerrainGeneration.Utilities;

namespace TerrainGeneration.Application.VoxelOctree.RenderOctree
{
    public class RenderOctreeNode
    {
        private TerrainChunk? TerrainChunk;
        public Vector3 Offset;
        private uint Size;
        private uint Lod;
        private int Depth;
        private int Hash;

        private Aabb Bounds;


        public RenderOctreeNode(RenderOctreeNode?[] chunks, bool[] leafHashes, Queue<int> updatedChunks, bool updateBorders, TransvoxelTerrainGenerator transvoxelTerrainGenerator, RenderOctreeDescriptor descriptor)
        {
            Offset = descriptor.Offset;
            Size = descriptor.Size;
            Lod = descriptor.Lod;
            Depth = descriptor.Depth;
            Hash = descriptor.Hash;
            Bounds = new Aabb(descriptor.Offset, Vector3.One * descriptor.Size);

            // Create chunk mesh
            TerrainChunkDescriptor terrainChunkDescriptor = new TerrainChunkDescriptor()
            {
                BorderWidth = descriptor.BorderWidth,
                ChunkOffset = descriptor.Offset,
                Depth = descriptor.Depth,
                ChunkSize = descriptor.Size,
                Lod = descriptor.Lod,
                ExpandBorders = 0,
                RetractBorders = 0,
            };
            TerrainChunk = new TerrainChunk(transvoxelTerrainGenerator, terrainChunkDescriptor);

            // Only used for chunks  that get made when shifting the world center
            if (updateBorders)
            {
                updatedChunks.Enqueue(Hash);
            }
        }

        // Removes terrain chunk
        public void DisposeTerrainChunk(RenderOctreeNode?[] chunks, bool[] leafHashes, Queue<int> updatedChunks, TransvoxelTerrainGenerator transvoxelTerrainGenerator)
        {
            // If it has children, set the borders of the children
            if (((Hash << 3) | 7) < chunks.Length /* && terrainChunk != null*/)
            {
                for (int i = 0; i < 8; i++)
                {
                    updatedChunks.Enqueue((Hash << 3) | i);
                }
            }

            leafHashes[Hash] = false;

            if (TerrainChunk != null)
            {
                transvoxelTerrainGenerator.ReturnTerrainMesh(TerrainChunk.TerrainMesh);
                TerrainChunk = null;
            }
        }

        // Removes from tree
        public void Dispose(RenderOctreeNode?[] chunks, bool[] leafHashes, TransvoxelTerrainGenerator transvoxelTerrainGenerator)
        {

            //if (terrainChunk == null)
            //    throw new Exception("Tried removing chunk" + hash + "from tree but it was missing a terrain chunk");

            if (TerrainChunk == null)
            {
                throw new Exception("Tried removing chunk" + Hash + "from tree but it was missing a terrain chunk");
            }

            leafHashes[Hash] = false; // No longer a leaf
            transvoxelTerrainGenerator.ReturnTerrainMesh(TerrainChunk.TerrainMesh); // Remove render data
            TerrainChunk = null;
            chunks[Hash] = null; // No longer in tree
        }

        public void Render(RenderOctreeNode[] chunks, TerrainMeshRenderDescriptor terrainMeshRenderDescriptor, /*TerrainSpawnBatch terrainSpawnBatch,*/ Array<Plane> frustumPlanes)
        {
            // If there's no chunk, but the frustum planes intersectbounds go deeper in the tree
            if (Bounds.IsWithinFrustumPlanes(frustumPlanes))
            {
                if (TerrainChunk != null)
                {
                    TerrainChunk.Render(terrainMeshRenderDescriptor);
                }
                else
                {
                    for (int i = 0; i < 1; i++)
                    {
                        RenderOctreeNode child = chunks[(Hash << 3) | i];
                        if (child != null)
                        {
                            child.Render(chunks, terrainMeshRenderDescriptor, frustumPlanes);
                        }
                    }
                }
            }
        }

        public void SetTerrainChunk(bool[] leafHashes, Queue<int> updatedChunks)
        {
            //if (terrainChunk != null)
            //    throw new ArgumentException("Tried creating a terrain chunk, but it already exists, hash: " + hash);

            //terrainChunk = new TerrainChunk(offset, size, lod, depth);
            leafHashes[Hash] = true;
            updatedChunks.Enqueue(Hash);
        }

        public int[] SetBorders(bool[] leafHashes)
        {
            // We try to get the x, y, z coordinate in the world cube that's subdivided by
            // this chunks lod. To do that, we start at the maximal coordinate for each 
            // axis. directions[0] is the x value, directions[1] is the y value, directions[2]
            // is the z value. We then subtract by powers of 8 until we get to the actual coordinate.
            // These are used to find east, north, and top sides of the chunk.
            int depth = Depth;
            int[] directions = new int[3] { (1 << depth) - 1, (1 << depth) - 1, (1 << depth) - 1 }; // x, y, z coordinates in world cube
            int tempHash = Hash;

            // Finds the coordinates
            for (int i = 0; i < depth; i++)
            {
                int currNum = tempHash & 0x7; // Get right three bits

                // We only move in this direction if the chunk is in the correct
                // position in its parents 8 children. So if its hash is divisible by
                // 2 we know that the chunk is on the left side of the chunk so we subtract
                // the x position to account for that.
                int xMult = currNum % 2 == 0 ? 1 : 0;
                int yMult = currNum / 4 == 0 ? 1 : 0;
                int zMult = currNum == 0 || currNum == 1 || currNum == 4 || currNum == 5 ? 1 : 0;

                directions[0] -= (1 << i) * xMult;
                directions[1] -= (1 << i) * yMult;
                directions[2] -= (1 << i) * zMult;

                tempHash >>= 3;
            }

            int[] adjacentChunkHashes = new int[6] { 0, 0, 0, 0, 0, 0 };

            int hash_x = CoordinateToHashLookup.coordinateToHashLookup[directions[0]];
            int hash_y = 4 * CoordinateToHashLookup.coordinateToHashLookup[directions[1]];
            int hash_z = 2 * CoordinateToHashLookup.coordinateToHashLookup[directions[2]];

            if (directions[0] < (1 << depth) - 1)
            {
                int hash_xp1 = CoordinateToHashLookup.coordinateToHashLookup[directions[0] + 1];
                adjacentChunkHashes[0] = hash_xp1 + hash_y + hash_z + (1 << (3 * depth));
            }

            if (directions[0] > 0)
            {
                int hash_xm1 = CoordinateToHashLookup.coordinateToHashLookup[directions[0] - 1];
                adjacentChunkHashes[1] = hash_xm1 + hash_y + hash_z + (1 << (3 * depth));
            }

            if (directions[1] < (1 << depth) - 1)
            {
                int hash_yp1 = 4 * CoordinateToHashLookup.coordinateToHashLookup[directions[1] + 1];
                adjacentChunkHashes[4] = hash_x + hash_yp1 + hash_z + (1 << (3 * depth));
            }

            if (directions[1] > 0)
            {
                int hash_ym1 = 4 * CoordinateToHashLookup.coordinateToHashLookup[directions[1] - 1];
                adjacentChunkHashes[5] = hash_x + hash_ym1 + hash_z + (1 << (3 * depth));
            }

            if (directions[2] < (1 << depth) - 1)
            {
                int hash_zp1 = 2 * CoordinateToHashLookup.coordinateToHashLookup[directions[2] + 1];
                adjacentChunkHashes[2] = hash_x + hash_y + hash_zp1 + (1 << (3 * depth));
            }

            if (directions[2] > 0)
            {
                int hash_zm1 = 2 * CoordinateToHashLookup.coordinateToHashLookup[directions[2] - 1];
                adjacentChunkHashes[3] = hash_x + hash_y + hash_zm1 + (1 << (3 * depth));
            }

            //If the current chunk's hash is in the chunks dictionary, then it's the same LOD
            //If that chunk doesn't exist and its parent doesn't exist in the dictionary, then it must be a higher lod
            //If the parent exists, then the adjacent chunk is a lower lod

            //For each of the six faces, a bit represents whether or not that face is a border or not: 
            //The bits are represented by EWNSTB: Top Bottom North South East West, so a border set to
            //001110 mean that the top, north, and south faces are touching a chunk with higher level of detail
            int expandBorders = 0x10;
            int retractBorders = 0x10;
            foreach (int adjacentHash in adjacentChunkHashes)
            {
                if (adjacentHash == 0)
                {
                    expandBorders <<= 1;
                    retractBorders <<= 1;
                    continue;
                }
                //If this is true, it must be a higher level of detail
                if (!leafHashes[adjacentHash] && !leafHashes[adjacentHash >> 3])
                {
                    retractBorders |= 1;
                }
                //If this is true, it must be a lower level of detail
                if (leafHashes[adjacentHash >> 3])
                {
                    expandBorders |= 1;
                }

                expandBorders <<= 1;
                retractBorders <<= 1;
            }

            //The last iteration of the loop shifts it an extra amount
            expandBorders >>= 1;
            retractBorders >>= 1;

            // Finally set the borders for the mesh
            if (TerrainChunk != null)
            {
                TerrainChunk.SetTerrainMeshBorders(retractBorders, expandBorders);
            }

            return adjacentChunkHashes;
        }


        public void UpdateHash(int newHash, RenderOctreeNode?[] oldChunks, RenderOctreeNode?[] newChunks, bool[] newLeafHashes)
        {
            // If it has children
            if (TerrainChunk == null)
            {
                for (int i = 0; i < 8; i++)
                {
                    // There's a chance when the world is moved that there were create events that got disposed of resulting in the chunks to be null
                    if (oldChunks[(Hash << 3) | i] != null)
                    {
                        oldChunks[(Hash << 3) | i].UpdateHash((newHash << 3) | i, oldChunks, newChunks, newLeafHashes);
                    }
                }
            }
            else
            {
                newLeafHashes[newHash] = true;
            }

            Hash = newHash;
            newChunks[Hash] = this;
        }

        public void SetOffset(Vector3 offset)
        {
            Offset = offset;
            Bounds = new Aabb(offset, Vector3.One * Size);
        }
    }
}
