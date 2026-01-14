using Godot;
using System.Runtime.InteropServices;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel;
public class TransvoxelShader
{
    private RenderingDevice Rd;
    private readonly string ShaderPath;
    private Rid Shader;
    private Rid Pipeline;

    private TransvoxelShaderParameters? Parameters = null;
    private Rid ParametersBuffer;
    private Rid ParametersUniformSet;

    private bool ParametersUpdated = false;

    private Rid LookupTablesBuffer;
    private Rid LookupTablesUniformSet;

    private Rid CounterBuffer;
    private RDUniform CounterBufferUniform;
    private Rid CounterUniformSet;

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
        ShaderPath = descriptor.ShaderPath;
        RDShaderFile shaderFile = GD.Load<RDShaderFile>(descriptor.ShaderPath);
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Shader = rd.ShaderCreateFromSpirV(shaderBytecode);
        Pipeline = rd.ComputePipelineCreate(Shader);

        // Setup Params Buffer
        Parameters = descriptor.Parameters;
        byte[] parameterBytes = StructHelpers.ToByteArray(descriptor.Parameters);
        ParametersBuffer = rd.UniformBufferCreate((uint)Marshal.SizeOf<TransvoxelShaderParameters>(), parameterBytes);
        RDUniform parametersUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        parametersUniform.AddId(ParametersBuffer);
        ParametersUpdated = true;

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
        CounterBuffer = rd.StorageBufferCreate(1 * sizeof(uint));
        RDUniform counterBufferUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        counterBufferUniform.AddId(CounterBuffer);
        CounterBufferUniform = counterBufferUniform;

        ParametersUniformSet = Rd.UniformSetCreate([parametersUniform], Shader, 0);
        LookupTablesUniformSet = Rd.UniformSetCreate([lookupTablesBufferUniform], Shader, 1);
        CounterUniformSet = Rd.UniformSetCreate([counterBufferUniform], Shader, 2);
    }

    private void SetParameters(TransvoxelShaderParameters parameters)
    {
        if (!Parameters.Equals(parameters))
        {
            Rd.BufferUpdate(ParametersBuffer, 0, (uint)Marshal.SizeOf<TransvoxelShaderParameters>(), StructHelpers.ToByteArray(parameters));
            ParametersUpdated = true;
        }
    }

    public void Dispatch(TransvoxelShaderParameters parameters, RDUniform sdfUniform, RDUniform normalsUniform, RDUniform verticesUniform)
    {
        SetParameters(parameters);
        Rd.BufferClear(CounterBuffer, 0, sizeof(uint));

        long computeList = Rd.ComputeListBegin();

        RunTransvoxelShader(computeList, sdfUniform, normalsUniform, verticesUniform);

        Rd.ComputeListEnd();
        Rd.Submit();
        Rd.Sync();
        ParametersUpdated = false;
    }

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

        Rid sdfUniformSet = Rd.UniformSetCreate([sdfUniform], Shader, 3);
        Rid normalUniformSet = Rd.UniformSetCreate([normalsUniform], Shader, 4);
        Rid verticesUniformSet = Rd.UniformSetCreate([verticesUniform], Shader, 5);

        Rd.ComputeListBindComputePipeline(computeList, Pipeline);
        Rd.ComputeListBindUniformSet(computeList, ParametersUniformSet, 0);
        Rd.ComputeListBindUniformSet(computeList, LookupTablesUniformSet, 1);
        Rd.ComputeListBindUniformSet(computeList, CounterUniformSet, 2);
        Rd.ComputeListBindUniformSet(computeList, sdfUniformSet, 3);
        Rd.ComputeListBindUniformSet(computeList, normalUniformSet, 4);
        Rd.ComputeListBindUniformSet(computeList, verticesUniformSet, 5);
        Rd.ComputeListDispatch(computeList, xGroups: chunkSize / (8 * lod), yGroups: chunkSize / (8 * lod), zGroups: chunkSize / (8 * lod));

        Rd.FreeRid(sdfUniformSet);
        Rd.FreeRid(normalUniformSet);
        Rd.FreeRid(verticesUniformSet);
    }


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

    public RDUniform GetCurrentVertexCountUniform()
    {
        return CounterBufferUniform;
    }
}
