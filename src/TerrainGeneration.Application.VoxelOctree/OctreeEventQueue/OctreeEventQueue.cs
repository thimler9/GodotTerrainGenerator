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
        public int WorkBudget;

        public OctreeEventQueue(IRenderOctree eventTargetTree, int workBudget) 
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

        public void AddEvent(IOctreeEvent octreeEvent)
        {
            EventQueue.Enqueue(octreeEvent);
        }

        public void Process()
        {
            int numWork = Math.Min(WorkBudget, EventQueue.Count);
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
