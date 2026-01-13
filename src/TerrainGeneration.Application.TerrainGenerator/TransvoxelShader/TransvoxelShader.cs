using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.TerrainGenerator.NormalsShader;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Application.TerrainGenerator.TransvoxelShader;
public class TransvoxelShader
{
    private RenderingDevice Rd;
    private string ShaderPath;
    private Rid Shader;
    private Rid Pipeline;

    private TransvoxelShaderParameters? Parameters = null;
    private Rid ParametersBuffer;
    private Rid ParametersUniformSet;

    private bool ParametersUpdated = false;

    private Rid CounterBuffer;
    private Rid CounterUniformSet;

    private Rid LookupTablesBuffer;
    private Rid LookupTablesUniformSet;

    // Think we want to keep the buffer pool for the triangles in the shader
    private uint MaxNumTerrainMeshesInQueue;
    private Queue<TerrainMesh> TerrainMeshes;

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
        RDUniform ParametersUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        ParametersUniform.AddId(ParametersBuffer);
        ParametersUpdated = true;

        // Setup Counter Buffer
        CounterBuffer = rd.StorageBufferCreate(1 * sizeof(float));
        RDUniform counterBufferUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        counterBufferUniform.AddId(CounterBuffer);

        // Setup Lookup Tables Buffer
        int[] lookupTablesData = LookupTables.LookupTablesData;
        byte[] lookupTablesDataBytes = new byte[lookupTablesData.Length * sizeof(int)];
        Buffer.BlockCopy(lookupTablesData, 0, lookupTablesDataBytes, 0, lookupTablesDataBytes.Length);

        LookupTablesBuffer = rd.UniformBufferCreate((uint)lookupTablesDataBytes.Length, lookupTablesDataBytes);
        RDUniform lookupTablesBufferUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        lookupTablesBufferUniform.AddId(LookupTablesBuffer);

        TerrainMeshes = new Queue<TerrainMesh>();
    }

    private void SetParameters(TransvoxelShaderParameters parameters)
    {

    }

    public void GetTerrainMesh(TransvoxelShaderParameters parameters, RDUniform sdfUniform, RDUniform normalsUniform)
    {

    }

    public void RunTransvoxelShader(long computeList, RDUniform sdfUniform, RDUniform normalsUniform)
    {

    }

    public void Dispose()
    {

    }

    public void PrintOutBuffer()
    {

    }
}
