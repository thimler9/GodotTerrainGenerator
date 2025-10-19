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
        /// Splits chunk up into 8 children. Uses data from splitData to determine how. Adds necessary events to queue.
        /// </summary>
        /// <param name="chunks"></param>
        /// <param name="eventQueue"></param>
        /// <param name="splitData"></param>
        public void MakeChildren(IAbstractOctreeNode[] chunks, IOctreeEventQueue eventQueue, AbstractOctreeNodeSplitData splitData);

        /// <summary>
        /// Traverses the tree and updates all of the chunks that need updating.
        /// 3 cases:
        ///     - The chunk does not change LOD so there are no changes
        ///     - The player is close enough to the chunk to split it up into smaller chunks of higher LOD
        ///     - The player is far enough away for the chunk to collapse and become a lower LOD
        /// </summary>
        /// <param name="chunks"></param>
        /// <param name="eventQueue"></param>
        /// <param name="spliData"></param>
        public void Update(IAbstractOctreeNode[] chunks, IOctreeEventQueue eventQueue, AbstractOctreeNodeSplitData spliData);

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
