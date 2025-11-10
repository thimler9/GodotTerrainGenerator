using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.SDFGenerator.SimplexNoise;
public class SimplexNoiseShader
{
    public readonly string ShaderPath;
    public Rid Shader;
    public Rid Pipeline;
    public SimplexNoiseShaderParameters? Parameters = null;
    public Rid ParametersBuffer;
    public RDUniform ParametersUniform;
    public Rid ParametersUniformSet;
    public Rid OutputUniformSet;

    /// <summary>
    /// Creates a SimplexNoiseShader. Used to take in the map buffer, and apply the inputted simplex noise to the map. 
    /// </summary>
    /// <param name="rd"></param>
    /// <param name="shaderPath"></param>
    /// <param name="parameters"></param>
    /// <param name="outputUniformSet"></param>
    public SimplexNoiseShader(RenderingDevice rd, SimplexNoiseShaderDescriptor descriptor, RDUniform outputUniform)
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
        byte[] parameterBytes = descriptor.Parameters.ToByteArray();
        ParametersBuffer = rd.UniformBufferCreate((uint)Marshal.SizeOf<SimplexNoiseShaderParameters>(), parameterBytes);
        ParametersUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        ParametersUniform.AddId(ParametersBuffer);
        ParametersUniformSet = rd.UniformSetCreate([ParametersUniform], Shader, 0);

        OutputUniformSet = rd.UniformSetCreate([outputUniform], Shader, 0);
    }

    /// <summary>
    /// Sets the parameters buffer to the new inputted parameters.
    /// </summary>
    /// <param name="rd"></param>
    /// <param name="parameters"></param>
    public void SetParameters(RenderingDevice rd, SimplexNoiseShaderParameters parameters)
    {
        if (!this.Parameters.Equals(parameters))
        {
            rd.BufferUpdate(ParametersBuffer, 0, (uint)Marshal.SizeOf<SimplexNoiseShaderParameters>(), parameters.ToByteArray());
        }
        else
        {
            GD.PrintErr($"{nameof(parameters)} was not different when trying to change SimplexNoiseShaderParameters.");
        }
    }

    /// <summary>
    /// Dispatches shader on the given compute list.
    /// </summary>
    /// <param name="computeList"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Dispatch(RenderingDevice rd, long computeList, uint chunkSize)
    {
        if (Parameters == null)
        {
            throw new ArgumentNullException(nameof(Parameters), "Cannot be null");
        }

        if (chunkSize / 8 == 0)
        {
            throw new ArgumentException($"{nameof(chunkSize)} / 8 must be positive. {nameof(chunkSize)} = {chunkSize}");
        }

        rd.ComputeListBindComputePipeline(computeList, Pipeline);
        rd.ComputeListBindUniformSet(computeList, ParametersUniformSet, 0);
        rd.ComputeListBindUniformSet(computeList, OutputUniformSet, 1);
        rd.ComputeListDispatch(computeList, xGroups: chunkSize / 8, yGroups: chunkSize / 8, zGroups: chunkSize / 8);
        rd.ComputeListEnd();
    }

    /// <summary>
    /// Disposes all necessary resources for the shader
    /// </summary>
    /// <param name="rd"></param>
    public void Dispose(RenderingDevice rd)
    {
        rd.FreeRid(Pipeline);
        rd.FreeRid(ParametersUniformSet);
        rd.FreeRid(ParametersBuffer);
        rd.FreeRid(OutputUniformSet);
        rd.FreeRid(Shader);
    }
}
