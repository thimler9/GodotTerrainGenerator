using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

namespace TerrainGeneration.Application.SDFGenerator;
public class SDFGeneratorSettings
{
    public SimplexNoiseShaderDescriptor SimplexNoiseShaderDescriptor;
    public uint ChunkSize;
}
