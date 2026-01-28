using Godot;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;
using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;
using TerrainGeneration.Utilities.Math.Extensions;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Application.TerrainGenerator;
public class TerrainMesh
{
    private const uint VERT_DIVISOR = 1;

    private RenderingDevice Rd;

    public Rid VertexBuffer;
    public RDUniform VertexBufferUniform;
    public Rid VertexBufferUniformSet;

    public Rid IndirectArgsBuffer;
    public RDUniform IndirectArgsBufferUniform;

    private TerrainMeshParameters? DrawParameters;
    private Rid TerrainMeshParamsBuffer;
    public RDUniform TerrainMeshParamsUniform;
    public Rid TerrainMeshParamsUniformSet;

    private TerrainMeshDescriptor Descriptor;

    /// <summary>
    /// Creates an instance of the Terrain Mesh. This initializes the buffers and uniforms needed for the terrain mesh.
    /// It does not create the triangles or indirect args. The terrain mesh should be used in a triangle generated algorithm
    /// like transvoxel or surface nets (not implemented).
    /// </summary>
    /// <param name="rd"></param>
    /// <param name="descriptor"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public TerrainMesh(RenderingDevice rd, TerrainMeshDescriptor descriptor)
    {
        if (!descriptor.ChunkSize.IsPowerOfTwo())
        {
            throw new ArgumentException($"{nameof(descriptor.ChunkSize)} must be a power of two."); 
        }

        if (descriptor.ChunkSize / 8 == 0)
        {
            throw new ArgumentException($"{nameof(descriptor.ChunkSize)} must be greater than 8.");
        }

        if (rd == null)
        {
            throw new ArgumentNullException($"{nameof(rd)} cannot be null.");
        }

        Rd = rd;
        Descriptor = descriptor;

        uint maxNumVerts = GetMaxNumVerts();

        // Each vert has a position and normal, both Vector3
        VertexBuffer = rd.StorageBufferCreate(maxNumVerts * (uint)Marshal.SizeOf<TerrainMeshVertex>());
        VertexBufferUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        VertexBufferUniform.AddId(VertexBuffer);

        // Max indirect args buffer
        IndirectArgsBuffer = rd.StorageBufferCreate(sizeof(uint) * 4, usage: RenderingDevice.StorageBufferUsage.Indirect);
        rd.BufferClear(IndirectArgsBuffer, 0, sizeof(uint) * 4);
        IndirectArgsBufferUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        IndirectArgsBufferUniform.AddId(IndirectArgsBuffer);

        TerrainMeshParamsBuffer = rd.UniformBufferCreate((uint)Marshal.SizeOf<TerrainMeshParameters>());
        TerrainMeshParamsUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        TerrainMeshParamsUniform.AddId(TerrainMeshParamsBuffer);
    }

    /// <summary>
    /// Gets the max possible amount of verts that can be in the vertex array. This assumes every cell
    /// has the max number of verts and divides by a heuristic VERT_DIVISOR to save on space.
    /// </summary>
    /// <returns></returns>
    private uint GetMaxNumVerts()
    {
        uint chunkSizeToLodRatio = Descriptor.ChunkSize / Descriptor.Lod;

        // 5 is the max number of triangles per cell, 3 is the number of verts per triangle
        uint maxNumInternalVerts = chunkSizeToLodRatio * chunkSizeToLodRatio * chunkSizeToLodRatio * 5 * 3;

        // 12 is the max number of triangles per cell, 3 is the number of verts per triangle, 6 is the number of sides to the chunk (we need 6 different border triangle sets)
        uint maxNumBorderVerts = chunkSizeToLodRatio * chunkSizeToLodRatio * 12 * 6 * 3;

        // Vert divisor is to save on memory since most chunks won't use the max amount
        uint maxNumVerts = Math.Min((maxNumBorderVerts + maxNumInternalVerts) / VERT_DIVISOR, Descriptor.MaxNumTriangles);

        return maxNumVerts;
    }

