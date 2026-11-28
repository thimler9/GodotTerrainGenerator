using Godot;

public partial class RigidBodyController : RigidBody3D
{
    [Export] public Camera3D Camera;
    [Export] public float MoveSpeed = 6.0f;
    [Export] public float MouseSensitivity = 0.003f;

    private float _yaw;
    private float _pitch;
    private bool _looking;

    public override void _Ready()
    {
        Camera ??= GetNode<Camera3D>("Camera3D");

        LockRotation = true;   // body won't tip over or spin from collisions

        _yaw = Camera.Rotation.Y;
        _pitch = Camera.Rotation.X;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Right)
        {
            _looking = mb.Pressed;
            Input.MouseMode = _looking
                ? Input.MouseModeEnum.Captured
                : Input.MouseModeEnum.Visible;
        }
        else if (@event is InputEventMouseMotion mm && _looking)
        {
            _yaw -= mm.Relative.X * MouseSensitivity;
            _pitch -= mm.Relative.Y * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));

            Camera.Rotation = new Vector3(_pitch, _yaw, 0f);
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        // WASD (forward is -Z)
        Vector3 input = Vector3.Zero;
        if (Input.IsKeyPressed(Key.W)) input.Z -= 1f;
        if (Input.IsKeyPressed(Key.S)) input.Z += 1f;
        if (Input.IsKeyPressed(Key.A)) input.X -= 1f;
        if (Input.IsKeyPressed(Key.D)) input.X += 1f;

        // Rotate input by yaw so "forward" follows where the camera is facing
        Vector3 dir = new Basis(Vector3.Up, _yaw) * input.Normalized();

        // Set horizontal velocity only; leave Y alone so gravity and jumps still work
        Vector3 vel = state.LinearVelocity;
        vel.X = dir.X * MoveSpeed;
        vel.Z = dir.Z * MoveSpeed;
        state.LinearVelocity = vel;
    }
}