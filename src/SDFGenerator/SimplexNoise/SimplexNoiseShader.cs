using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Application.SDFGenerator.SimplexNoise;
public class SimplexNoiseShader
{
    private RenderingDevice Rd;
    public readonly string ShaderPath;
    public Rid Shader;
    public Rid Pipeline;

    public SimplexNoiseShaderParameters? Parameters = null;
    public Rid ParametersBuffer;
    public Rid ParametersUniformSet;

    public Rid SDFParamtersUniformSet;
    public Rid OutputUniformSet;

    /// <summary>
    /// Creates a SimplexNoiseShader. Used to take in the map buffer, and apply the inputted simplex noise to the map. 
    /// </summary>
    /// <param name="rd"></param>
    /// <param name="shaderPath"></param>
    /// <param name="parameters"></param>
    /// <param name="outputUniformSet"></param>
    public SimplexNoiseShader(RenderingDevice rd, SimplexNoiseShaderDescriptor descriptor, RDUniform sdfUniform, RDUniform outputUniform)
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
        RDUniform ParametersUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        ParametersUniform.AddId(ParametersBuffer);

        ParametersUniformSet = rd.UniformSetCreate([ParametersUniform], Shader, 0);
        SDFParamtersUniformSet = rd.UniformSetCreate([sdfUniform], Shader, 1);
        OutputUniformSet = rd.UniformSetCreate([outputUniform], Shader, 2);
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
        }
    }

    /// <summary>
    /// Dispatches shader on the given compute list.
    /// </summary>
    /// <param name="computeList"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Dispatch(long computeList, uint chunkSize, uint lod)
    {
        if (Parameters == null)
        {
            throw new ArgumentNullException(nameof(Parameters), "Cannot be null");
        }

        if (chunkSize / (8 * lod)  == 0)
        {
            throw new ArgumentException($"{nameof(chunkSize)} / (8 * {nameof(lod)} must be positive. {nameof(chunkSize)} = {chunkSize}, {nameof(lod)} = {lod}");
        }

        Rd.ComputeListBindComputePipeline(computeList, Pipeline);
        Rd.ComputeListBindUniformSet(computeList, ParametersUniformSet, 0);
        Rd.ComputeListBindUniformSet(computeList, SDFParamtersUniformSet, 1);
        Rd.ComputeListBindUniformSet(computeList, OutputUniformSet, 2);
        Rd.ComputeListDispatch(computeList, xGroups: chunkSize / (8 * lod) + 2, yGroups: chunkSize / (8 * lod) + 2, zGroups: chunkSize / (8 * lod) + 2);
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
}
