using Godot;
using System.Runtime.InteropServices;
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
    private Rid Shader;
    private Rid Pipeline;

    private TransvoxelShaderParameters? Parameters = null;
    private Rid ParametersBuffer;
    private Rid ParametersUniformSet;

    private Rid LookupTablesBuffer;
    private Rid LookupTablesUniformSet;

    private Rid CounterBuffer;
    private RDUniform CounterBufferUniform;
    private Rid CounterUniformSet;

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
        RDShaderFile shaderFile = GD.Load<RDShaderFile>(descriptor.ShaderPath);
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Shader = rd.ShaderCreateFromSpirV(shaderBytecode);
        Pipeline = rd.ComputePipelineCreate(Shader);

        // Setup Params Buffer
        ParametersBuffer = rd.UniformBufferCreate((uint)Marshal.SizeOf<TransvoxelShaderParameters>());
        RDUniform parametersUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        parametersUniform.AddId(ParametersBuffer);

        // Setup Lookup Tables Buffer
        int[] lookupTablesData = LookupTables.LookupTablesData;
        byte[] lookupTablesDataBytes = new byte[lookupTablesData.Length * sizeof(int)];
        Buffer.BlockCopy(lookupTablesData, 0, lookupTablesDataBytes, 0, lookupTablesDataBytes.Length);

        LookupTablesBuffer = rd.StorageBufferCreate((uint)lookupTablesDataBytes.Length, lookupTablesDataBytes);
        RDUniform lookupTablesBufferUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        lookupTablesBufferUniform.AddId(LookupTablesBuffer);

        // Setup Counter Buffer
        CounterBuffer = rd.StorageBufferCreate(sizeof(uint));
        RDUniform counterBufferUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        counterBufferUniform.AddId(CounterBuffer);
        CounterBufferUniform = counterBufferUniform;

        ParametersUniformSet = Rd.UniformSetCreate([parametersUniform], Shader, PARAMETERS_SHADER_SET);
        LookupTablesUniformSet = Rd.UniformSetCreate([lookupTablesBufferUniform], Shader, LOOKUP_TABLES_SHADER_SET);
        CounterUniformSet = Rd.UniformSetCreate([counterBufferUniform], Shader, COUNTER_SHADER_SET);
    }

    /// <summary>
    /// Sets the parameters needed for the transvoxel algorithm. Does CPU -> GPU
    /// </summary>
    /// <param name="parameters"></param>
    private void SetParameters(TransvoxelShaderParameters parameters)
    {
        if (!Parameters.Equals(parameters))
        {
            Rd.BufferUpdate(ParametersBuffer, 0, (uint)Marshal.SizeOf<TransvoxelShaderParameters>(), StructHelpers.ToByteArray(parameters));
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
    public void Dispatch(TransvoxelShaderParameters parameters, RDUniform sdfUniform, RDUniform normalsUniform, RDUniform verticesUniform)
    {
        SetParameters(parameters);
        Rd.BufferClear(CounterBuffer, 0, sizeof(uint));

        long computeList = Rd.ComputeListBegin();
        RunTransvoxelShader(computeList, sdfUniform, normalsUniform, verticesUniform);

        Rd.ComputeListEnd();
    }

    /// <summary>
    /// Runs the transvoxel algorithm.
    /// </summary>
    /// <param name="computeList"></param>
    /// <param name="sdfUniform"></param>
    /// <param name="normalsUniform"></param>
    /// <param name="verticesUniform"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    private void RunTransvoxelShader(long computeList, RDUniform sdfUniform, RDUniform normalsUniform, RDUniform verticesUniform)
    {
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

        Rid sdfUniformSet = Rd.UniformSetCreate([sdfUniform], Shader, SDF_SHADER_SET);
        Rid normalUniformSet = Rd.UniformSetCreate([normalsUniform], Shader, NORMALS_SHADER_SET);
        Rid verticesUniformSet = Rd.UniformSetCreate([verticesUniform], Shader, VERTICES_SHADER_SET);

        Rd.ComputeListBindComputePipeline(computeList, Pipeline);
        Rd.ComputeListBindUniformSet(computeList, ParametersUniformSet, PARAMETERS_SHADER_SET);
        Rd.ComputeListBindUniformSet(computeList, LookupTablesUniformSet, LOOKUP_TABLES_SHADER_SET);
        Rd.ComputeListBindUniformSet(computeList, CounterUniformSet, COUNTER_SHADER_SET);
        Rd.ComputeListBindUniformSet(computeList, sdfUniformSet, SDF_SHADER_SET);
        Rd.ComputeListBindUniformSet(computeList, normalUniformSet, NORMALS_SHADER_SET);
        Rd.ComputeListBindUniformSet(computeList, verticesUniformSet, VERTICES_SHADER_SET);
        Rd.ComputeListDispatch(computeList, xGroups: chunkSize / (8 * lod), yGroups: chunkSize / (8 * lod), zGroups: chunkSize / (8 * lod));

        Rd.FreeRid(sdfUniformSet);
        Rd.FreeRid(normalUniformSet);
        Rd.FreeRid(verticesUniformSet);
    }

    /// <summary>
    /// Disposes all needed resources for the transvoxel algorithm
    /// </summary>
    public void Dispose()
    {
        Rd.FreeRid(Pipeline);
        Rd.FreeRid(ParametersUniformSet);
        Rd.FreeRid(ParametersBuffer);
        Rd.FreeRid(LookupTablesUniformSet);
        Rd.FreeRid(LookupTablesBuffer);
        Rd.FreeRid(CounterUniformSet);
        Rd.FreeRid(CounterBuffer);
        Rd.FreeRid(Shader);
    }

    /// <summary>
    /// Gets the uniform for the counter buffer. Used for generating indirect args.
    /// </summary>
    /// <returns></returns>
    public RDUniform GetCurrentVertexCountUniform()
    {
        return CounterBufferUniform;
    }
}
