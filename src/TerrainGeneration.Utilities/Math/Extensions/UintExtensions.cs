using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Utilities.Math.Extensions;

public static class UintExtensions
{
    public static bool IsPowerOfTwo(this uint x)
    {
        // Taken from
        // https://stackoverflow.com/questions/600293/how-to-check-if-a-number-is-a-power-of-2
        return (x & (x - 1)) == 0;
    }
}
