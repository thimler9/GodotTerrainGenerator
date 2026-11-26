using Godot;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.GPU.Constant;

namespace TerrainGeneration.Application.SDFGenerator.GPU.Sphere;

[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct SphereShaderParameters : ISDFShaderParameters
{
    [FieldOffset(0)]
    private Vector3 Center;

    [FieldOffset(12)]
    private OperationType OperationType;
    
    [FieldOffset(16)]
    private float Radius;

    public SphereShaderParameters()
    {
        Radius = 0;
        Center = Vector3.Zero;
        OperationType = OperationType.Set;
    }

    public SphereShaderParameters(SphereStage stage)
    {
        Radius = stage.Radius;
        Center = stage.Center;
        OperationType = stage.OperationType;
    }

    public override bool Equals(object? obj)
    {
        return obj is SphereShaderParameters other
            && Radius == other.Radius
            && Center == other.Center
            && OperationType == other.OperationType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Radius, Center, OperationType);
    }

    public static bool operator ==(SphereShaderParameters left, SphereShaderParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SphereShaderParameters left, SphereShaderParameters right)
    {
        return !(left == right);
    }
}
