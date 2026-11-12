using System.Text.Json.Serialization;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.CPU.Constant;

public sealed class ConstantStage : ISDFPipelineStageCPU
{
    public const string FunctionName = "Constant";

    public string Function => FunctionName;

    public float Value { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperationType OperationType { get; set; }

    public ICpuSdfParameters CreateParameters()
    {
        return new ConstantParameters(Value, OperationType);
    }
}
