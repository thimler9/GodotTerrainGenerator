using System;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel.CPU;
public class Transvoxel
{
    private const uint EDGE_TABLE_INDEX = 0;
    private const uint EDGE_TO_VERTICES_INDEX = 256;
    private const uint NUM_TRI_INDEX = 280;
    private const uint TRIANGLE_LOOKUP_INDEX = 536;

    [StructLayout(LayoutKind.Sequential)]
    private struct MarchingCubesTriangle
    {
        public Godot.Vector3 Vertex0 { get; set; }
        public Godot.Vector3 Vertex1 { get; set; }
        public Godot.Vector3 Vertex2 { get; set; }
        public MarchingCubesTriangle(Godot.Vector3 vertex0, Godot.Vector3 vertex1, Godot.Vector3 vertex2)
        {
            Vertex0 = vertex0;
            Vertex1 = vertex1;
            Vertex2 = vertex2;
        }
    }


    public Transvoxel()
    {

    }

    /// <summary>
    /// Returns a list of vertices for the given SDF using the Transvoxel algorithm.
    /// </summary>
    /// <param name="transvoxelParameters"></param>
    /// <param name="sdf"></param>
    /// <returns></returns>
    public Godot.Vector3[] GenerateMarchingCubes(TransvoxelParameters transvoxelParameters, float[] sdf, Godot.Vector3? chunkOffset)
    {
        List<MarchingCubesTriangle> triangles = new List<MarchingCubesTriangle>();
        uint lod = transvoxelParameters.Lod;
        uint numCells = transvoxelParameters.ChunkSize / transvoxelParameters.Lod;

        for (int x = 0; x < numCells; x++)
        {
            for (uint y = 0; y < numCells; y++)
            {
                for (uint z = 0; z < numCells; z++)
                {
                    uint tempX = (uint)x * lod;
                    uint tempY = y * lod;
                    uint tempZ = z * lod;

                    uint adjustedSize = transvoxelParameters.ChunkSize / lod + 2; // this is the size of the sdf length

                    Godot.Vector4[] currentVertices = new Godot.Vector4[8]
                    {
                        new Godot.Vector4(tempX, tempY, tempZ, sdf[x + y * adjustedSize + z * adjustedSize * adjustedSize]),
                        new Godot.Vector4(tempX, tempY + lod, tempZ, sdf[x + (y + 1) * adjustedSize + z * adjustedSize * adjustedSize]),
                        new Godot.Vector4(tempX + lod, tempY + lod, tempZ, sdf[(x + 1) + (y + 1) * adjustedSize + z * adjustedSize * adjustedSize]),
                        new Godot.Vector4(tempX + lod, tempY, tempZ, sdf[(x + 1) + y * adjustedSize + z * adjustedSize * adjustedSize]),
                        new Godot.Vector4(tempX, tempY, tempZ + lod, sdf[x + y * adjustedSize + (z + 1) * adjustedSize * adjustedSize]),
                        new Godot.Vector4(tempX, tempY + lod, tempZ + lod, sdf[x + (y + 1) * adjustedSize + (z + 1) * adjustedSize * adjustedSize]),
                        new Godot.Vector4(tempX + lod, tempY + lod, tempZ + lod, sdf[(x + 1) + (y + 1) * adjustedSize + (z + 1) * adjustedSize * adjustedSize]),
                        new Godot.Vector4(tempX + lod, tempY, tempZ + lod, sdf[(x + 1) + y * adjustedSize + (z + 1) * adjustedSize * adjustedSize])
                    };

                    uint edgeIndex = 0;
                    uint edgeCut = 0;
                    Godot.Vector3[] triangleVertices = new Godot.Vector3[12];

                    for (int i = 7; i >= 0; i--)
                    {
                        uint vertexValue = currentVertices[i].W >= 0.0f ? 1u : 0u;
                        edgeIndex |= vertexValue;
                        edgeIndex <<= 1;
                    }
                    edgeIndex >>= 1;
                    edgeCut = (uint)LookupTables.LookupTablesData[EDGE_TABLE_INDEX + edgeIndex];

                    if (edgeCut == 0)
                    {
                        continue;
                    }

                    for (int j = 0; j < 12; j++)
                    {
                        uint firstVoxelIndex = (uint)LookupTables.LookupTablesData[EDGE_TO_VERTICES_INDEX + j * 2];
                        uint secondVoxelIndex = (uint)LookupTables.LookupTablesData[EDGE_TO_VERTICES_INDEX + j * 2 + 1];
                        Godot.Vector4 v0 = currentVertices[firstVoxelIndex];
                        Godot.Vector4 v1 = currentVertices[secondVoxelIndex];
                        float t = v0.W / (v0.W - v1.W);

                        Godot.Vector4 interpolatedVertex = v0.Lerp(v1, t);
                        triangleVertices[j] = new Godot.Vector3(interpolatedVertex.X, interpolatedVertex.Y, interpolatedVertex.Z);
                    }

                    for (int i = 0; i < LookupTables.LookupTablesData[NUM_TRI_INDEX + edgeIndex]; i++)
                    {
                        int index0 = LookupTables.LookupTablesData[TRIANGLE_LOOKUP_INDEX + edgeIndex * 16 + i * 3 + 2];
                        int index1 = LookupTables.LookupTablesData[TRIANGLE_LOOKUP_INDEX + edgeIndex * 16 + i * 3 + 1];
                        int index2 = LookupTables.LookupTablesData[TRIANGLE_LOOKUP_INDEX + edgeIndex * 16 + i * 3 ];

                        Godot.Vector3 vertex0 = triangleVertices[index0] + (chunkOffset == null ? Godot.Vector3.Zero : chunkOffset.Value);
                        Godot.Vector3 vertex1 = triangleVertices[index1] + (chunkOffset == null ? Godot.Vector3.Zero : chunkOffset.Value);
                        Godot.Vector3 vertex2 = triangleVertices[index2] + (chunkOffset == null ? Godot.Vector3.Zero : chunkOffset.Value);

                        triangles.Add(new MarchingCubesTriangle(vertex0, vertex1, vertex2));
                    }

                }
            }
        }

        MarchingCubesTriangle[] triangleArray = triangles.ToArray();
        Godot.Vector3[] output = MemoryMarshal.Cast<MarchingCubesTriangle, Godot.Vector3>(triangleArray.AsSpan()).ToArray();

        return output;
    }


}
