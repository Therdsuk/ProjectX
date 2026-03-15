using Godot;
using System;

/// <summary>
/// A simple camera controller that smoothly follows a target Node3D.
/// </summary>
public partial class CameraController : Camera3D
{
    /// <summary>The node this camera should follow.</summary>
    public Node3D Target { get; set; }

    /// <summary>The offset from the target's position.</summary>
    [Export] public Vector3 Offset { get; set; } = new Vector3(0, 10, 10);

    /// <summary>How quickly the camera catches up to the target (higher is faster).</summary>
    [Export] public float FollowSpeed { get; set; } = 5f;
    
    /// <summary>How quickly the camera rotates (higher is faster).</summary>
    [Export] public float RotationSpeed { get; set; } = 8f;

    private float _targetYaw = 45f;
    private float _currentYaw = 45f;
    private float _baseYaw = 45f;
    private Vector3 _currentFocusPosition;

    public override void _Ready()
    {
        _baseYaw = RotationDegrees.Y;
        _targetYaw = _baseYaw;
        _currentYaw = _baseYaw;
        if (Target != null) _currentFocusPosition = Target.GlobalPosition;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Q)
            {
                _targetYaw += 45f;
            }
            else if (keyEvent.Keycode == Key.E)
            {
                _targetYaw -= 45f;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // 1. Smoothly interpolate the yaw rotation (using LerpAngle for safe 0-360 transitions)
        float currentRad = Mathf.DegToRad(_currentYaw);
        float targetRad = Mathf.DegToRad(_targetYaw);
        _currentYaw = Mathf.RadToDeg(Mathf.LerpAngle(currentRad, targetRad, (float)delta * RotationSpeed));
        
        // Apply the rotation to the Camera's Y-axis
        Vector3 rot = RotationDegrees;
        rot.Y = _currentYaw;
        RotationDegrees = rot;

        if (Target == null) return;

        // 2. Smoothly follow the target position (the "Focus Point")
        _currentFocusPosition = _currentFocusPosition.Lerp(Target.GlobalPosition, (float)delta * FollowSpeed);

        // 3. Calculate the exact point on the orbit circle based on current interpolated yaw
        // We rotate the original offset to find where the camera should be in its arc
        Basis yawRotation = Basis.FromEuler(new Vector3(0, Mathf.DegToRad(_currentYaw - _baseYaw), 0));
        Vector3 rotatedOffset = yawRotation * Offset;

        // 4. Set position directly relative to the focus point. 
        // This ensures a perfect circular arc without cutting corners.
        GlobalPosition = _currentFocusPosition + rotatedOffset;
    }
}
