using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator;

namespace TerrainGeneration.Application.TerrainGenerator;
internal class TerrainMesh
{
    private const uint VERT_DIVISOR = 10;

    public Rid VertexBuffer;
    public Rid VertexBufferUniform;

    public Rid IndirectArgsBuffer;
    public Rid IndirectArgsBufferUniform;

    public TerrainMeshParameters? TerrainMeshParameters;
    public Rid TerrainMeshParamsUniformBuffer;
    public Rid TerrainMeshParamsUniformSet;

    public TerrainMesh(RenderingDevice rd, TerrainMeshParameters parameters)
    {
        uint chunkSizeToLodRatio = parameters.ChunkSize / parameters.Lod;
        
        // 5 is the max number of triangles per cell, 3 is the number of verts per triangle
        uint maxNumInternalVerts = chunkSizeToLodRatio * chunkSizeToLodRatio * chunkSizeToLodRatio * 5 * 3;

        // 12 is the max number of triangles per cell, 3 is the number of verts per triangle, 6 is the number of sides to the chunk (we need 6 different border triangle sets)
        uint maxNumBorderVerts = chunkSizeToLodRatio * chunkSizeToLodRatio * 12 * 6 * 3;

        // Vert divisor is to save on memory since most chunks won't use the max amount
        uint maxNumVerts = (maxNumBorderVerts + maxNumInternalVerts) / VERT_DIVISOR;

        // Each vert has a position and normal, both Vector3
        VertexBuffer = rd.StorageBufferCreate(maxNumVerts * 2 * 3 * sizeof(float));
        RDUniform vertexBufferuniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        vertexBufferuniform.AddId(VertexBuffer);
        VertexBufferUniform = rd.UniformSetCreate([vertexBufferuniform], parameters.TransvoxelShader, 0);

        // Max indirect args buffer
        IndirectArgsBuffer = rd.StorageBufferCreate(4 * sizeof(uint));
        RDUniform indirectArgsUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        indirectArgsUniform.AddId(IndirectArgsBuffer);
        IndirectArgsBufferUniform = rd.UniformSetCreate([indirectArgsUniform], parameters.TransvoxelShader, 1);
    }
}
