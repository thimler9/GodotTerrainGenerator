using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Utilities.EngineAbstractions;
public class GraphicShader : IDisposable
{
    private RenderingDevice Rd;
    public Rid Shader;

    public GraphicShader(RenderingDevice rd, string shaderPath)
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
    }

    public void Dispose()
    {
        Rd.FreeRid(Shader);
    }

    public bool IsValid()
    {
        return Shader.IsValid;
    }
}
