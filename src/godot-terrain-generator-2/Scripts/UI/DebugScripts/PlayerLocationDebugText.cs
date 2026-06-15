using Godot;
using System;

public partial class PlayerLocationDebugText : Label
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        // Checks for a camera in the scene, sets the text to the cameras global p't find a camera, it sets the text to "No Camera Found"
		var camera = GetTree().CurrentScene.GetNodeOrNull<Camera3D>("Camera3D");
		if (camera != null)
		{
			var position = camera.GlobalPosition;
			SetText($"Camera Position: X={position.X:F2}, Y={position.Y:F2}, Z={position.Z:F2}");
		}
		else
		{
			SetText("No Camera Found");
        }
    }
}
