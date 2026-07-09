using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;
using TerrainGeneration.Utilities.EngineAbstractions;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel.NormalsShader;
public class NormalsShader
{
    private const int PARAMETERS_SHADER_SET = 0;
    private const int SDF_SHADER_SET = 1;
    private const int OUTPUT_NORMALS_SHADER_SET = 2;

    private RenderingDevice Rd;
    
    private NormalsShaderParameters? Parameters = null;

    private ComputeShader Shader;
    private ComputeBuffer ParametersBuffer;
    private ComputeBuffer OutputBuffer;

    /// <summary>
    /// Creates a Normals Shader instance. Used for calcing normals for the Terrain Meshes
    /// </summary>
    /// <param name="rd"></param>
    /// <param name="descriptor"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
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
        Shader = new ComputeShader(rd, descriptor.ShaderPath);
        ParametersBuffer = new ComputeBuffer(rd, (uint)Marshal.SizeOf<NormalsShaderParameters>(), RenderingDevice.UniformType.UniformBuffer, 0);

        // Create the output buffer used throughout calculations
        uint chunkSizeToLodRatio = descriptor.ChunkSize / descriptor.Lod;
        if (chunkSizeToLodRatio == 0)
        {
            throw new ArgumentException($"{nameof(descriptor.ChunkSize)} / {nameof(descriptor.Lod)} must be greater than 0");
        }

        OutputBuffer = new ComputeBuffer(rd, (chunkSizeToLodRatio + 1) * (chunkSizeToLodRatio + 1) * (chunkSizeToLodRatio + 1) * sizeof(float) * 3, RenderingDevice.UniformType.StorageBuffer, 0);
    }

    /// <summary>
    /// Updates the paramters in the normals shader parameters buffer
    /// </summary>
    /// <param name="parameters"></param>
    private void SetParameters(NormalsShaderParameters parameters)
    {
        if (!Parameters.Equals(parameters))
        {
            ParametersBuffer.SetData(0, (uint)Marshal.SizeOf<NormalsShaderParameters>(), StructHelpers.ToByteArray(parameters));
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Dispatches the shader; creates a buffer with the normals for the sdf values.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="inputSDFUniform"></param>
    public ComputeBuffer Dispatch(NormalsShaderParameters parameters, ComputeBuffer inputSDFBuffer)
    {
        SetParameters(parameters);

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

        using ComputePass normalsPass = Shader.GetComputePass();
        normalsPass.BindComputeBuffer(ParametersBuffer, PARAMETERS_SHADER_SET);
        normalsPass.BindComputeBuffer(inputSDFBuffer, SDF_SHADER_SET);
        normalsPass.BindComputeBuffer(OutputBuffer, OUTPUT_NORMALS_SHADER_SET);
        normalsPass.Dispatch(chunkSize / (8 * lod) + 1, chunkSize / (8 * lod) + 1, chunkSize / (8 * lod) + 1);

        return OutputBuffer;
    }

    /// <summary>
    /// Disposes the resources associated with the normals shader
    /// </summary>
    public void Dispose()
    {
        ParametersBuffer.Dispose();
        OutputBuffer.Dispose();
        Shader.Dispose();
    }

    /// <summary>
    /// Prints out all of the normals in the normals buffer. Use for debugging only. VERY slow.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
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

        var outputBytes = OutputBuffer.GetData();
        float[] output = new float[(Parameters.Value.ChunkSize / Parameters.Value.Lod + 1) * (Parameters.Value.ChunkSize / Parameters.Value.Lod + 1) * (Parameters.Value.ChunkSize / Parameters.Value.Lod + 1) * 3];
        Buffer.BlockCopy(outputBytes, 0, output, 0, output.Length * sizeof(float));
        
        Vector3[] outputVectors = new Vector3[output.Length / 4];
        for (int i = 0; i < output.Length / 4; i++)
        {
            outputVectors[i] = new Vector3(output[i * 3], output[i * 3 + 1], output[i * 3 + 2]);
        }
        
        GD.Print("Output: ", string.Join(", ", outputVectors.Select(vec => $"{vec}\n").ToArray()));
        Console.WriteLine(string.Join(", ", outputVectors));
    }
}
