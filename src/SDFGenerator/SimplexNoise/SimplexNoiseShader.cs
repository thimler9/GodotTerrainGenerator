using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Application.SDFGenerator.SimplexNoise;
public class SimplexNoiseShader : ISDFShader
{
    private const int PARAMETERS_SHADER_SET = 0;
    private const int SDF_PARAMETERS_SHADER_SET = 1;
    private const int OUTPUT_SHADER_SET = 2;

    private RenderingDevice Rd;
    private readonly string ShaderPath;
    private Rid Shader;
    private Rid Pipeline;

    private SimplexNoiseShaderParameters? Parameters = null;
    private Rid ParametersBuffer;
    private RDUniform ParametersUniform;
    private Rid ParametersUniformSet;

    private Rid SDFParamtersUniformSet;
    private Rid OutputUniformSet;

    /// <summary>
    /// Creates a SimplexNoiseShader. Used to take in the map buffer, and apply the inputted simplex noise to the map. 
    /// </summary>
    /// <param name="rd"></param>
    /// <param name="shaderPath"></param>
    /// <param name="parameters"></param>
    /// <param name="outputUniformSet"></param>
    public SimplexNoiseShader(RenderingDevice rd, SimplexNoiseShaderDescriptor descriptor)
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
        ParametersBuffer = rd.UniformBufferCreate((uint)Marshal.SizeOf<SimplexNoiseShaderParameters>(), parameterBytes);
        ParametersUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        ParametersUniform.AddId(ParametersBuffer);

        ParametersUniformSet = rd.UniformSetCreate([ParametersUniform], Shader, PARAMETERS_SHADER_SET);
    }

    /// <summary>
    /// Sets the parameters buffer to the new inputted parameters. Use somewhat sparingly, does CPU->GPU
    /// </summary>
    /// <param name="rd"></param>
    /// <param name="parameters"></param>
    public void SetParameters(SimplexNoiseShaderParameters parameters)
    {
        if (!this.Parameters.Equals(parameters))
        {
            Rd.BufferUpdate(ParametersBuffer, 0, (uint)Marshal.SizeOf<SimplexNoiseShaderParameters>(), StructHelpers.ToByteArray(parameters));
            
            if (ParametersUniformSet.IsValid)
            {
                Rd.FreeRid(ParametersUniformSet);
            }
            ParametersUniformSet = Rd.UniformSetCreate([ParametersUniform], Shader, PARAMETERS_SHADER_SET);
        }
    }

    /// <summary>
    /// Sets the output uniform set used for dispatching.
    /// </summary>
    /// <param name="outputUniform"></param>
    public void SetOutputUniformSet(RDUniform outputUniform)
    {
        if (OutputUniformSet.IsValid)
        {
            Rd.FreeRid(OutputUniformSet);
        }
        OutputUniformSet = Rd.UniformSetCreate([outputUniform], Shader, OUTPUT_SHADER_SET);
    }

    public void SetSDFParametersUniformSet(RDUniform sdfUniform)
    {
        if (SDFParamtersUniformSet.IsValid)
        {
            Rd.FreeRid(SDFParamtersUniformSet);
        }
        SDFParamtersUniformSet = Rd.UniformSetCreate([sdfUniform], Shader, SDF_PARAMETERS_SHADER_SET);
    }

    /// <summary>
    /// Dispatches shader on the given compute list.
    /// </summary>
    /// <param name="computeList"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Dispatch(uint chunkSize, uint lod)
    {
        if (Parameters == null)
        {
            throw new ArgumentNullException(nameof(Parameters), "Cannot be null");
        }

        if (chunkSize / (8 * lod)  == 0)
        {
            throw new ArgumentException($"{nameof(chunkSize)} / (8 * {nameof(lod)} must be positive. {nameof(chunkSize)} = {chunkSize}, {nameof(lod)} = {lod}");
        }

        if (!OutputUniformSet.IsValid && !Rd.UniformSetIsValid(OutputUniformSet))
        {
            throw new ArgumentNullException($"{nameof(OutputUniformSet)} is not valid. You need to call {nameof(SetOutputUniformSet)} first");
        }

        if (!SDFParamtersUniformSet.IsValid && !Rd.UniformSetIsValid(SDFParamtersUniformSet))
        {
            throw new ArgumentNullException($"{nameof(SDFParamtersUniformSet)} is not valid. You need to call {nameof(SetSDFParametersUniformSet)} first");
        }

        long computeList = Rd.ComputeListBegin();

        Rd.ComputeListBindComputePipeline(computeList, Pipeline);
        Rd.ComputeListBindUniformSet(computeList, ParametersUniformSet, PARAMETERS_SHADER_SET);
        Rd.ComputeListBindUniformSet(computeList, SDFParamtersUniformSet, SDF_PARAMETERS_SHADER_SET);
        Rd.ComputeListBindUniformSet(computeList, OutputUniformSet, OUTPUT_SHADER_SET);
        Rd.ComputeListDispatch(computeList, xGroups: chunkSize / (8 * lod) + 2, yGroups: chunkSize / (8 * lod) + 2, zGroups: chunkSize / (8 * lod) + 2);
        Rd.ComputeListEnd();
    }

    /// <summary>
    /// Disposes all necessary resources for the shader
    /// </summary>
    /// <param name="Rd"></param>
    public void Dispose()
    {
        Rd.FreeRid(Pipeline);
        Rd.FreeRid(ParametersUniformSet);
        Rd.FreeRid(ParametersBuffer);
        Rd.FreeRid(OutputUniformSet);
        Rd.FreeRid(SDFParamtersUniformSet);
        Rd.FreeRid(Shader);
    }

    public SimplexNoiseShaderParameters? GetSimplexShaderParameters()
    {
        return Parameters;
    }
}
