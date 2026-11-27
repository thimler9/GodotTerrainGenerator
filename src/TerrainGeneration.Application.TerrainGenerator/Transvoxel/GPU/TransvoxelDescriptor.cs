using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel;
public class TransvoxelDescriptor
{
    public required TransvoxelShaderDescriptor TransvoxelShaderDescriptor;
    public required IndirectArgsShaderDescriptor IndirectArgsShaderDescriptor;
    public required uint MaxNumTerrainMeshesInQueue;
}
