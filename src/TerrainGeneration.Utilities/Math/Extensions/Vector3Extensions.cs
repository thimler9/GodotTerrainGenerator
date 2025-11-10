using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace TerrainGeneration.Utilities.Math.Extensions;
public static class Vector3Extensions
{
    public static bool FuzzyEquals(this Vector3 a, Vector3 b, float epsilon = FloatExtensions.DEFAULT_EPSILON)
    {
        return a.X.FuzzyEquals(b.X, epsilon) && a.Y.FuzzyEquals(b.Y, epsilon) && a.Z.FuzzyEquals(b.Z, epsilon);
    }

}
