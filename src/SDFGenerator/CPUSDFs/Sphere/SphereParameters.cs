using Godot;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;

namespace TerrainGeneration.Application.SDFGenerator.CPU.Sphere;

public sealed class SphereParameters : ISDFParametersCPU
{
    public float Radius { get; }
    public Vector3 Center { get; }
    public OperationType OperationType { get; }

    public SphereParameters(float radius, Vector3 center, OperationType operationType)
    {
        Radius = radius;
        Center = center;
        OperationType = operationType;
    }
}
