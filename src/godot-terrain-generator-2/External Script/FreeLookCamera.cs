
using System;
using Godot;

partial class FreeLookCamera : FreeLookCameraBase
{   
    public bool Enabled
    {
        set
        {
            _enabled = value;
            if (_enabled)
            {
                // save current active camera
                Viewport viewport = GetViewport();
                if (viewport == null)
                {
                    return;
                }

                _previousCamera = viewport.GetCamera3D();
                _previousMouseMode = Input.MouseMode;

                if (_previousCamera != null)
                {
                    // Copy current camera properties into this camera
                    GlobalTransform = _previousCamera.GlobalTransform;
                    Fov = _previousCamera.Fov;
                    Near = _previousCamera.Near;
                    Far = _previousCamera.Far;
                    Projection = _previousCamera.Projection;

                    // disable current camera
                    _previousCamera.Current = false;
                }

                // Enable free look camera and set as the current one
                Current = true;
            }
            else
            {
                Current = false;
                if (_previousCamera != null)
                {
                    _previousCamera.Current = true;                
                    Input.MouseMode = _previousMouseMode;
                }
            }
        }

        get
        {
            return _enabled;
        }
    }

    private bool _enabled = false;
    private Camera3D _previousCamera;
    private Input.MouseModeEnum _previousMouseMode;

    // Starts disabled so the scene's normal camera remains active until free-look is requested.
    public override void _Ready()
    {
        Current = false;
    }

    // Forwards input to the base free-look controls only while this camera is active.
    public override void _Input(InputEvent _event)
    {
        if (! Enabled)
        {
            return;
        }
        base._Input(_event);
    }

    // Updates movement only while free-look mode is enabled.
    public override void _Process(double delta)
    {
        if (! Enabled)
        {
            return;
        }

        base._Process(delta);
    }
}
