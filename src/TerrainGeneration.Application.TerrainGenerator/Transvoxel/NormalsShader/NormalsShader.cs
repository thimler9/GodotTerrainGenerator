using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel.NormalsShader;
public class NormalsShader
{
    private RenderingDevice Rd;
    private string ShaderPath;
    private Rid Shader;
    private Rid Pipeline;
    
    private NormalsShaderParameters? Parameters = null;
    private Rid ParametersBuffer;
    private Rid ParametersUniformSet;

    //private SimplexNoiseShaderParameters? SimplexNoiseParameters = null;
    //private Rid SimplexNoiseParametersBuffer;
    //private Rid SimplexNoiseParametersUniformSet;

    // We keep the buffer in the shader since the same buffer is used everytime
    private Rid OutputNormalsBuffer;
    public RDUniform OutputNormalsUniform { get; }
    private Rid OutputNormalsUniformSet;

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
        ShaderPath = descriptor.ShaderPath;
        RDShaderFile shaderFile = GD.Load<RDShaderFile>(descriptor.ShaderPath);
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Shader = rd.ShaderCreateFromSpirV(shaderBytecode);
        Pipeline = rd.ComputePipelineCreate(Shader);

        // Set Paramters
        Parameters = descriptor.Parameters;
        byte[] parameterBytes = StructHelpers.ToByteArray(descriptor.Parameters);
        ParametersBuffer = rd.UniformBufferCreate((uint)Marshal.SizeOf<NormalsShaderParameters>(), parameterBytes);
        RDUniform parametersUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        parametersUniform.AddId(ParametersBuffer);

        //SimplexNoiseParameters = descriptor.SimplexNoiseParameters;
        //byte[] sdfParameterBytes = StructHelpers.ToByteArray(descriptor.SimplexNoiseParameters);
        //SimplexNoiseParametersBuffer = rd.UniformBufferCreate((uint)Marshal.SizeOf<SimplexNoiseShaderParameters>(), sdfParameterBytes);
        //RDUniform sdfParametersUniform = new RDUniform()
        //{
        //    UniformType = RenderingDevice.UniformType.UniformBuffer,
        //    Binding = 0
        //};
        //sdfParametersUniform.AddId(SimplexNoiseParametersBuffer);

        // Create the output buffer used throughout calculations
        uint chunkSizeToLodRatio = descriptor.Parameters.ChunkSize / descriptor.Parameters.Lod;
        if (chunkSizeToLodRatio == 0)
        {
            throw new ArgumentException($"{nameof(descriptor.Parameters.ChunkSize)} / {nameof(descriptor.Parameters.Lod)} must be greater than 0");
        }

        OutputNormalsBuffer = rd.StorageBufferCreate((chunkSizeToLodRatio + 1) * (chunkSizeToLodRatio + 1) * (chunkSizeToLodRatio + 1) * sizeof(float) * 3);
        OutputNormalsUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        OutputNormalsUniform.AddId(OutputNormalsBuffer);

        ParametersUniformSet = rd.UniformSetCreate([parametersUniform], Shader, 0);
        OutputNormalsUniformSet = rd.UniformSetCreate([OutputNormalsUniform], Shader, 2);
        //SimplexNoiseParametersUniformSet = rd.UniformSetCreate([sdfParametersUniform], Shader, 3);
    }

    /// <summary>
    /// Updates the paramters in the normals shader parameters buffer
    /// </summary>
    /// <param name="parameters"></param>
    private void SetParameters(NormalsShaderParameters parameters)
    {
        if (!Parameters.Equals(parameters))
        {
            Rd.BufferUpdate(ParametersBuffer, 0, (uint)Marshal.SizeOf<NormalsShaderParameters>(), StructHelpers.ToByteArray(parameters));
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Dispatches the shader; creates a buffer with the normals for the sdf values.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="inputSDFUniform"></param>
    public void Dispatch(NormalsShaderParameters parameters, RDUniform inputSDFUniform)
    {
        SetParameters(parameters);

        // No reason to run if parameters haven't been changed
        // Run the shaders
        RunNormalsShader(inputSDFUniform);
    }

    /// <summary>
    /// Runs the normals shader
    /// </summary>
    /// <param name="computeList"></param>
    /// <param name="inputSDFUniform"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    private void RunNormalsShader(RDUniform inputSDFUniform)
    {
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

        Rid inputSDFUniformSet = Rd.UniformSetCreate([inputSDFUniform], Shader, 1);

        long computeList = Rd.ComputeListBegin();
        Rd.ComputeListBindComputePipeline(computeList, Pipeline);
        Rd.ComputeListBindUniformSet(computeList, ParametersUniformSet, 0);
        Rd.ComputeListBindUniformSet(computeList, inputSDFUniformSet, 1);
        Rd.ComputeListBindUniformSet(computeList, OutputNormalsUniformSet, 2);
        //Rd.ComputeListBindUniformSet(computeList, SimplexNoiseParametersUniformSet, 3);
        Rd.ComputeListDispatch(computeList, xGroups: chunkSize / (8 * lod) + 1, yGroups: chunkSize / (8 * lod) + 1, zGroups: chunkSize / (8 * lod) + 1);
        Rd.ComputeListEnd();

        Rd.FreeRid(inputSDFUniformSet);
    }

    /// <summary>
    /// Disposes the resources associated with the normals shader
    /// </summary>
    public void Dispose()
    {
        // Free the shaders
        Rd.FreeRid(Pipeline);
        Rd.FreeRid(ParametersUniformSet);
        Rd.FreeRid(ParametersBuffer);
        Rd.FreeRid(OutputNormalsUniformSet);
        Rd.FreeRid(Shader);
        Rd.FreeRid(OutputNormalsBuffer);
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

        var outputBytes = Rd.BufferGetData(OutputNormalsBuffer);
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
