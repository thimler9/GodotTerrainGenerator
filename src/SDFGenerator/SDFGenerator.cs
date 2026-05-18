using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

namespace TerrainGeneration.Application.SDFGenerator;

public class SDFGenerator
{
    private RenderingDevice Rd;
    SDFShaderParameters SDFShaderParameters;

    public RDUniform OutputBufferUniform { get; set; }
    Rid OutputBuffer;

    RDUniform SDFParametersUniform;
    Rid SDFParametersBuffer;

    ISDFShader SDFShader;

    /// <summary>
    /// Create an SDFGenerator; dispatches compute shader for some sdf function.
    /// </summary>
    /// <param name="settings"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public SDFGenerator(RenderingDevice rd, SDFGeneratorSettings settings)
    {
        if (settings.SDFShader == null)
        {
            throw new ArgumentNullException(nameof(settings.SDFShader), "Cannot be null");
        }

        Rd = rd;
        SDFShaderParameters = settings.SDFShaderParameters;

        // Create the output buffer used throughout calculations
        uint chunkSizeToLodRatio = SDFShaderParameters.ChunkSize / SDFShaderParameters.Lod;

        Rid outputBuffer = Rd.StorageBufferCreate((chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * sizeof(float));
        OutputBufferUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        OutputBufferUniform.AddId(outputBuffer);
        OutputBuffer = outputBuffer;

        // Create the sdf paramters buffer that has info used in all shaders
        byte[] sdfParametersBytes = settings.SDFShaderParameters.ToByteArray();
        SDFParametersBuffer = Rd.UniformBufferCreate((uint)Marshal.SizeOf<SDFShaderParameters>(), sdfParametersBytes);
        SDFParametersUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        SDFParametersUniform.AddId(SDFParametersBuffer);

        // Setup the shaders
        SDFShader = settings.SDFShader;
        SDFShader.SetOutputUniformSet(OutputBufferUniform);
        SDFShader.SetSDFParametersUniformSet(SDFParametersUniform);
    }

    /// <summary>
    /// Sets the parameters for the sdf shader params buffer
    /// </summary>
    /// <param name="parameters"></param>
    public void SetSDFParameters(SDFShaderParameters parameters)
    {
        if (!this.SDFShaderParameters.Equals(parameters))
        {
            Rd.BufferUpdate(SDFParametersBuffer, 0, (uint)Marshal.SizeOf<SDFShaderParameters>(), parameters.ToByteArray());
            SDFShaderParameters = parameters;
        }
    }

    /// <summary>
    /// Executes the sdf shader. Will update the parameters if necessary.
    /// </summary>
    /// <param name="parameters"></param>
    public void DispatchShaders(SDFShaderParameters parameters)
    {
        SetSDFParameters(parameters);

        // Run the shaders
        SimplexNoise();
    }

    /// <summary>
    /// Runs the Shader's dispatch function
    /// </summary>
    /// <param name="computeList"></param>
    private void SimplexNoise()
    {
        SDFShader.Dispatch(SDFShaderParameters.ChunkSize, SDFShaderParameters.Lod);
    }

    /// <summary>
    /// Prints out the sdf buffer
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public void PrintOutBuffer()
    {
        if (Rd == null)
        {
            throw new ArgumentNullException(nameof(Rd), "Cannot be null");
        }

        var outputBytes = Rd.BufferGetData(OutputBuffer);
        var output = new float[((SDFShaderParameters.ChunkSize / SDFShaderParameters.Lod) + 2) * ((SDFShaderParameters.ChunkSize / SDFShaderParameters.Lod) + 2) * ((SDFShaderParameters.ChunkSize / SDFShaderParameters.Lod) + 2)];
        Buffer.BlockCopy(outputBytes, 0, output, 0, output.Length * sizeof(float));
        GD.Print("Output: ", string.Join(", ", output));
        Console.WriteLine(string.Join(", ", output));
    }

    /// <summary>
    /// Frees all resources associated with the generator.
    /// </summary>
    public void Dispose()
    {
        // Free the shaders
        SDFShader.Dispose();

        Rd.FreeRid(SDFParametersBuffer);
        Rd.FreeRid(OutputBuffer);
        Rd.Free();
    }
}
