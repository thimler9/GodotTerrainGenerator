using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

namespace TerrainGeneration.Application.SDFGenerator;
public class SDFGeneratorSettings
{
    public ISDFShader? SDFShader;
    public SDFShaderParameters SDFShaderParameters;
}
