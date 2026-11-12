using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.CPU.Constant;

public sealed class ConstantParameters : ISDFParametersCPU
{
    public float Value { get; }
    public OperationType OperationType { get; }

    public ConstantParameters(float value, OperationType operationType)
    {
        Value = value;
        OperationType = operationType;
    }
}
