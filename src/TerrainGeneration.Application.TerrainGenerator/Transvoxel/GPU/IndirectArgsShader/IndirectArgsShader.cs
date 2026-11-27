using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Utilities.EngineAbstractions;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel;
public class IndirectArgsShader
{
    private const int COUNTER_SHADER_SET = 0;
    private const int INDIRECT_ARGS_SHADER_SET = 1;


    public RenderingDevice Rd;

    private readonly string ShaderPath;
    private ComputeShader Shader;

    /// <summary>
    /// Creates an instance of the indirect args shader. Sets the parameters needed to draw the triangles to the screen.
    /// </summary>
    /// <param name="rd"></param>
    /// <param name="descriptor"></param>
    /// <exception cref="ArgumentNullException"></exception>
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
        Shader = new ComputeShader(rd, descriptor.ShaderPath);
    }

    /// <summary>
    /// Dispatch the indirect args shader. Sets the necessary parameters for drawing the triangles on the screen.
    /// </summary>
    /// <param name="counterUniform"></param>
    /// <param name="indirectArgsBufferUniform"></param>
    public void Dispatch(ComputeBuffer counterBuffer, ComputeBuffer indirectArgsBuffer)
    {
        using ComputePass pass = Shader.GetComputePass();
        pass.BindComputeBuffer(counterBuffer, COUNTER_SHADER_SET);
        pass.BindComputeBuffer(indirectArgsBuffer, INDIRECT_ARGS_SHADER_SET);
        pass.Dispatch(1, 1, 1);
    }
}
