using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel.CPU;
public class TransvoxelParameters
{

    public uint ChunkSize { get; set; }
    public uint Lod { get; set; }
    public float BorderWidth { get; set; }
}
