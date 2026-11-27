using Godot;
using System.Runtime.InteropServices;
using TerrainGeneration.Utilities.EngineAbstractions;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel;
public class TransvoxelShader
{
    private const int PARAMETERS_SHADER_SET = 0;
    private const int LOOKUP_TABLES_SHADER_SET = 1;
    private const int COUNTER_SHADER_SET = 2;
    private const int SDF_SHADER_SET = 3;
    private const int NORMALS_SHADER_SET = 4;
    private const int VERTICES_SHADER_SET = 5;

    private RenderingDevice Rd;
    private ComputeShader Shader;

    private TransvoxelShaderParameters? Parameters = null;
    private ComputeBuffer ParametersBuffer;
    private ComputeBuffer LookupTablesBuffer;
    private ComputeBuffer CounterBuffer;


    /// <summary>
    /// Creates an instance of the transvoxel shader. This algorithm creates triangles from an sdf using the 
    /// transvoxel algorithm. See https://transvoxel.org/
    /// </summary>
    /// <param name="rd"></param>
    /// <param name="descriptor"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public TransvoxelShader(RenderingDevice rd, TransvoxelShaderDescriptor descriptor)
    {
        // Setup Shader info
        if (string.IsNullOrWhiteSpace(descriptor.ShaderPath))
        {
            throw new ArgumentNullException(nameof(descriptor.ShaderPath), "Cannot be null or whitespace");
        }

        if (rd == null)
        {
            throw new ArgumentNullException(nameof(rd), "Cannot be null");
        }

        Rd = rd;
        Shader = new ComputeShader(rd, descriptor.ShaderPath);

        ParametersBuffer = new ComputeBuffer(rd, (uint)Marshal.SizeOf<TransvoxelShaderParameters>(), RenderingDevice.UniformType.UniformBuffer, 0);

        // Setup Lookup Tables Buffer
        int[] lookupTablesData = LookupTables.LookupTablesData;
        byte[] lookupTablesDataBytes = new byte[lookupTablesData.Length * sizeof(int)];
        Buffer.BlockCopy(lookupTablesData, 0, lookupTablesDataBytes, 0, lookupTablesDataBytes.Length);

        LookupTablesBuffer = new ComputeBuffer(rd, (uint)(LookupTables.LookupTablesData.Length * sizeof(int)), RenderingDevice.UniformType.StorageBuffer, 0, lookupTablesDataBytes);
        CounterBuffer = new ComputeBuffer(rd, sizeof(int), RenderingDevice.UniformType.StorageBuffer, 0);
    }

    /// <summary>
    /// Sets the parameters needed for the transvoxel algorithm. Does CPU -> GPU
    /// </summary>
    /// <param name="parameters"></param>
    private void SetParameters(TransvoxelShaderParameters parameters)
    {
        if (!Parameters.Equals(parameters))
        {
            ParametersBuffer.SetData(0, (uint)Marshal.SizeOf<TransvoxelShaderParameters>(), StructHelpers.ToByteArray(parameters));
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Runs the transvoxel algorithm. Uses the given sdfuniform to generate the triangles.
    /// </summary>
    /// <param name="parameters">Chunk parameters for the algorithm</param>
    /// <param name="sdfUniform">Rid buffer of the sdf</param>
    /// <param name="normalsUniform">Rid buffer of the normals for the sdf</param>
    /// <param name="verticesUniform">Rid buffer of the place to output the triangles</param>
    public void Dispatch(TransvoxelShaderParameters parameters, ComputeBuffer sdfUniform, ComputeBuffer normalsUniform, ComputeBuffer verticesUniform)
    {
        SetParameters(parameters);

        if (Parameters == null)
        {
            throw new ArgumentNullException(nameof(Parameters), "Cannot be null");
        }

        uint chunkSize = Parameters.Value.ChunkSize;
        uint lod = Parameters.Value.Lod;

        if (chunkSize / (8 * lod) == 0)
        {
            throw new ArgumentException($"{nameof(chunkSize)} / (8 * {nameof(lod)} must be positive. {nameof(chunkSize)} = {chunkSize}, {nameof(lod)} = {lod}");
        }

        CounterBuffer.ClearData(0, sizeof(uint));

        using ComputePass pass = Shader.GetComputePass();
        pass.BindComputeBuffer(ParametersBuffer, PARAMETERS_SHADER_SET);
        pass.BindComputeBuffer(LookupTablesBuffer, LOOKUP_TABLES_SHADER_SET);
        pass.BindComputeBuffer(CounterBuffer, COUNTER_SHADER_SET);
        pass.BindComputeBuffer(sdfUniform, SDF_SHADER_SET);
        pass.BindComputeBuffer(normalsUniform, NORMALS_SHADER_SET);
        pass.BindComputeBuffer(verticesUniform, VERTICES_SHADER_SET);
        pass.Dispatch(chunkSize / (8 * lod), chunkSize / (8 * lod), chunkSize / (8 * lod));
    }

    /// <summary>
    /// Disposes all needed resources for the transvoxel algorithm
    /// </summary>
    public void Dispose()
    {
        Shader.Dispose();
        ParametersBuffer.Dispose();
        LookupTablesBuffer.Dispose();
        CounterBuffer.Dispose();
    }

    /// <summary>
    /// Gets the uniform for the counter buffer. Used for generating indirect args.
    /// </summary>
    /// <returns></returns>
    public ComputeBuffer GetCurrentVertexCountShader()
    {
        return CounterBuffer;
    }
}
