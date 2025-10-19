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

        public int lodDivider { get; set; }
        public float lodDistanceCutoff { get; set; }
    }
}
