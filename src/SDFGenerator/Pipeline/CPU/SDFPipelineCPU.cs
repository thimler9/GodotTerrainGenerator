using System;
using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;

public sealed class SDFPipelineCPU
{
    private readonly Dictionary<string, ICpuSdfFunction> sdfsByFunction = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<BiomeDescriptorCPU> Biomes { get; }
    public TemperatureDescriptorCPU Temperature { get; }

    public SDFPipelineCPU(
        TemperatureDescriptorCPU temperature,
        IReadOnlyList<BiomeDescriptorCPU> biomes,
        IReadOnlyDictionary<string, SDFFunctionConfiguration> functionMap)
    {
        Temperature = temperature ?? throw new ArgumentNullException(nameof(temperature));
        Biomes = biomes ?? throw new ArgumentNullException(nameof(biomes));
        ArgumentNullException.ThrowIfNull(functionMap);

        IReadOnlyDictionary<string, ICpuSdfFunction> registeredFunctions = DiscoverSdfs();
        foreach (KeyValuePair<string, SDFFunctionConfiguration> function in functionMap)
        {
            string cpuFunctionName = function.Value.CPUFunction
                ?? throw new ArgumentException($"No CPU function configured for '{function.Key}'.", nameof(functionMap));

            if (!registeredFunctions.TryGetValue(cpuFunctionName, out ICpuSdfFunction? cpuFunction))
            {
                throw new NotSupportedException($"CPU function '{cpuFunctionName}' configured for '{function.Key}' is not registered.");
            }

            sdfsByFunction.Add(function.Key, cpuFunction);
        }
    }

    public float[] GetSDF(Vector3 chunkOffset, uint chunkSize, uint lod)
    {
        var sdfParameters = new SDFParametersCPU(chunkOffset, chunkSize, lod);

        int length = (int)sdfParameters.ChunkSize * (int)sdfParameters.Lod;
        // TODO THIS SHOULD NOT BE MADE EVERY CALL
        float[] temperatureValues = new float[length];

        foreach (ISDFPipelineStageCPU stage in Temperature.Sdfs)
        {
            temperatureValues = Dispatch(stage, sdfParameters, temperatureValues);
        }

        // TODO THIS SHOULD NOT BE MADE EVERY CALL
        float[] output = new float[length];
        foreach (BiomeDescriptorCPU biome in Biomes)
        {
            var biomeParameters = new BiomeParametersCPU(
                biome.Temperature,
                biome.TemperatureSpread,
                biome.Depth,
                biome.DepthSpread,
                biome.IgnoreBiome);
            foreach (ISDFPipelineStageCPU stage in biome.Sdfs)
            {
                output = Dispatch(stage, sdfParameters, biomeParameters, temperatureValues);
            }
        }

        return output;
    }

    private float[] Dispatch(ISDFPipelineStageCPU stage, SDFParametersCPU sdfParameters, float[] temperatureValues)
    {
        return Dispatch(stage, sdfParameters, default, temperatureValues);
    }

    private float[] Dispatch(ISDFPipelineStageCPU stage, SDFParametersCPU sdfParameters, BiomeParametersCPU biomeParameters, float[] temperatureValues)
    {
        if (!sdfsByFunction.TryGetValue(stage.Function, out ICpuSdfFunction? sdfFunction))
        {
            throw new InvalidOperationException($"No CPU SDF registered for pipeline function '{stage.Function}'.");
        }

        sdfFunction.Dispatch(stage.CreateParameters(), sdfParameters, biomeParameters, temperatureValues, out float[] output);
        return output ?? throw new InvalidOperationException($"CPU SDF '{stage.Function}' did not provide an output buffer.");
    }

    private static IReadOnlyDictionary<string, ICpuSdfFunction> DiscoverSdfs()
    {
        var sdfs = new Dictionary<string, ICpuSdfFunction>(StringComparer.OrdinalIgnoreCase);
        Assembly assembly = typeof(SDFPipelineCPU).Assembly;

        foreach (Type type in assembly.GetTypes())
        {
            if (!typeof(ICpuSdfFunction).IsAssignableFrom(type) || !type.IsClass || type.IsAbstract)
            {
                continue;
            }

            if (Activator.CreateInstance(type) is not ICpuSdfFunction sdf)
            {
                throw new InvalidOperationException($"CPU SDF type '{type.FullName}' must have a public parameterless constructor.");
            }

            if (string.IsNullOrWhiteSpace(sdf.FunctionName))
            {
                throw new InvalidOperationException($"CPU SDF type '{type.FullName}' must provide a non-empty function name.");
            }

            if (!sdfs.TryAdd(sdf.FunctionName, sdf))
            {
                throw new InvalidOperationException($"Duplicate CPU SDF function name '{sdf.FunctionName}'.");
            }
        }

        return sdfs;
    }
}
