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

    private TransvoxelDescriptor Descriptor;

    /// <summary>
    /// Creates an instance of the transvoxel algorithm processor. It has access to getting a terrain mesh
    /// using the transvoxel algorithm.
    /// </summary>
    /// <param name="rd"></param>
    /// <param name="descriptor"></param>
    /// <exception cref="ArgumentNullException"></exception>
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
        Descriptor = descriptor;
    }

    /// <summary>
    /// Gets the TerrainMesh for the given sdf uniform and normals uniforms. NOTE, this sets the vertices in the terrain mesh
    /// but does not create the uniform set that is needed for the shader.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="sdfUniform"></param>
    /// <param name="normalsUniform"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public TerrainMesh GetTerrainMesh(TransvoxelShaderParameters parameters, RDUniform sdfUniform, RDUniform normalsUniform)
    {
        // Get the terrain mesh
        TerrainMesh? terrainMesh;
        if (!TerrainMeshes.TryDequeue(out terrainMesh))
        {
            if (Descriptor.TransvoxelShaderDescriptor == null)
            {
                throw new ArgumentNullException($"{nameof(Descriptor.TransvoxelShaderDescriptor)} does not exist.");
            }

            // Create a new one if there aren't any in the queue
            TerrainMeshDescriptor terrainMeshParameters = new TerrainMeshDescriptor()
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

        if (sdfUniform == null)
        {
            throw new ArgumentNullException($"{nameof(sdfUniform)} cannot be null.");
        }

        if (normalsUniform == null)
        {
            throw new ArgumentNullException($"{nameof(normalsUniform)} must be null.");
        }

        // Set vertices
        TransvoxelShader.Dispatch(parameters, sdfUniform, normalsUniform, terrainMesh.VertexBufferUniform);

        // Set indirect args
        IndirectArgsShader.Dispatch(TransvoxelShader.GetCurrentVertexCountUniform(), terrainMesh.IndirectArgsBufferUniform);

        // Get indirect args
        return terrainMesh;
    }

    /// <summary>
    /// Returns a terrain mesh to the terrain mesh queue. If there is not space in the queue, the memory gets freed.
    /// </summary>
    /// <param name="terrainMesh"></param>
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
        else
        {
            GD.PrintErr("Tried to return terrain mesh when it was null");
        }
    }

    /// <summary>
    /// Disposes of all of the resources associated with the transvoxel instance. This includes all
    /// terrain meshes and the transvoxel shader resources.
    /// </summary>
    public void Dispose()
    {
        foreach (var terrainMesh in TerrainMeshes)
        {
            terrainMesh.Dispose();
        }

        TransvoxelShader.Dispose();
    }
}
