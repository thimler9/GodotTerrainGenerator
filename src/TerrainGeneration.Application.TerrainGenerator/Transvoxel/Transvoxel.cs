using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel;
public class Transvoxel
{
    private TransvoxelShader TransvoxelShader;
    private IndirectArgsShader IndirectArgsShader;
    private RenderingDevice Rd;

    // Think we want to keep the buffer pool for the triangles in the shader
    private uint MaxNumTerrainMeshesInQueue;
    private Queue<TerrainMesh> TerrainMeshes;

    public Transvoxel(RenderingDevice rd, TransvoxelDescriptor descriptor)
    {

        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor), "Cannot be null.");
        }

        if (descriptor.TransvoxelShaderDescriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor.TransvoxelShaderDescriptor), "Cannot be null.");
        }

        if (descriptor.IndirectArgsShaderDescriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor.IndirectArgsShaderDescriptor), "Cannot be null.");
        }

        if (rd == null)
        {
            throw new ArgumentNullException(nameof(rd), "Cannot be null.");
        }

        Rd = rd;
        TerrainMeshes = new Queue<TerrainMesh>();
        TransvoxelShader = new TransvoxelShader(Rd, descriptor.TransvoxelShaderDescriptor);
        IndirectArgsShader = new IndirectArgsShader(Rd, descriptor.IndirectArgsShaderDescriptor);
        MaxNumTerrainMeshesInQueue = descriptor.MaxNumTerrainMeshesInQueue;
    }

    public TerrainMesh GetTerrainMesh(TransvoxelShaderParameters parameters, RDUniform sdfUniform, RDUniform normalsUniform)
    {
        // Get the terrain mesh
        TerrainMesh? terrainMesh;
        if (!TerrainMeshes.TryDequeue(out terrainMesh))
        {
            // Create a new one if there aren't any in the queue
            TerrainMeshParameters terrainMeshParameters = new TerrainMeshParameters()
            {
                ChunkSize = parameters.ChunkSize,
                Lod = parameters.Lod,
                MaxNumTriangles = parameters.MaxNumVertices,
            };
            terrainMesh = new TerrainMesh(Rd, terrainMeshParameters);
        }

        if (terrainMesh == null)
        {
            throw new ArgumentNullException(nameof(terrainMesh), "This should not be null");
        }

        // Get vertices
        TransvoxelShader.Dispatch(parameters, sdfUniform, normalsUniform, terrainMesh.VertexBufferUniform);

        // Get indirect args
        IndirectArgsShader.Dispatch(TransvoxelShader.GetCurrentVertexCountUniform(), terrainMesh.IndirectArgsBufferUniform);

        // Get indirect args
        return terrainMesh;
    }

    public void ReturnTerrainMesh(TerrainMesh terrainMesh)
    {
        if (terrainMesh != null)
        {
            if (TerrainMeshes.Count < MaxNumTerrainMeshesInQueue - 1)
            {
                TerrainMeshes.Enqueue(terrainMesh);
            }
            else
            {
                terrainMesh.Dispose();
            }
        }
    }

    public void Dispose()
    {
        foreach (var terrainMesh in TerrainMeshes)
        {
            terrainMesh.Dispose();
        }

        TransvoxelShader.Dispose();
    }
}
