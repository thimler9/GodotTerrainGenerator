using System;
using System.Collections.Generic;
using System.Linq;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEvent;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEventQueue;
using TerrainGeneration.Application.VoxelOctree.Abstractions.RenderOctree;
using TerrainGeneration.Application.VoxelOctree.OctreeEvents;

namespace TerrainGeneration.Application.VoxelOctree.OctreeEventQueue
{
    public class OctreeEventQueue : IOctreeEventQueue
    {
        public IRenderOctree EventTargetTree;
        public uint WorkBudget;

        private readonly Dictionary<int, ChunkIntentEvent> PendingChunkIntents;

        /// <summary>
        /// Creates an octree event queue. Processes the latest desired octree state against the render octree.
        /// </summary>
        /// <param name="eventTargetTree"></param>
        /// <param name="workBudget"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public OctreeEventQueue(IRenderOctree eventTargetTree, uint workBudget)
        {
            if (eventTargetTree == null)
            {
                throw new ArgumentNullException(nameof(eventTargetTree), "Cannot be null");
            }

            if (workBudget <= 0)
            {
                throw new ArgumentException(nameof(eventTargetTree), "Must be positive");
            }

            EventTargetTree = eventTargetTree;
            WorkBudget = workBudget;
            PendingChunkIntents = new Dictionary<int, ChunkIntentEvent>();
        }

        /// <summary>
        /// Adds a new octree change. Chunk intents are coalesced so only the latest state per chunk is processed.
        /// </summary>
        /// <param name="octreeEvent"></param>
        public void AddEvent(IOctreeEvent octreeEvent)
        {
            if (octreeEvent == null)
            {
                return;
            }

            if (octreeEvent is ChunkIntentEvent intent)
            {
                PendingChunkIntents[intent.Hash] = intent;
                return;
            }
        }

        /// <summary>
        /// Processes pending changes. Will process up to the work budget or until the queue is empty, whatever is smaller.
        /// </summary>
        public void Process()
        {
            uint numWork = Math.Min(WorkBudget, (uint)PendingChunkIntents.Count);
            IOctreeEvent[] eventsToProcess = new IOctreeEvent[numWork];

            int eventIndex = 0;

            foreach (ChunkIntentEvent intent in SelectChunkIntents(numWork - (uint)eventIndex))
            {
                PendingChunkIntents.Remove(intent.Hash);
                eventsToProcess[eventIndex] = intent;
                eventIndex++;
            }

            EventTargetTree.ProcessEvents(eventsToProcess);
        }

        private IEnumerable<ChunkIntentEvent> SelectChunkIntents(uint maxCount)
        {
            int count = (int)maxCount;
            return PendingChunkIntents.Values
                .OrderBy(intent => GetIntentPriority(intent.State))
                .ThenBy(intent => GetDepthPriority(intent))
                .Take(count)
                .ToArray();
        }

        private static int GetIntentPriority(ChunkIntentState state)
        {
            return state switch
            {
                ChunkIntentState.Missing => 0,
                ChunkIntentState.Internal => 1,
                ChunkIntentState.Leaf => 2,
                _ => 3,
            };
        }

        private static int GetDepthPriority(ChunkIntentEvent intent)
        {
            return intent.State == ChunkIntentState.Missing ? -intent.Depth : intent.Depth;
        }
    }
}
