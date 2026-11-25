using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Utilities.Math.Extensions;
public static class SIMDExtensions
{
    public const float DEFAULT_EPSILON = 0.1f;

    /// <summary>
    /// Returns a mask each element in the vectors indicating whether the absolute difference between the corresponding elements is less than or equal to the specified epsilon.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="epislon"></param>
    /// <returns></returns>
    public static Vector<int> FuzzyEquals(this Vector<float> a, Vector<float> b, float epislon = DEFAULT_EPSILON)
    {
        Vector<float> absDiff = Vector.Abs(a - b);
        Vector<float> epsilonVector = new Vector<float>(epislon);
        return Vector.LessThanOrEqual(absDiff, epsilonVector);
    }
}
