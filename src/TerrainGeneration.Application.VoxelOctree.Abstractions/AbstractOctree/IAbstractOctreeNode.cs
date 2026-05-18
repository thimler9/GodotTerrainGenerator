using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEventQueue;

namespace TerrainGeneration.Application.VoxelOctree.Abstractions.AbstractOctree
{
    public interface IAbstractOctreeNode
    {
        /// <summary>
        /// Removes all children to this chunk, making it a leaf.
        /// </summary>
        /// <param name="chunks"></param>
        /// <param name="eventQueue"></param>
        public void CollapseChildren(IAbstractOctreeNode[] chunks, IOctreeEventQueue eventQueue);

        /// <summary>
        /// Determines if the chunk has children.
        /// </summary>
        /// <param name="chunks"></param>
        /// <returns></returns>
        public bool HasChildren(IAbstractOctreeNode[] chunks);

        /// <summary>
        /// Given a new array of chunks and a new hash, sets the hashes of the newChunks to the proper hash.
        /// </summary>
        /// <param name="newHash"></param>
        /// <param name="oldChunks"></param>
        /// <param name="newChunks"></param>
        public void UpdateHashAndChildrenHashes(int newHash, IAbstractOctreeNode[] oldChunks, IAbstractOctreeNode[] newChunks);
    }
}
