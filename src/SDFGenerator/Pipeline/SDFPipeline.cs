using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;
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
    RDUniform? SDFParametersUniform;
    Rid SDFParametersBuffer;

    RDUniform? OutputUniform;
    Rid OutputBuffer;

    RDUniform? DummyBiomeParametersUniform;
    Rid DummyBiomeParametersBuffer;

    RDUniform? DummyTemperatureValuesUniform;
    Rid DummyTemperatureValuesBuffer;

    RDUniform? TemperatureValueUniform;
    Rid TemperatureValueBuffer;

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
                _ => throw new NotSupportedException($"Unsupported shader function '{functionName}'.")
            };

            SDFShadersByFunction[functionName] = shader;
        }
    }

    public RDUniform GetSDF(SDFShaderParameters sdfShaderParameters)
    {
        SetSDFParameters(sdfShaderParameters);
        SetOutputBuffer(sdfShaderParameters.ChunkSize, sdfShaderParameters.Lod);
        SetTemperatureValueBuffer(sdfShaderParameters.ChunkSize, sdfShaderParameters.Lod);

        if (OutputUniform == null || SDFParametersUniform == null)
        {
            throw new InvalidOperationException("Output uniform or SDF parameters uniform is not set.");
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
                SDFParametersUniform,
                DummyBiomeParametersUniform,
                DummyTemperatureValuesUniform,
                TemperatureValueUniform);
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
                    SDFParametersUniform,
                    biome.BiomeParametersUniform,
                    TemperatureValueUniform,
                    OutputUniform);
            }
        }

        return OutputUniform;
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
            if (!SDFParametersBuffer.IsValid)
            {
                SDFParametersBuffer = Rd.UniformBufferCreate((uint)Marshal.SizeOf<SDFShaderParameters>());
                SDFParametersUniform = new RDUniform()
                {
                    UniformType = RenderingDevice.UniformType.UniformBuffer,
                    Binding = 0
                };
                SDFParametersUniform.AddId(SDFParametersBuffer);
            }

            Rd.BufferUpdate(SDFParametersBuffer, 0, (uint)Marshal.SizeOf<SDFShaderParameters>(), parameters.ToByteArray());
            SDFShaderParameters = parameters;
        }
    }

    private void SetOutputBuffer(uint chunkSize, uint lod)
    {
        if (!OutputBuffer.IsValid)
        {
            uint chunkSizeToLodRatio = chunkSize / lod;
            OutputBuffer = Rd.StorageBufferCreate((chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * sizeof(float));
            OutputUniform = new RDUniform()
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 0
            };
            OutputUniform.AddId(OutputBuffer);
        }
    }

    private void SetTemperatureValueBuffer(uint chunkSize, uint lod)
    {
        if (!TemperatureValueBuffer.IsValid)
        {
            uint chunkSizeToLodRatio = chunkSize / lod;
            TemperatureValueBuffer = Rd.StorageBufferCreate((chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * sizeof(float));
            TemperatureValueUniform = new RDUniform()
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 0
            };
            TemperatureValueUniform.AddId(TemperatureValueBuffer);
        }
    }

    private void SetupBiomeUniforms(IReadOnlyList<BiomeDescriptor> biomes, RenderingDevice rd)
    {
        // Biome parameters are set in the shader dispatch loop, so we don't need to do anything here for now.
        foreach (BiomeDescriptor biome in biomes)
        {
            if (!biome.BiomeParametersBuffer.IsValid)
            {
                BiomeParameters biomeParams = new BiomeParameters(biome.Temperature, biome.TemperatureSpread, biome.Depth, biome.DepthSpread, biome.IgnoreBiome);

                biome.BiomeParametersBuffer = rd.UniformBufferCreate((uint)Marshal.SizeOf<SimplexNoiseShaderParameters>());
                biome.BiomeParametersUniform = new RDUniform()
                {
                    UniformType = RenderingDevice.UniformType.UniformBuffer,
                    Binding = 0
                };
                biome.BiomeParametersUniform.AddId(biome.BiomeParametersBuffer);
            }
        }
    }

    private void SetupDummyUniforms(RenderingDevice rd)
    {
        BiomeParameters biomeParameters = new BiomeParameters(0f, 0f, 0f, 0f, true);
        DummyBiomeParametersBuffer = rd.UniformBufferCreate((uint)Marshal.SizeOf<BiomeParameters>(), StructHelpers.ToByteArray(biomeParameters));
        DummyBiomeParametersUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0
        };
        DummyBiomeParametersUniform.AddId(DummyBiomeParametersBuffer);

        DummyTemperatureValuesBuffer = rd.StorageBufferCreate(sizeof(float));
        DummyTemperatureValuesUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        DummyTemperatureValuesUniform.AddId(DummyTemperatureValuesBuffer);
    }

    public void Dispose()
    {
        Rd.FreeRid(SDFParametersBuffer);
        Rd.FreeRid(TemperatureValueBuffer);
        Rd.FreeRid(OutputBuffer);
        Rd.FreeRid(DummyBiomeParametersBuffer);
        Rd.FreeRid(DummyTemperatureValuesBuffer);
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
        var outputBytes = Rd.BufferGetData(TemperatureValueBuffer);
        float[] output = new float[(chunkSize / lod + 1) * (chunkSize / lod + 1) * (chunkSize / lod + 1)];
        Buffer.BlockCopy(outputBytes, 0, output, 0, output.Length * sizeof(float));
    }
}
