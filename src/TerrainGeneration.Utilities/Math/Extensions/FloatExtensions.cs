using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Utilities.Math.Extensions;
public static class FloatExtensions
{
    public const float DEFAULT_EPSILON = 0.1f;

    public static bool FuzzyEquals(this float a, float b, float epislon = DEFAULT_EPSILON)
    {
        if (a == b)
        {
            return true;
        }

        return MathF.Abs(a - b) <= epislon;
    }
}
