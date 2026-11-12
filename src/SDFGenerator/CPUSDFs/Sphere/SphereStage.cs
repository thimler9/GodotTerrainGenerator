using System.Text.Json.Serialization;
using Godot;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;

namespace TerrainGeneration.Application.SDFGenerator.CPU.Sphere;

public sealed class SphereStage : ISDFPipelineStageCPU
{
    public const string FunctionName = "Sphere";

    public string Function => FunctionName;

    public float Radius { get; set; }
    public Vector3 Center { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperationType OperationType { get; set; }

    public ISDFParametersCPU CreateParameters()
    {
        return new SphereParameters(Radius, Center, OperationType);
    }
}
