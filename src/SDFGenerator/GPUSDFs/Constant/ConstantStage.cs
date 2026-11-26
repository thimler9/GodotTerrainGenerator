using System.Text.Json.Serialization;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.GPU.Constant;

public sealed class ConstantStage : ISDFPipelineStageGPU
{
    public const string FunctionName = "Constant";

    public string Function => FunctionName;

    public float Value { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperationType OperationType { get; set; }

    public ISDFShaderParameters CreateShaderParameters()
    {
        return new ConstantShaderParameters(this);
    }
}
