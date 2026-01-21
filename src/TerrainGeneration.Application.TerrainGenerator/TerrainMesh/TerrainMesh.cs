using Godot;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;
using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;

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

    public TerrainMeshParameters? Parameters;

    public TerrainMesh(RenderingDevice rd, TerrainMeshParameters parameters)
    {
        Rd = rd;
        Parameters = parameters;

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
    }

    private uint GetMaxNumVerts()
    {
        if (Parameters == null)
        {
            throw new ArgumentNullException(nameof(Parameters), "Cannot be null");
        }

        uint chunkSizeToLodRatio = Parameters.ChunkSize / Parameters.Lod;

        // 5 is the max number of triangles per cell, 3 is the number of verts per triangle
        uint maxNumInternalVerts = chunkSizeToLodRatio * chunkSizeToLodRatio * chunkSizeToLodRatio * 5 * 3;

        // 12 is the max number of triangles per cell, 3 is the number of verts per triangle, 6 is the number of sides to the chunk (we need 6 different border triangle sets)
        uint maxNumBorderVerts = chunkSizeToLodRatio * chunkSizeToLodRatio * 12 * 6 * 3;

        // Vert divisor is to save on memory since most chunks won't use the max amount
        uint maxNumVerts = Math.Min((maxNumBorderVerts + maxNumInternalVerts) / VERT_DIVISOR, Parameters.MaxNumTriangles);

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
    }

    public void ResetBuffers()
    {
        Rd.BufferClear(IndirectArgsBuffer, 0, sizeof(uint) * 4);
    }

    public void Render(TerrainMeshDrawDescriptor descriptor)
    {
        long drawList = Rd.DrawListBegin(descriptor.SreenBuffer, RenderingDevice.DrawFlags.ClearColor0);

        VertexBufferUniformSet = Rd.UniformSetCreate([VertexBufferUniform], descriptor.Shader, 0);

        Rd.DrawListBindRenderPipeline(drawList, descriptor.RenderPipeline);
        Rd.DrawListBindVertexArray(drawList, descriptor.EmptyVertexArrayBuffer);
        Rd.DrawListBindUniformSet(drawList, VertexBufferUniformSet, 0);
        Rd.DrawListDrawIndirect(drawList, false, IndirectArgsBuffer, 0, 1, sizeof(uint) * 4);
    }
}
