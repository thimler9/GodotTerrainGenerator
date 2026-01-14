using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel;
public class TransvoxelDescriptor
{
    public TransvoxelShaderDescriptor? TransvoxelShaderDescriptor;
    public IndirectArgsShaderDescriptor? IndirectArgsShaderDescriptor;
    public uint MaxNumTerrainMeshesInQueue;
}
