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

    public void Dispose()
    {
        Rd.FreeRid(IndirectArgsBuffer);
        Rd.FreeRid(VertexBuffer);
        Rd.FreeRid(TerrainMeshParamsBuffer);
        Rd.FreeRid(VertexBufferUniformSet);
        Rd.FreeRid(TerrainMeshParamsUniformSet);
    }

    public void ResetBuffers()
    {
        Rd.BufferClear(IndirectArgsBuffer, 0, sizeof(uint) * 4);
    }

    private RDUniform TryGetTerrainMeshParamsUniform()
    {
        if (DrawParameters == null)
        {
            throw new ArgumentNullException($"{nameof(DrawParameters)} cannot be null, call {nameof(SetParamsBuffer)} before calling this function");
        }

        if (!TerrainMeshParamsBuffer.IsValid)
        {
            throw new ArgumentException($"{nameof(TerrainMeshParamsBuffer)} is not valid.");
        }

        return TerrainMeshParamsUniform;
    }

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

    public void SetTerrainMeshParametersUniformSet(Rid shader, uint shaderSet)
    {
        if (!shader.IsValid)
        {
            throw new ArgumentNullException($"{shader} is not valid. Must pass valid shader.");
        }

        if (DrawParameters == null)
        {
            throw new ArgumentException($"Must set {DrawParameters} before trying to get uniform set.");
        }

        TerrainMeshParamsUniformSet = Rd.UniformSetCreate([TerrainMeshParamsUniform], shader, shaderSet);
    }

    public void SetVertexUniformSet(Rid shader, uint shaderSet)
    {
        if (!shader.IsValid)
        {
            throw new ArgumentNullException($"{shader} is not valid. Must pass valid shader.");
        }

        VertexBufferUniformSet = Rd.UniformSetCreate([VertexBufferUniform], shader, shaderSet);
    }




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
    }
}
