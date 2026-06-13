using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel.NormalsShader;
public class NormalsShaderDescriptor
{
    public string? ShaderPath;
    public uint ChunkSize;
    public uint Lod;
}
