using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.VoxelOctree.Abstractions.RenderOctree
{
    public class UpdatedHash
    {
        public UpdatedHash(int oldHash, int newHash) 
        {
            OldHash= oldHash;
            NewHash= newHash;
        }

        public int OldHash { get; set; }
        public int NewHash { get; set; }
    }
}
