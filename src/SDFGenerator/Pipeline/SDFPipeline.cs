using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.Constant;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;
using TerrainGeneration.Utilities.EngineAbstractions;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline;

public sealed class SDFPipeline
{
    private RenderingDevice Rd;
    public IReadOnlyList<BiomeDescriptor> Biomes { get; }
    public TemperatureDescriptor Temperature { get; }

    /// <summary>
    /// Maps function name (e.g. "SimplexNoise") to a shader path.
    /// </summary>
    private readonly Dictionary<string, ISDFShader> SDFShadersByFunction = new(StringComparer.OrdinalIgnoreCase);

    // Used in dispatching the shaders
    SDFShaderParameters SDFShaderParameters;
    private ComputeBuffer SDFParametersBuffer;
    private ComputeBuffer OutputBuffer;
    private ComputeBuffer DummyBiomeParametersBuffer;
    private ComputeBuffer DummyTemperatureValuesBuffer;
    private ComputeBuffer TemperatureValueBuffer;

    public SDFPipeline(TemperatureDescriptor temperature, IReadOnlyList<BiomeDescriptor> biomes, IReadOnlyDictionary<string, string> functionShaderMap, RenderingDevice rd)
    {
        Rd = rd;
        Biomes = biomes ?? throw new ArgumentNullException("Biomes cannot be null.");
        Temperature = temperature ?? throw new ArgumentNullException("Temperature cannot be null");
        SetupSDFShaders(functionShaderMap, rd);
        SetupBiomeUniforms(biomes, rd);
        SetupDummyUniforms(rd);
    }

    private void SetupSDFShaders(IReadOnlyDictionary<string, string> functionShaderMap, RenderingDevice rd)
    {
        foreach (var kvp in functionShaderMap)
        {
            string functionName = kvp.Key;
            string shaderPath = kvp.Value;

            ISDFShader shader = functionName switch
            {
                SimplexNoiseStage.FunctionName => new SimplexNoiseShader(rd, shaderPath),
                ConstantStage.FunctionName => new ConstantShader(rd, shaderPath),
                _ => throw new NotSupportedException($"Unsupported shader function '{functionName}'.")
            };

            SDFShadersByFunction[functionName] = shader;
        }
    }

    public ComputeBuffer GetSDF(SDFShaderParameters sdfShaderParameters)
    {
        SetSDFParameters(sdfShaderParameters);
        SetOutputBuffer(sdfShaderParameters.ChunkSize, sdfShaderParameters.Lod);
        SetTemperatureValueBuffer(sdfShaderParameters.ChunkSize, sdfShaderParameters.Lod);

        if (OutputBuffer == null || SDFParametersBuffer == null)
        {
            throw new InvalidOperationException("Output buffer or SDF parameters buffer is not set.");
        }

        if (Temperature == null)
        {
            throw new InvalidOperationException("Temperature descriptor is not set.");
        }

        foreach (ISDFPipelineStage stage in Temperature.Sdfs)
        {
            if (!SDFShadersByFunction.TryGetValue(stage.Function, out ISDFShader sdfShader))
            {
                throw new InvalidOperationException($"No shader registered for pipeline function '{stage.Function}'.");
            }

            sdfShader.Dispatch(
                sdfShaderParameters.ChunkSize,
                sdfShaderParameters.Lod,
                stage.CreateShaderParameters(),
                SDFParametersBuffer,
                DummyBiomeParametersBuffer,
                DummyTemperatureValuesBuffer,
                TemperatureValueBuffer);

            float[]? data = TemperatureValueBuffer.GetData<float>();
            if (data != null)
            {
                var test = data;
            }
        }

        foreach (BiomeDescriptor biome in Biomes)
        {
            // Get the temperature buffer

            foreach (ISDFPipelineStage stage in biome.Sdfs)
            {
                if (!SDFShadersByFunction.TryGetValue(stage.Function, out ISDFShader sdfShader))
                {
                    throw new InvalidOperationException($"No shader registered for pipeline function '{stage.Function}'.");
                }

                sdfShader.Dispatch(
                    sdfShaderParameters.ChunkSize,
                    sdfShaderParameters.Lod,
                    stage.CreateShaderParameters(),
                    SDFParametersBuffer,
                    biome.BiomeParametersBuffer,
                    TemperatureValueBuffer,
                    OutputBuffer);
            }
        }

        return OutputBuffer;
    }

