using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel;
public class IndirectArgsShader
{
    public RenderingDevice Rd;

    private readonly string ShaderPath;
    private Rid Shader;
    private Rid Pipeline;

    public IndirectArgsShader(RenderingDevice rd, IndirectArgsShaderDescriptor descriptor)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor), "Cannot be null.");
        }
        
        if (string.IsNullOrWhiteSpace(descriptor.ShaderPath))
        {
            throw new ArgumentNullException(nameof(descriptor.ShaderPath), "Cannot be null or whitespace");
        }
        
        if (rd == null)
        {
            throw new ArgumentNullException(nameof(descriptor), "Cannot be null.");
        }

        Rd = rd;
        ShaderPath = descriptor.ShaderPath;
        RDShaderFile shaderFile = GD.Load<RDShaderFile>(descriptor.ShaderPath);
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Shader = rd.ShaderCreateFromSpirV(shaderBytecode);
        Pipeline = rd.ComputePipelineCreate(Shader);
    }

    public void Dispatch(RDUniform counterUniform, RDUniform indirectArgsBufferUniform)
    {
        long computeList = Rd.ComputeListBegin();

        RunIndirectArgsShader(computeList, counterUniform, indirectArgsBufferUniform);

        Rd.ComputeListEnd();
        Rd.Submit();
        Rd.Sync();
    }

    public void RunIndirectArgsShader(long computeList, RDUniform counterUniform, RDUniform indirectArgsUniform)
    {
        Rid counterUniformSet = Rd.UniformSetCreate([counterUniform], Shader, 0);
        Rid indirectArgsUniformSet = Rd.UniformSetCreate([indirectArgsUniform], Shader, 1);

        Rd.ComputeListBindComputePipeline(computeList, Pipeline);
        Rd.ComputeListBindUniformSet(computeList, counterUniformSet, 0);
        Rd.ComputeListBindUniformSet(computeList, indirectArgsUniformSet, 1);
        Rd.ComputeListDispatch(computeList, xGroups: 1, yGroups: 1, zGroups: 1);

        Rd.FreeRid(counterUniformSet);
        Rd.FreeRid(indirectArgsUniformSet);
    }
}
