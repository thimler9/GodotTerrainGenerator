using System.Text.Json.Serialization;
using TerrainGeneration.Application.SDFGenerator.Constant;

namespace TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

public sealed class ConstantStage : ISDFPipelineStage
{
    public const string FunctionName = "Constant";

    public string Function => FunctionName;

    public float Value { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperationType OperationType { get; set; }

    public IShaderParameters CreateShaderParameters()
    {
        return new ConstantShaderParameters(this);
    }
}
