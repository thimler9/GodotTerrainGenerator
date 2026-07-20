using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Utilities.EngineAbstractions;

public class ComputeShader : IDisposable
{
    private RenderingDevice Rd;
    public Rid Shader;
    private Rid Pipeline;

    public ComputeShader(RenderingDevice rd, string shaderPath)
    {
        if (string.IsNullOrWhiteSpace(shaderPath))
        {
            throw new ArgumentNullException($"{nameof(shaderPath)} cannot be null.");
        }

        if (rd == null)
        {
            throw new ArgumentNullException($"{nameof(rd)} cannot be null.");
        }

        Rd = rd;
        RDShaderFile shaderFile = GD.Load<RDShaderFile>(shaderPath);
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Shader = Rd.ShaderCreateFromSpirV(shaderBytecode);
        Pipeline = Rd.ComputePipelineCreate(Shader);
    }

    public ComputePass GetComputePass()
    {
        return new ComputePass(Rd, this, Pipeline);
    }

    public void Dispose()
    {
        Rd.FreeRid(Shader);
        Rd.FreeRid(Pipeline);
    }
}
