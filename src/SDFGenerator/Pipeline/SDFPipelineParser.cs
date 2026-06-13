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

            if (!root.TryGetProperty("pipeline", out JsonElement pipelineElement))
            {
                throw new SDFPipelineParseException("JSON must contain a top-level 'pipeline' array.");
            }

            if (pipelineElement.ValueKind != JsonValueKind.Array)
            {
                throw new SDFPipelineParseException("The 'pipeline' field must be an array.");
            }

            var stages = new List<ISDFPipelineStage>();
            foreach (JsonElement stageElement in pipelineElement.EnumerateArray())
            {
                stages.Add(ParseStage(stageElement));
            }

            // Parse optional top-level shaders map: { "FunctionName": "shader/path.glsl" }
            Dictionary<string, string> shaderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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

            return new SDFPipeline(stages, shaderMap, rd);
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
        var descriptor = new SimplexNoiseStage();

        descriptor = JsonSerializer.Deserialize<SimplexNoiseStage>(stageElement.GetRawText(), SerializerOptions)
            ?? throw new SDFPipelineParseException("Unable to deserialize SimplexNoise stage.");

        return descriptor;
    }
}
