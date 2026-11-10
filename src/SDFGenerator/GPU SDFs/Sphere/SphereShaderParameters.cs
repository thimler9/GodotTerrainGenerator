using Godot;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.GPU.Constant;

namespace TerrainGeneration.Application.SDFGenerator.GPU.Sphere;

[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct SphereShaderParameters : ISDFShaderParameters
{
    [FieldOffset(0)]
    private float Radius;

    [FieldOffset(4)]
    private Vector3 RadiusPadding;

    [FieldOffset(16)]
    private Vector3 Center;

    [FieldOffset(28)]
    private OperationType OperationType;

    public SphereShaderParameters()
    {
        Radius = 0;
        RadiusPadding = Vector3.Zero;
        Center = Vector3.Zero;
        OperationType = OperationType.Set;
    }

    public SphereShaderParameters(SphereStage stage)
    {
        Radius = stage.Radius;
        RadiusPadding = Vector3.Zero;
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
