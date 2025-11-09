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
    public SimplexNoiseParameters? Parameters = null;
    public Rid ParametersBuffer;
    public RDUniform ParametersUniform;
    public Rid ParametersUniformSet;
    public Rid OutputUniformSet;

    public SimplexNoiseShader(RenderingDevice rd, string shaderPath, SimplexNoiseParameters parameters, Rid outputUniformSet)
    {
        ShaderPath = shaderPath;
        RDShaderFile shaderFile = GD.Load<RDShaderFile>(shaderPath);
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Shader = rd.ShaderCreateFromSpirV(shaderBytecode);
        Pipeline = rd.ComputePipelineCreate(Shader);

        // Set Paramters
        Parameters = parameters;
        byte[] parameterBytes = parameters.ToByteArray();
        ParametersBuffer = rd.UniformBufferCreate((uint)Marshal.SizeOf<SimplexNoiseParameters>(), parameterBytes);
        ParametersUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        ParametersUniform.AddId(ParametersBuffer);
        ParametersUniformSet = rd.UniformSetCreate([ParametersUniform], Shader, 0);

        OutputUniformSet = outputUniformSet;
    }

    public void SetParameters(SimplexNoiseParameters parameters)
    {
        
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

    public void Dispose(RenderingDevice rd)
    {
        rd.FreeRid(Pipeline);
        rd.FreeRid(ParametersUniformSet);
        rd.FreeRid(ParametersUniform);
        rd.FreeRid(Shader);
    }
}
