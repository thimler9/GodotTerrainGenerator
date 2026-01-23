using Godot;
using GodotTerrainGenerator2.Test_Scripts;
using System;

public partial class TestInjectDataIntoRender : Node3D
{
	[Export]
	public Camera3D test;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		TestTerrainMeshRender testTerrainMeshRender = test.Compositor.CompositorEffects[0] as TestTerrainMeshRender;
		testTerrainMeshRender.test = 1;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
