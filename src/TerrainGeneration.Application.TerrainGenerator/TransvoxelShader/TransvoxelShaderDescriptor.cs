using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator.TransvoxelShader;
public class TransvoxelShaderDescriptor
{
    public TransvoxelShaderParameters Parameters;
    public string? ShaderPath;
    public uint MaxNumTerrainMeshesInQueue;
}
