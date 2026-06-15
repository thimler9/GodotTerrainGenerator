using Godot;
using System;

public partial class EventsInQueueDebugText : Label
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Looks for a node with the script TestVoxelOctree, grabs the VoxelOctree from it, and sets the text to the number of events in the queue. If it can't find the node or the VoxelOctree, it sets the text to "VoxelOctree Not Found"
		var testVoxelOctreeNode = GetTree().CurrentScene.GetNodeOrNull<TestVoxelOctree>("VoxelOctree");
        if (testVoxelOctreeNode != null)
		{
			var eventsInQueue = testVoxelOctreeNode.VoxelOctree.GetNumEventsInQueue();
			SetText($"Events in Queue: {eventsInQueue}");
		}
		else
		{
			SetText("TestVoxelOctree Node Not Found");
        }
	}
}
