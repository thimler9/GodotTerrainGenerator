using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Application.TerrainGenerator.NormalsShader;
internal class NormalsShader
{
    private RenderingDevice Rd;
    private string ShaderPath;
    private Rid Shader;
    private Rid Pipeline;
    
    private NormalsShaderParameters? Parameters = null;
    private Rid ParametersBuffer;
    private Rid ParametersUniformSet;

    private bool ParametersUpdated = false;

    // We keep the buffer in the shader since the same buffer is used everytime
    private Rid OutputNormalsBuffer;
    private Rid OutputNormalsUniformSet;

    public NormalsShader(RenderingDevice rd, NormalsShaderDescriptor descriptor)
    {
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

        // Set Paramters
        Parameters = descriptor.Parameters;
        byte[] parameterBytes = StructHelpers.ToByteArray(descriptor.Parameters);
        ParametersBuffer = rd.UniformBufferCreate((uint)Marshal.SizeOf<NormalsShaderParameters>(), parameterBytes);
        RDUniform ParametersUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        ParametersUniform.AddId(ParametersBuffer);
        ParametersUpdated = true;

        // Create the output buffer used throughout calculations
        uint chunkSizeToLodRatio = descriptor.Parameters.ChunkSize / descriptor.Parameters.Lod;
        if (chunkSizeToLodRatio == 0)
        {
            throw new ArgumentException($"{nameof(descriptor.Parameters.ChunkSize)} / {nameof(descriptor.Parameters.Lod)} must be greater than 0");
        }

        OutputNormalsBuffer = rd.StorageBufferCreate((chunkSizeToLodRatio + 1) * (chunkSizeToLodRatio + 1) * (chunkSizeToLodRatio + 1) * sizeof(float) * 3);
        RDUniform outputBufferUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        outputBufferUniform.AddId(OutputNormalsBuffer);

        ParametersUniformSet = rd.UniformSetCreate([ParametersUniform], Shader, 0);
        OutputNormalsUniformSet = rd.UniformSetCreate([outputBufferUniform], Shader, 2);
    }

    private void SetParameters(NormalsShaderParameters parameters)
    {
        if (!this.Parameters.Equals(parameters))
        {
            Rd.BufferUpdate(ParametersBuffer, 0, (uint)Marshal.SizeOf<NormalsShaderParameters>(), StructHelpers.ToByteArray(parameters));
            ParametersUpdated = true;
        }
    }

    public void Dispatch(NormalsShaderParameters parameters, RDUniform inputSDFUniform)
    {
        SetParameters(parameters);

        // No reason to run if parameters haven't been changed
        if (ParametersUpdated)
        {
            long computeList = Rd.ComputeListBegin();

            // Run the shaders
            RunNormalsShader(computeList, inputSDFUniform);

            Rd.ComputeListEnd();
            Rd.Submit();
            Rd.Sync();
            ParametersUpdated = false;
        }
    }

    public void RunNormalsShader(long computeList, RDUniform inputSDFUniform)
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

        Rid inputSDFUniformSet = Rd.UniformSetCreate([inputSDFUniform], Shader, 1);

        Rd.ComputeListBindComputePipeline(computeList, Pipeline);
        Rd.ComputeListBindUniformSet(computeList, ParametersUniformSet, 0);
        Rd.ComputeListBindUniformSet(computeList, inputSDFUniformSet, 1);
        Rd.ComputeListBindUniformSet(computeList, OutputNormalsUniformSet, 2);
        Rd.ComputeListDispatch(computeList, xGroups: chunkSize / (8 * lod) + 1, yGroups: chunkSize / (8 * lod) + 1, zGroups: chunkSize / (8 * lod) + 1);

        Rd.FreeRid(inputSDFUniformSet);
    }

    public void Dispose()
    {
        // Free the shaders
        Rd.FreeRid(Pipeline);
        Rd.FreeRid(ParametersUniformSet);
        Rd.FreeRid(ParametersBuffer);
        Rd.FreeRid(OutputNormalsUniformSet);
        Rd.FreeRid(Shader);
        Rd.FreeRid(OutputNormalsBuffer);
    }

    public void PrintOutBuffer()
    {
        if (Parameters == null)
        {
            throw new ArgumentNullException(nameof(Parameters), "Cannot be null");
        }

        if (Rd == null)
        {
            throw new ArgumentNullException(nameof(Rd), "Cannot be null");
        }

        var outputBytes = Rd.BufferGetData(OutputNormalsBuffer);
        var output = new Vector3[(Parameters.Value.ChunkSize / Parameters.Value.Lod + 1) * (Parameters.Value.ChunkSize / Parameters.Value.Lod + 1) * (Parameters.Value.ChunkSize / Parameters.Value.Lod + 1)];
        Buffer.BlockCopy(outputBytes, 0, output, 0, output.Length * sizeof(float) * 3);
        GD.Print("Output: ", string.Join(", ", output));
        Console.WriteLine(string.Join(", ", output));
    }
}
