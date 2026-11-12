using Godot;
using System.Text.Json.Serialization;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.GPU.Constant;

namespace TerrainGeneration.Application.SDFGenerator.GPU.Sphere;

public sealed class SphereStage : ISDFPipelineStageGPU
{
    public const string FunctionName = "Sphere";

    public string Function => FunctionName;

    public float Radius { get; init; }
    public Vector3 Center { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperationType OperationType { get; init; }

    public ISDFShaderParameters CreateShaderParameters()
    {
        return new SphereShaderParameters(this);
    }
}
