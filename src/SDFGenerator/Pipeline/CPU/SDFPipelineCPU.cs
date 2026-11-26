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
    private readonly Dictionary<string, ISDFFunctionCPU> sdfsByFunction = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<BiomeDescriptorCPU> Biomes { get; }
    public TemperatureDescriptorCPU Temperature { get; }


    private float[] Output;
    private float[] TemperatureValues;

    // These are used to get the temperature values since they don't depend on biome and temperature values
    private float[] DummyTemperatureValues = new float[1];
    private BiomeParametersCPU DummyBiomeParameters = new BiomeParametersCPU(0f, 0f, 0f, 0f, true);

    public SDFPipelineCPU(
        TemperatureDescriptorCPU temperature,
        IReadOnlyList<BiomeDescriptorCPU> biomes,
        IReadOnlyDictionary<string, SDFFunctionConfiguration> functionMap)
    {
        Temperature = temperature ?? throw new ArgumentNullException(nameof(temperature));
        Biomes = biomes ?? throw new ArgumentNullException(nameof(biomes));
        ArgumentNullException.ThrowIfNull(functionMap);

        IReadOnlyDictionary<string, ISDFFunctionCPU> registeredFunctions = DiscoverSdfs();
        foreach (KeyValuePair<string, SDFFunctionConfiguration> function in functionMap)
        {
            string cpuFunctionName = function.Value.CPUFunction
                ?? throw new ArgumentException($"No CPU function configured for '{function.Key}'.", nameof(functionMap));

            if (!registeredFunctions.TryGetValue(cpuFunctionName, out ISDFFunctionCPU? cpuFunction))
            {
                throw new NotSupportedException($"CPU function '{cpuFunctionName}' configured for '{function.Key}' is not registered.");
            }

            sdfsByFunction.Add(function.Key, cpuFunction);
        }
    }

    /// <summary>
    /// Gets the signed distance field (SDF) for the specified chunk offset, size, and level of detail (LOD).
    /// This SDF is a reference, so modifying it will modify the internal state of the SDFPipelineCPU instance. If you need to modify the SDF, make a copy of the returned array.
    /// </summary>
    /// <param name="chunkOffset"></param>
    /// <param name="chunkSize"></param>
    /// <param name="lod"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public float[] GetSDF(Vector3 chunkOffset, uint chunkSize, uint lod)
    {
        SDFParametersCPU sdfParameters = new SDFParametersCPU(chunkOffset, chunkSize, lod);
        uint length = sdfParameters.ChunkSize / sdfParameters.Lod + 2;
        
        // Initialize output if it isn't already
        if (Output == null)
        {
            Output = new float[length * length * length];
        }

        // Initialize temperatureValues if it isn't already
        if (TemperatureValues == null)
        {
            TemperatureValues = new float[length * length * length];
        }

        foreach (ISDFPipelineStageCPU stage in Temperature.Sdfs)
        {
            if (!sdfsByFunction.TryGetValue(stage.Function, out ISDFFunctionCPU? sdfFunction))
            {
                throw new InvalidOperationException($"No CPU SDF registered for pipeline function '{stage.Function}'.");
            }

            sdfFunction.Dispatch(
                stage.CreateParameters(),
                sdfParameters,
                DummyBiomeParameters,
                DummyTemperatureValues,
                TemperatureValues
            );
        }

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
                if (!sdfsByFunction.TryGetValue(stage.Function, out ISDFFunctionCPU? sdfFunction))
                {
                    throw new InvalidOperationException($"No CPU SDF registered for pipeline function '{stage.Function}'.");
                }

                sdfFunction.Dispatch(stage.CreateParameters(), sdfParameters, biomeParameters, TemperatureValues, Output);
            }
        }

        return Output;
    }

    private static IReadOnlyDictionary<string, ISDFFunctionCPU> DiscoverSdfs()
    {
        var sdfs = new Dictionary<string, ISDFFunctionCPU>(StringComparer.OrdinalIgnoreCase);
        Assembly assembly = typeof(SDFPipelineCPU).Assembly;

        foreach (Type type in assembly.GetTypes())
        {
            if (!typeof(ISDFFunctionCPU).IsAssignableFrom(type) || !type.IsClass || type.IsAbstract)
            {
                continue;
            }

            if (Activator.CreateInstance(type) is not ISDFFunctionCPU sdf)
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