    /// <summary>
    /// Prints all vertices in the vertex buffer. Use only for debug purposes; requires CPU readback.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public void PrintVertices()
    {
        if (Rd == null)
        {
            throw new ArgumentNullException(nameof(Rd), "Cannot be null");
        }

        var outputBytes = Rd.BufferGetData(VertexBuffer);
 
        float[] output = new float[GetMaxNumVerts() * (uint)Marshal.SizeOf<TerrainMeshVertex>() / sizeof(float)];

        Buffer.BlockCopy(outputBytes, 0, output, 0, output.Length * sizeof(float));

        TerrainMeshVertex[] outputVertices = new TerrainMeshVertex[output.Length / 8];

        string outputString = "";
        for (int i = 0; i < output.Length / (8 * 3); i++)
        {
            int index = i * 3;
            TerrainMeshVertex vert1 = new TerrainMeshVertex(
                new Vector4(output[index * 8], output[index * 8 + 1], output[index * 8 + 2], output[index * 8 + 3]),
                new Vector4(output[index * 8 + 4], output[index * 8 + 5], output[index * 8 + 6], output[index * 8 + 7])
            );
            outputVertices[index] = vert1;

            index++;
            TerrainMeshVertex vert2 = new TerrainMeshVertex(
                new Vector4(output[index * 8], output[index * 8 + 1], output[index * 8 + 2], output[index * 8 + 3]),
                new Vector4(output[index * 8 + 4], output[index * 8 + 5], output[index * 8 + 6], output[index * 8 + 7])
            );
            outputVertices[index] = vert2;

            index++;
            TerrainMeshVertex vert3 = new TerrainMeshVertex(
                new Vector4(output[index * 8], output[index * 8 + 1], output[index * 8 + 2], output[index * 8 + 3]),
                new Vector4(output[index * 8 + 4], output[index * 8 + 5], output[index * 8 + 6], output[index * 8 + 7])
            );
            outputVertices[index] = vert3;

            outputString += $"\n\t{vert1.ToString()}\t{vert2.ToString()}\t{vert3.ToString()}";
        }

        GD.Print("Output count: " + output.Length / 8);
        GD.Print("Output: ", outputString);
    }

    /// <summary>
    /// Prints the indirect args buffer. Use only for debug purposes; requires CPU readback.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public void PrintIndirectArgs()
    {
        if (Rd == null)
        {
            throw new ArgumentNullException(nameof(Rd), "Cannot be null");
        }

        var outputBytes = Rd.BufferGetData(IndirectArgsBuffer);

        uint[] output = new uint[4];
        Buffer.BlockCopy(outputBytes, 0, output, 0, output.Length * sizeof(uint));
        GD.Print("Output: ", string.Join(", ", output));
    }

    /// <summary>
    /// Frees all memory associated with this Terrain Mesh
    /// </summary>
    public void Dispose()
    {
        Rd.FreeRid(IndirectArgsBuffer);
        Rd.FreeRid(VertexBuffer);
        Rd.FreeRid(TerrainMeshParamsBuffer);
        Rd.FreeRid(VertexBufferUniformSet);
        Rd.FreeRid(TerrainMeshParamsUniformSet);
    }

    /// <summary>
    /// Clears the indirect args buffer if need be.
    /// </summary>
    public void ResetBuffers()
    {
        Rd.BufferClear(IndirectArgsBuffer, 0, sizeof(uint) * 4);
    }

    /// <summary>
    /// Sets the TerrainMeshParams args for rendering. Minimize usage, this does CPU -> GPU
    /// </summary>
    /// <param name="parameters"></param>
    /// <exception cref="ArgumentException"></exception>
    public void SetParamsBuffer(TerrainMeshParameters parameters)
    {
        if (!TerrainMeshParamsBuffer.IsValid)
        {
            throw new ArgumentException($"{nameof(TerrainMeshParamsBuffer)} is not valid.");
        }

        // The two are the same, don't replace
        if (DrawParameters != null && DrawParameters.Equals(parameters))
        {
            return;
        }

        byte[] parameterBytes = StructHelpers.ToByteArray(parameters);
        Rd.BufferUpdate(TerrainMeshParamsBuffer, 0, (uint)parameterBytes.Length, parameterBytes);
        DrawParameters = parameters;
    }

