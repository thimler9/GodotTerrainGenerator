using Godot;

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

    public TerrainMesh(RenderingDevice rd, TerrainMeshParameters parameters)
    {
        Rd = rd;
        Parameters = parameters;

        uint maxNumVerts = GetMaxNumVerts();

        // Each vert has a position and normal, both Vector3
        VertexBuffer = rd.StorageBufferCreate(maxNumVerts * 2 * 3 * sizeof(float));
        VertexBufferUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        VertexBufferUniform.AddId(VertexBuffer);

        // Max indirect args buffer
        IndirectArgsBuffer = rd.StorageBufferCreate(4 * sizeof(uint));
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
        if (Parameters == null)
        {
            throw new ArgumentNullException(nameof(Parameters), "Cannot be null");
        }

        if (Rd == null)
        {
            throw new ArgumentNullException(nameof(Rd), "Cannot be null");
        }

        var outputBytes = Rd.BufferGetData(VertexBuffer);
 
        float[] output = new float[GetMaxNumVerts() * 2 * 3];
        Buffer.BlockCopy(outputBytes, 0, output, 0, output.Length * sizeof(float));

        TerrainMeshVertex[] outputVertices = new TerrainMeshVertex[output.Length / 6];
        for (int i = 0; i < output.Length / 3; i++)
        {
            outputVertices[i] = new TerrainMeshVertex(
                new Vector3(output[i * 6], output[i * 6 + 1], output[i * 6 + 2]),
                new Vector3(output[i * 6 + 3], output[i * 6 + 4], output[i * 6 + 5])
            );
        }

        GD.Print("Output: ", string.Join(", ", outputVertices.Select(vert => vert.ToString())));
        Console.WriteLine(string.Join(", ", outputVertices.Select(vert => vert.ToString())));
    }

    public void Dispose()
    {
        Rd.FreeRid(IndirectArgsBuffer);
        Rd.FreeRid(VertexBuffer);
    }
}
