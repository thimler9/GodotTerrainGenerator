using Godot;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;

namespace TerrainGeneration.Application.TerrainGenerator;
public class TerrainMesh
{
    private const uint VERT_DIVISOR = 10;

    private RenderingDevice Rd;

    public Rid VertexBuffer;
    public RDUniform VertexBufferUniform;

    public Rid IndirectArgsBuffer;
    public RDUniform IndirectArgsBufferUniform;

    public TerrainMeshParameters? Parameters;

    private Rid RenderPipeline;

    public TerrainMesh(RenderingDevice rd, TerrainMeshParameters parameters)
    {
        Rd = rd;
        Parameters = parameters;

        uint maxNumVerts = GetMaxNumVerts();

        // Each vert has a position and normal, both Vector3
        VertexBuffer = rd.VertexBufferCreate(maxNumVerts * (uint)Marshal.SizeOf<TerrainMeshVertex>(), creationBits: RenderingDevice.BufferCreationBits.AsStorageBit);
        VertexBufferUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        VertexBufferUniform.AddId(VertexBuffer);

        // Max indirect args buffer
        IndirectArgsBuffer = rd.StorageBufferCreate(sizeof(uint) * 4);
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
        uint maxNumVerts = (maxNumBorderVerts + maxNumInternalVerts) / VERT_DIVISOR;

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
        for (int i = 0; i < output.Length / 8; i++)
        {
            outputVertices[i] = new TerrainMeshVertex(
                new Vector3(output[i * 8], output[i * 8 + 1], output[i * 8 + 2]),
                new Vector3(output[i * 8 + 4], output[i * 8 + 5], output[i * 8 + 6])
            );
        }

        GD.Print("Output: ", string.Join(", ", outputVertices.Select(vert => vert.ToString())));
        Console.WriteLine(string.Join(", ", outputVertices.Select(vert => vert.ToString())));
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

    public void Render()
    {
        long drawList = Rd.DrawListBeginForScreen(
            DisplayServer.WindowGetCurrentScreen()
        );

        Rd.DrawListBindRenderPipeline(drawList, RenderPipeline);
        Rd.DrawListBindVertexArray(drawList, VertexBuffer);
        Rd.DrawListDrawIndirect(drawList, false, IndirectArgsBuffer, 0, 1, sizeof(uint) * 4);
    }

    private Rid GetRenderPipeline(TerrainMeshRenderPipelineDescriptor descriptor)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor), "Cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(descriptor.ShaderPath))
        {
            throw new ArgumentNullException(nameof(descriptor.ShaderPath), "Cannot be null or whitespace");
        }

        string shaderPath = descriptor.ShaderPath;
        RDShaderFile shaderFile = GD.Load<RDShaderFile>(descriptor.ShaderPath);
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Rid shader = Rd.ShaderCreateFromSpirV(shaderBytecode);

        // Position descriptor for vertices
        RDVertexAttribute vertexAttributePosition = new RDVertexAttribute()
        {
            Format = RenderingDevice.DataFormat.R32G32Sfloat,
            Frequency = RenderingDevice.VertexFrequency.Vertex,
            Location = 0,
            Offset = 0,
            Stride = (uint)Marshal.SizeOf<Vector3>()
        };

        // Normal descriptor for vertices
        RDVertexAttribute vertexAttributeNormal = new RDVertexAttribute()
        {
            Format = RenderingDevice.DataFormat.R32G32Sfloat,
            Frequency = RenderingDevice.VertexFrequency.Vertex,
            Location = 1,
            Offset = 0,
            Stride = (uint)Marshal.SizeOf<Vector3>()
        };

        long vertexFormat = Rd.VertexFormatCreate([vertexAttributePosition, vertexAttributeNormal]);
    }
}
