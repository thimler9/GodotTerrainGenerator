using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEvent;
using TerrainGeneration.Application.VoxelOctree.Abstractions.OctreeEventQueue;
using TerrainGeneration.Application.VoxelOctree.Abstractions.RenderOctree;

namespace TerrainGeneration.Application.VoxelOctree.OctreeEventQueue
{
    public class OctreeEventQueue : IOctreeEventQueue
    {
        public Queue<IOctreeEvent> EventQueue;
        public IRenderOctree EventTargetTree;
        public uint WorkBudget;

        /// <summary>
        /// Creates an octree event queue. Processes the events sent from the abstract octree against the render octree.
        /// </summary>
        /// <param name="eventTargetTree"></param>
        /// <param name="workBudget"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public OctreeEventQueue(IRenderOctree eventTargetTree, uint workBudget) 
        { 
            if (eventTargetTree== null)
            {
                throw new ArgumentNullException(nameof(eventTargetTree), "Cannot be null");
            }

            if (workBudget <= 0)
            {
                throw new ArgumentException(nameof(eventTargetTree), "Must be positive");
            }

            this.EventTargetTree = eventTargetTree;
            this.WorkBudget = workBudget;
            EventQueue = new Queue<IOctreeEvent>();
        }

        /// <summary>
        /// Adds a new event to the tree.
        /// </summary>
        /// <param name="octreeEvent"></param>
        public void AddEvent(IOctreeEvent octreeEvent)
        {
            EventQueue.Enqueue(octreeEvent);
        }

        /// <summary>
        /// Processes events in the tree. Will process up to the work budget or until the queue is empty, whatever is smaller.
        /// </summary>
        public void Process()
        {
            uint numWork = Math.Min(WorkBudget, (uint)EventQueue.Count);
            IOctreeEvent[] eventsToProcess = new IOctreeEvent[numWork];

            // Grab events to process this frame
            for (int i = 0; i < numWork; i++)
            {
                IOctreeEvent currEvent = EventQueue.Dequeue();
                eventsToProcess[i] = currEvent;
            }

            EventTargetTree.ProcessEvents(eventsToProcess);
        }
    }
}
