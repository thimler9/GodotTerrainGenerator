using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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

        //private Queue<ChunkStateEvent> MissingEvents = new Queue<ChunkStateEvent>();
        //private Queue<ChunkStateEvent> InternalEvents = new Queue<ChunkStateEvent>();
        //private Queue<ChunkStateEvent> LeafEvents = new Queue<ChunkStateEvent>();

        private Dictionary<int, OctreeEvent> ChunkStateEvents = new Dictionary<int, OctreeEvent>();

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
        }

        /// <summary>
        /// Adds a new octree change. Chunk intents are coalesced so only the latest state per chunk is processed.
        /// </summary>
        /// <param name="octreeEvent"></param>
        public void AddEvent(OctreeEvent octreeEvent)
        {
            if (octreeEvent == null)
            {
                return;
            }

            if (octreeEvent is ChunkStateEvent stateEvent)
            {
                ChunkStateEvents[stateEvent.Hash] = stateEvent;
            }
        }

        /// <summary>
        /// Processes pending changes. Will process up to the work budget or until the queue is empty, whatever is smaller.
        /// </summary>
        public void Process()
        {
            uint numWork = Math.Min(WorkBudget, (uint)ChunkStateEvents.Count);

            // Process all of the other events immediately
            OctreeEvent[] otherEvents = ChunkStateEvents.Values
                .Where(v => (v is ChunkStateEvent stateEvent) && (stateEvent.State != ChunkState.Leaf))
                .ToArray();
            foreach (var octreeEvent in otherEvents)
            {
                if (octreeEvent is ChunkStateEvent stateEvent)
                {
                    ChunkStateEvents.Remove(stateEvent.Hash);
                }
            }

            // We only want to complete the events that terrain over several frames
            OctreeEvent[] leafEvents = GetEventsToProcess(numWork).ToArray();
            foreach (var chunkEvent in leafEvents)
            {
                if (chunkEvent is ChunkStateEvent stateEvent)
                {
                    ChunkStateEvents.Remove(stateEvent.Hash);
                }
            }
            
            // We want to process leaf events first; if the leaves have the terrain first, we can avoid unecessary allocations in the internals
            EventTargetTree.ProcessEvents(leafEvents);
            EventTargetTree.ProcessEvents(otherEvents);
        }

        private IEnumerable<OctreeEvent> GetEventsToProcess(uint numWork)
        {
            // We want to prioritize leaf events, then internal events, then missing events. We also want to prioritize deeper chunks over shallower chunks.
            return ChunkStateEvents.Values
                .Where(v => v is ChunkStateEvent stateEvent && (stateEvent.State == ChunkState.Leaf))
                .OrderBy(v => GetChunkDepthPriority(v))
                .Take((int)numWork);
        }

        //private int GetChunkEventPriority(OctreeEvent octreeEvent)
        //{
        //    return octreeEvent switch
        //    {
        //        ChunkStateEvent stateEvent => stateEvent.State switch
        //        {
        //            ChunkState.Leaf => 0,
        //            ChunkState.Internal => 1,
        //            ChunkState.Missing => 2,
        //            _ => 3
        //        },
        //        _ => 4
        //    };
        //}

        private int GetChunkDepthPriority(OctreeEvent octreeEvent)
        {
            return octreeEvent switch
            {
                ChunkStateEvent stateEvent => -stateEvent.Depth,
                _ => int.MaxValue
            };
        }
    }
}
