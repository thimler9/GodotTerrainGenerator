using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Godot;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Utilities.Math.Extensions;

namespace TerrainGeneration.Application.SDFGenerator.Constant;

public enum OperationType : uint
{
    Add = 0,
    Subtract = 1,
    Multiply = 2,
    Divide = 3,
    Set = 4,
}

[StructLayout(LayoutKind.Explicit)]
public struct ConstantShaderParameters : IShaderParameters
{

    [FieldOffset(0)]
    private float Value;

    [FieldOffset(4)]
    private OperationType OperationType;

    [FieldOffset(8)]
    private Vector2 Padding;

    public ConstantShaderParameters()
    {

    }

    public ConstantShaderParameters(ConstantStage stage)
    {
        Value = stage.Value;
        OperationType = stage.OperationType;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is ConstantShaderParameters))
        {
            return false;
        }

        ConstantShaderParameters other = (ConstantShaderParameters)obj;

        return other.Value == this.Value && OperationType == other.OperationType;
    }
    public static bool operator ==(ConstantShaderParameters left, ConstantShaderParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ConstantShaderParameters left, ConstantShaderParameters right)
    {
        return !(left == right);
    }
}
