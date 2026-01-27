using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.VoxelOctree.AbstractOctree
{
    public class TerrainLod
    {
        public TerrainLod() { }

        public uint LodDivider { get; set; }
        public float LodDistanceCutoff { get; set; }
    }
}
