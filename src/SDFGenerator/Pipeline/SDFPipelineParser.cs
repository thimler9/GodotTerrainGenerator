using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
            IReadOnlyDictionary<string, string> shaderMap = ParseShaders(root);

            return new SDFPipeline(temperature, biomes, shaderMap, rd);
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

    private static string ReadJsonFromPath(string filePath)
    {
        if (filePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            using FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            return file.GetAsText();
        }

        return System.IO.File.ReadAllText(filePath);
    }

    private static TemperatureDescriptor ParseTemperature(JsonElement root)
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
            Sdfs = ParseSdfStages(sdfElement)
        };
    }

    private static IReadOnlyList<BiomeDescriptor> ParseBiomes(JsonElement root)
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

    private static IReadOnlyDictionary<string, string> ParseShaders(JsonElement root)
    {
        var shaderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("shaders", out JsonElement shadersElement) && shadersElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in shadersElement.EnumerateObject())
            {
                string? path = prop.Value.GetString();
                if (!string.IsNullOrWhiteSpace(path))
                {
                    shaderMap[prop.Name] = path!;
                }
            }
        }

        return shaderMap;
    }

    private static BiomeDescriptor ParseBiome(JsonElement biomeElement)
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
            Sdfs = ParseSdfStages(sdfElement)
        };
    }

    private static IReadOnlyList<ISDFPipelineStage> ParseSdfStages(JsonElement sdfElement)
    {
        var stages = new List<ISDFPipelineStage>();
        foreach (JsonElement stageElement in sdfElement.EnumerateArray())
        {
            stages.Add(ParseStage(stageElement));
        }

        return stages;
    }

    private static ISDFPipelineStage ParseStage(JsonElement stageElement)
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
        if (function == string.Empty)
        {
            throw new SDFPipelineParseException("Pipeline stage 'function' field cannot be empty.");
        }

        return function switch
        {
            SimplexNoiseStage.FunctionName => ParseSimplexNoiseStage(stageElement),
            _ => throw new SDFPipelineParseException($"Unsupported pipeline function '{function}'.")
        };
    }

    private static ISDFPipelineStage ParseSimplexNoiseStage(JsonElement stageElement)
    {
        return JsonSerializer.Deserialize<SimplexNoiseStage>(stageElement.GetRawText(), SerializerOptions)
            ?? throw new SDFPipelineParseException("Unable to deserialize SimplexNoise stage.");
    }
}
