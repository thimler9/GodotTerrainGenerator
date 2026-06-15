using Godot;
using System;

public partial class PauseQueue : Button
{
    private void OnToggle(bool toggledOn)
    {
        // See if there is a VoxelOctree node in the scene and gets reference to it
        var testVoxelOctreeNode = GetTree().CurrentScene.GetNodeOrNull<TestVoxelOctree>("VoxelOctree");
        if (testVoxelOctreeNode != null)
        {
            testVoxelOctreeNode.VoxelOctree.EventQueueTogglePause(toggledOn);
            Text = toggledOn ? "Unpause Queue" : "Pause Queue";
        }
    }
}
