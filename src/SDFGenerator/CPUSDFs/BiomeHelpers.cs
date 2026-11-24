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
        float depthDifference = depthValue - biomeDepth;
        return (float)Math.Exp(-(depthDifference * depthDifference) / (biomeDepthSpread * biomeDepthSpread));
    }

    public static Vector<float> CalculateProbability(Vector<float> depthValues, float biomeDepth, float biomeDepthSpread)
    {
        Vector<float> depthDifference = depthValues - new Vector<float>(biomeDepth);
        Vector<float> exponent = -(depthDifference * depthDifference) / (new Vector<float>(biomeDepthSpread * biomeDepthSpread));
        return Vector.Exp(exponent);
    }
}