    /// <summary>
    /// Sets the parameters for the sdf shader params buffer
    /// </summary>
    /// <param name="parameters"></param>
    private void SetSDFParameters(SDFShaderParameters parameters)
    {
        if (!this.SDFShaderParameters.Equals(parameters))
        {
            // If the buffer isn't valid, we need to create one
            if (SDFParametersBuffer == null)
            {
                SDFParametersBuffer = new ComputeBuffer(Rd, (uint)Marshal.SizeOf<SDFShaderParameters>(), RenderingDevice.UniformType.UniformBuffer, 0);
            }

            SDFParametersBuffer.SetData(0, (uint)Marshal.SizeOf<SDFShaderParameters>(), parameters.ToByteArray());
            SDFShaderParameters = parameters;
        }
    }

    private void SetOutputBuffer(uint chunkSize, uint lod)
    {
        if (OutputBuffer == null)
        {
            uint chunkSizeToLodRatio = chunkSize / lod;
            OutputBuffer = new ComputeBuffer(Rd, (chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * sizeof(float), RenderingDevice.UniformType.StorageBuffer, 0);
        }
    }

    private void SetTemperatureValueBuffer(uint chunkSize, uint lod)
    {
        if (TemperatureValueBuffer == null)
        {
            uint chunkSizeToLodRatio = chunkSize / lod;
            TemperatureValueBuffer = new ComputeBuffer(Rd, (chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * sizeof(float), RenderingDevice.UniformType.StorageBuffer, 0);
        }
    }

    private void SetupBiomeUniforms(IReadOnlyList<BiomeDescriptor> biomes, RenderingDevice rd)
    {
        // Biome parameters are set in the shader dispatch loop, so we don't need to do anything here for now.
        foreach (BiomeDescriptor biome in biomes)
        {
            if (biome.BiomeParametersBuffer == null)
            {
                BiomeParameters biomeParams = new BiomeParameters(biome.Temperature, biome.TemperatureSpread, biome.Depth, biome.DepthSpread, biome.IgnoreBiome);
                biome.BiomeParametersBuffer = new ComputeBuffer(rd, (uint)Marshal.SizeOf<BiomeParameters>(), RenderingDevice.UniformType.UniformBuffer, 0, data: StructHelpers.ToByteArray(biomeParams));
            }
        }
    }

    private void SetupDummyUniforms(RenderingDevice rd)
    {
        BiomeParameters biomeParameters = new BiomeParameters(0f, 0f, 0f, 0f, true);
        DummyBiomeParametersBuffer = new ComputeBuffer(
            rd,
            (uint)Marshal.SizeOf<BiomeParameters>(),
            RenderingDevice.UniformType.UniformBuffer,
            0,
            StructHelpers.ToByteArray(biomeParameters));
        DummyTemperatureValuesBuffer = new ComputeBuffer(rd, sizeof(float), RenderingDevice.UniformType.StorageBuffer, 0);
    }

    public void Dispose()
    {
        SDFParametersBuffer.Dispose();
        TemperatureValueBuffer.Dispose();
        OutputBuffer.Dispose();
        DummyTemperatureValuesBuffer.Dispose();
        DummyBiomeParametersBuffer.Dispose();
        foreach (ISDFShader shader in SDFShadersByFunction.Values)
        {
            shader.Dispose();
        }
    }

    /// <summary>
    /// Prints out all of the normals in the normals buffer. Use for debugging only. VERY slow.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public void PrintOutBuffer(uint chunkSize, uint lod)
    {
        var outputBytes = TemperatureValueBuffer.GetData<byte>();
        float[] output = new float[(chunkSize / lod + 1) * (chunkSize / lod + 1) * (chunkSize / lod + 1)];
        Buffer.BlockCopy(outputBytes, 0, output, 0, output.Length * sizeof(float));
    }
}
