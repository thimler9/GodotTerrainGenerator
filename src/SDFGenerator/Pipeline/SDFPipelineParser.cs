using Godot;
using System.Text.Json;
using System.Reflection;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using FileAccess = Godot.FileAccess;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline;

public sealed class SDFPipelineParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    // Stage types are discovered from the assembly so adding a new stage does not
    // require another deserialization method or switch case in this parser.
    private static readonly IReadOnlyDictionary<string, Type> GpuStageTypes = DiscoverGpuStageTypes();
    private static readonly IReadOnlyDictionary<string, Type> CpuStageTypes = DiscoverCpuStageTypes();

    /// <summary>
    /// Parses pipeline JSON and creates the pipeline, including its configured stages and shaders.
    /// </summary>
    public SDFPipeline Parse(string json, RenderingDevice rd)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON content cannot be null or whitespace.", nameof(json));
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            TemperatureDescriptor temperature = ParseTemperature(root);
            IReadOnlyList<BiomeDescriptor> biomes = ParseBiomes(root);
            IReadOnlyDictionary<string, SDFFunctionConfiguration> functionMap = ParseSDFFunctions(root);

            SDFPipelineGPU sDFPipelineGPU = new SDFPipelineGPU(temperature, biomes, functionMap, rd);
            SDFPipelineCPU sDFPipelineCPU = new SDFPipelineCPU(temperature, biomes, functionMap);

            return new SDFPipeline()
            {
                gPUSDFPipeline = sDFPipelineGPU,
                cPUSDFPipeline = sDFPipelineCPU
            };
        }
        catch (JsonException ex)
        {
            throw new SDFPipelineParseException("Failed to parse pipeline JSON.", ex);
        }
    }

    public SDFPipeline ParseFromFile(string filePath, RenderingDevice rd)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        }

        string json = ReadJsonFromPath(filePath);
        return Parse(json, rd);
    }

    private string ReadJsonFromPath(string filePath)
    {
        if (filePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            using FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            return file.GetAsText();
        }

        return System.IO.File.ReadAllText(filePath);
    }

    private TemperatureDescriptor ParseTemperature(JsonElement root)
    {
        if (!root.TryGetProperty("temperature", out JsonElement temperatureElement))
        {
            return new TemperatureDescriptor();
        }

        if (temperatureElement.ValueKind != JsonValueKind.Object)
        {
            throw new SDFPipelineParseException("The 'temperature' section must be an object.");
        }

        if (!temperatureElement.TryGetProperty("sdf", out JsonElement sdfElement) || sdfElement.ValueKind != JsonValueKind.Array)
        {
            throw new SDFPipelineParseException("The 'temperature' section must contain an 'sdf' array.");
        }

        return new TemperatureDescriptor
        {
            Sdfs = ParseGpuSdfStages(sdfElement),
            CpuSdfs = ParseCpuSdfStages(sdfElement)
        };
    }

    private IReadOnlyList<BiomeDescriptor> ParseBiomes(JsonElement root)
    {
        if (!root.TryGetProperty("biomes", out JsonElement biomesElement))
        {
            return Array.Empty<BiomeDescriptor>();
        }

        if (biomesElement.ValueKind != JsonValueKind.Array)
        {
            throw new SDFPipelineParseException("The 'biomes' field must be an array.");
        }

        var biomes = new List<BiomeDescriptor>();
        foreach (JsonElement biomeElement in biomesElement.EnumerateArray())
        {
            biomes.Add(ParseBiome(biomeElement));
        }

        return biomes;
    }

    private IReadOnlyDictionary<string, SDFFunctionConfiguration> ParseSDFFunctions(JsonElement root)
    {
        if (!root.TryGetProperty("sDFFunction", out JsonElement functionsElement)
            || functionsElement.ValueKind != JsonValueKind.Object)
        {
            throw new SDFPipelineParseException("The 'sDFFunction' section must be an object.");
        }

        var functionMap = new Dictionary<string, SDFFunctionConfiguration>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in functionsElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                throw new SDFPipelineParseException($"SDF function '{property.Name}' configuration must be an object.");
            }

            string? gpuShader = null;
            if (property.Value.TryGetProperty("gpuShader", out JsonElement gpuShaderElement)
                && gpuShaderElement.ValueKind == JsonValueKind.String)
            {
                gpuShader = gpuShaderElement.GetString();
            }

            string? cpuFunction = null;
            if (property.Value.TryGetProperty("cpuFunction", out JsonElement cpuFunctionElement)
                && cpuFunctionElement.ValueKind == JsonValueKind.String)
            {
                cpuFunction = cpuFunctionElement.GetString();
            }

            functionMap.Add(property.Name, new SDFFunctionConfiguration
            {
                GPUShader = gpuShader,
                CPUFunction = cpuFunction
            });
        }

        return functionMap;
    }

    private BiomeDescriptor ParseBiome(JsonElement biomeElement)
    {
        if (biomeElement.ValueKind != JsonValueKind.Object)
        {
            throw new SDFPipelineParseException("Each biome must be a JSON object.");
        }

        if (!biomeElement.TryGetProperty("temperature", out JsonElement temperatureElement)
            || temperatureElement.ValueKind != JsonValueKind.Number)
        {
            throw new SDFPipelineParseException("Biome must contain a numeric 'temperature' field.");
        }

        if (!biomeElement.TryGetProperty("temperatureSpread", out JsonElement temperatureSpreadElement)
            || temperatureSpreadElement.ValueKind != JsonValueKind.Number)
        {
            throw new SDFPipelineParseException("Biome must contain a numeric 'temperatureSpread' field.");
        }

        if (!biomeElement.TryGetProperty("depth", out JsonElement depthElement)
            || depthElement.ValueKind != JsonValueKind.Number)
        {
            throw new SDFPipelineParseException("Biome must contain a numeric 'depth' field.");
        }

        if (!biomeElement.TryGetProperty("depthSpread", out JsonElement depthSpreadElement)
            || depthSpreadElement.ValueKind != JsonValueKind.Number)
        {
            throw new SDFPipelineParseException("Biome must contain a numeric 'depthSpread' field.");
        }

        bool ignoreBiome = false;
        if (biomeElement.TryGetProperty("ignoreBiome", out JsonElement ignoreBiomeElement))
        {
            if (ignoreBiomeElement.ValueKind != JsonValueKind.True && ignoreBiomeElement.ValueKind != JsonValueKind.False)
            {
                throw new SDFPipelineParseException("Biome 'ignoreBiome' field must be a boolean.");
            }

            ignoreBiome = ignoreBiomeElement.GetBoolean();
        }

        if (!biomeElement.TryGetProperty("sdf", out JsonElement sdfElement) || sdfElement.ValueKind != JsonValueKind.Array)
        {
            throw new SDFPipelineParseException("Biome must contain an 'sdf' array with sdf function inputs.");
        }

        return new BiomeDescriptor
        {
            Temperature = temperatureElement.GetSingle(),
            TemperatureSpread = temperatureSpreadElement.GetSingle(),
            Depth = depthElement.GetSingle(),
            DepthSpread = depthSpreadElement.GetSingle(),
            IgnoreBiome = ignoreBiome,
            Sdfs = ParseSdfStages<ISDFPipelineStageGPU>(sdfElement, GpuStageTypes),
            CpuSdfs = ParseCpuSdfStages(sdfElement)
        };
    }

    private IReadOnlyList<ISDFPipelineStageGPU> ParseGpuSdfStages(JsonElement sdfElement)
    {
        return ParseSdfStages<ISDFPipelineStageGPU>(sdfElement, GpuStageTypes);
    }

    private IReadOnlyList<ISDFPipelineStageCPU> ParseCpuSdfStages(JsonElement sdfElement)
    {
        return ParseSdfStages<ISDFPipelineStageCPU>(sdfElement, CpuStageTypes);
    }

    private IReadOnlyList<TStage> ParseSdfStages<TStage>(JsonElement sdfElement, IReadOnlyDictionary<string, Type> stageTypes)
        where TStage : class
    {
        var stages = new List<TStage>();
        foreach (JsonElement stageElement in sdfElement.EnumerateArray())
        {
            stages.Add(ParseStage<TStage>(stageElement, stageTypes));
        }

        return stages;
    }

    private TStage ParseStage<TStage>(JsonElement stageElement, IReadOnlyDictionary<string, Type> stageTypes)
        where TStage : class
    {
        if (stageElement.ValueKind != JsonValueKind.Object)
        {
            throw new SDFPipelineParseException("Each pipeline stage must be a JSON object.");
        }

        if (!stageElement.TryGetProperty("function", out JsonElement functionElement))
        {
            throw new SDFPipelineParseException("Pipeline stage is missing required 'function' field.");
        }

        string function = functionElement.GetString() ?? string.Empty;
        if (function.Length == 0)
        {
            throw new SDFPipelineParseException("Pipeline stage 'function' field cannot be empty.");
        }

        if (!stageTypes.TryGetValue(function, out Type? stageType))
        {
            throw new SDFPipelineParseException($"Unsupported pipeline function '{function}'.");
        }

        object? stage = JsonSerializer.Deserialize(stageElement.GetRawText(), stageType, SerializerOptions);
        if (stage is TStage typedStage)
        {
            return typedStage;
        }

        throw new SDFPipelineParseException($"Failed to deserialize pipeline stage for function '{function}'.");
    }

    private static IReadOnlyDictionary<string, Type> DiscoverGpuStageTypes()
    {
        var stageTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        Assembly assembly = typeof(SDFPipelineParser).Assembly;

        // FunctionName is the registration contract shared by all pipeline stages.
        foreach (Type type in assembly.GetTypes())
        {
            if (!typeof(ISDFPipelineStageGPU).IsAssignableFrom(type) || !type.IsClass || type.IsAbstract)
            {
                continue;
            }

            FieldInfo? functionNameField = type.GetField("FunctionName", BindingFlags.Public | BindingFlags.Static);
            string? functionName = functionNameField?.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                continue;
            }

            if (!stageTypes.TryAdd(functionName, type))
            {
                throw new InvalidOperationException($"Duplicate SDF pipeline function name '{functionName}'.");
            }
        }

        return stageTypes;
    }

    private static IReadOnlyDictionary<string, Type> DiscoverCpuStageTypes()
    {
        var stageTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        Assembly assembly = typeof(SDFPipelineParser).Assembly;

        foreach (Type type in assembly.GetTypes())
        {
            if (!typeof(ISDFPipelineStageCPU).IsAssignableFrom(type) || !type.IsClass || type.IsAbstract)
            {
                continue;
            }

            FieldInfo? functionNameField = type.GetField("FunctionName", BindingFlags.Public | BindingFlags.Static);
            string? functionName = functionNameField?.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                continue;
            }

            if (!stageTypes.TryAdd(functionName, type))
            {
                throw new InvalidOperationException($"Duplicate CPU pipeline function name '{functionName}'.");
            }
        }

        return stageTypes;
    }
}