    /// <summary>
    /// Sets the uniform set for the terrain mesh parameters
    /// </summary>
    /// <param name="shader">The shader that uses the TerrainMeshParameters. This expects the TerrainMeshParameters to be at set = 2</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public void SetTerrainMeshParametersUniformSet(Rid shader)
    {
        if (!shader.IsValid)
        {
            throw new ArgumentNullException($"{shader} is not valid. Must pass valid shader.");
        }

        if (DrawParameters == null)
        {
            throw new ArgumentException($"Must set {DrawParameters} before trying to get uniform set.");
        }

        TerrainMeshParamsUniformSet = Rd.UniformSetCreate([TerrainMeshParamsUniform], shader, 2);
    }

    /// <summary>
    /// Sets the uniform set for the vertices
    /// </summary>
    /// <param name="shader">The shader that uses the Vertices. This expects the vertex buffer to be at set = 1</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void SetVertexUniformSet(Rid shader)
    {
        if (!shader.IsValid)
        {
            throw new ArgumentNullException($"{shader} is not valid. Must pass valid shader.");
        }

        VertexBufferUniformSet = Rd.UniformSetCreate([VertexBufferUniform], shader, 1);
    }



    /// <summary>
    /// Renders the terrain mesh
    /// </summary>
    /// <param name="renderDescriptor"></param>
    /// <exception cref="ArgumentException"></exception>
    public void Render(TerrainMeshRenderDescriptor renderDescriptor)
    {
        if (renderDescriptor.ClearColors.Length == 0)
        {
            throw new ArgumentException($"{nameof(renderDescriptor.ClearColors)} must have an element");
        }

        if (!renderDescriptor.ScreenBuffer.IsValid)
        {
            throw new ArgumentException($"{nameof(renderDescriptor.ScreenBuffer)} must be valid");
        }

        if (!renderDescriptor.EmptyVertexArray.IsValid)
        {
            throw new ArgumentException($"{nameof(renderDescriptor.EmptyVertexArray)} must be valid");
        }

        if (!renderDescriptor.RenderPipeline.IsValid)
        {
            throw new ArgumentException($"{nameof(renderDescriptor.RenderPipeline)} must be valid");
        }

        if (!renderDescriptor.RenderSceneDataUniformSet.IsValid)
        {
            throw new ArgumentException($"{nameof(renderDescriptor.RenderSceneDataUniformSet)} must be valid");
        }

        if (!renderDescriptor.Shader.IsValid)
        {
            throw new ArgumentException($"{nameof(renderDescriptor.Shader)} must be valid");
        }

        SetTerrainMeshParametersUniformSet(renderDescriptor.Shader);
        SetVertexUniformSet(renderDescriptor.Shader);

        // Setup draw call
        long drawList = Rd.DrawListBegin(renderDescriptor.ScreenBuffer, RenderingDevice.DrawFlags.IgnoreColorAll, renderDescriptor.ClearColors);
        Rd.DrawCommandBeginLabel("Draw Terrain", new Color(0.0f, 0.0f, 0.0f, 0.0f));
        Rd.DrawListBindRenderPipeline(drawList, renderDescriptor.RenderPipeline);
        Rd.DrawListBindVertexArray(drawList, renderDescriptor.EmptyVertexArray); // The rendering call requires some vertex array, but we don't need it, so we pass an empty one

        // Set the buffers for drawing
        Rd.DrawListBindUniformSet(drawList, renderDescriptor.RenderSceneDataUniformSet, 0);
        Rd.DrawListBindUniformSet(drawList, VertexBufferUniformSet, 1);
        Rd.DrawListBindUniformSet(drawList, TerrainMeshParamsUniformSet, 2);

        // Draw call
        Rd.DrawListDrawIndirect(drawList, false, IndirectArgsBuffer);
        Rd.DrawListEnd();
        Rd.DrawCommandEndLabel();

        Rd.FreeRid(VertexBufferUniformSet);
        Rd.FreeRid(TerrainMeshParamsUniformSet);
    }
}
