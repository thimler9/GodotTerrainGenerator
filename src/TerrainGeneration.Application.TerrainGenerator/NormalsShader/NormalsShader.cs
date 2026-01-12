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

    public string ShaderPath;
    public Rid Shader;
    public Rid Pipeline;

    public NormalsShaderParameters? Parameters = null;
    public Rid ParametersBuffer;
    public Rid ParametersUniformSet;

    private bool ParametersUpdated = false;

    // We keep the buffer in the shader since the same buffer is used everytime
    public Rid OutputNormalsBuffer;
    public Rid OutputNormalsUniformSet;

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

    private void SetParameters(RenderingDevice rd, NormalsShaderParameters parameters)
    {
        if (!this.Parameters.Equals(parameters))
        {
            rd.BufferUpdate(ParametersBuffer, 0, (uint)Marshal.SizeOf<NormalsShaderParameters>(), StructHelpers.ToByteArray(parameters));
            ParametersUpdated = true;
        }
    }

    public void Dispatch(RenderingDevice rd, NormalsShaderParameters parameters, Rid inputSDFUniformSet)
    {
        SetParameters(rd, parameters);

        // No reason to run if parameters haven't been changed
        if (ParametersUpdated)
        {
            long computeList = rd.ComputeListBegin();

            // Run the shaders


            rd.ComputeListEnd();
            rd.Submit();
            rd.Sync();
            ParametersUpdated = false;
        }
    }

    public void RunNormalsShader(RenderingDevice rd, long computeList, Rid inputSDFUniformSet)
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

        rd.ComputeListBindComputePipeline(computeList, Pipeline);
        rd.ComputeListBindUniformSet(computeList, ParametersUniformSet, 0);
        rd.ComputeListBindUniformSet(computeList, inputSDFUniformSet, 1);
        rd.ComputeListBindUniformSet(computeList, OutputNormalsUniformSet, 2);
        rd.ComputeListDispatch(computeList, xGroups: chunkSize / (8 * lod) + 1, yGroups: chunkSize / (8 * lod) + 1, zGroups: chunkSize / (8 * lod) + 1);
    }

    public void Dispose(RenderingDevice rd)
    {
        // Free the shaders
        rd.FreeRid(Pipeline);
        rd.FreeRid(ParametersUniformSet);
        rd.FreeRid(ParametersBuffer);
        rd.FreeRid(OutputNormalsUniformSet);
        rd.FreeRid(Shader);
        rd.FreeRid(OutputNormalsBuffer);
    }

    public void PrintOutBuffer(RenderingDevice rd)
    {
        if (Parameters == null)
        {
            throw new ArgumentNullException(nameof(Parameters), "Cannot be null");
        }

        if (rd == null)
        {
            throw new ArgumentNullException(nameof(rd), "Cannot be null");
        }

        var outputBytes = rd.BufferGetData(OutputNormalsBuffer);
        var output = new Vector3[(Parameters.Value.ChunkSize / Parameters.Value.Lod + 1) * (Parameters.Value.ChunkSize / Parameters.Value.Lod + 1) * (Parameters.Value.ChunkSize / Parameters.Value.Lod + 1)];
        Buffer.BlockCopy(outputBytes, 0, output, 0, output.Length * sizeof(float) * 3);
        GD.Print("Output: ", string.Join(", ", output));
        Console.WriteLine(string.Join(", ", output));
    }
}
