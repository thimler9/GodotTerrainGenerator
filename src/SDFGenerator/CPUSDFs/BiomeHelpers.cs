using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Utilities.Math.Extensions;

namespace TerrainGeneration.Application.SDFGenerator.CPUSDFs;
public static class BiomeHelpers
{

    public static float CalculateProbability(float depthValue, float biomeDepth, float biomeDepthSpread)
    {
        // If the biome depth spread is zero, we can treat it as a hard cutoff for the depth value.
        if (biomeDepthSpread.FuzzyEquals(0.0f, 0.001f))
        {
            return depthValue.FuzzyEquals(biomeDepth, 0.001f) ? 1.0f : 0.0f;
        }

        float depthDifference = depthValue - biomeDepth;
        return (float)Math.Exp(-(depthDifference * depthDifference) / (biomeDepthSpread * biomeDepthSpread));
    }

    public static Vector<float> CalculateProbability(Vector<float> depthValues, float biomeDepth, float biomeDepthSpread)
    {
        // If the biome depth spread is zero, we can treat it as a hard cutoff for the depth value.
        if (biomeDepthSpread.FuzzyEquals(0.0f, 0.001f))
        {
            var depthValueMask = depthValues.FuzzyEquals(new Vector<float>(biomeDepth), 0.001f);
            return Vector.ConditionalSelect(depthValueMask, new Vector<float>(1.0f), new Vector<float>(0.0f));
        }

        Vector<float> depthDifference = depthValues - new Vector<float>(biomeDepth);
        Vector<float> exponent = -(depthDifference * depthDifference) / (new Vector<float>(biomeDepthSpread * biomeDepthSpread));
        return Vector.Exp(exponent);
    }
}
