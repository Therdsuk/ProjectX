using Godot;
using System;

/// <summary>
/// Rail + Orbit camera controller.
/// - A/D keys: slide the focus point (pin) left/right along the X-axis rail
/// - Q/E keys: rotate the camera around the current focus point
/// The camera orbits at a fixed offset distance from the pin.
/// The pin is clamped to configurable board boundaries.
/// </summary>
public partial class CameraController : Camera3D
{
    /// <summary>The height (Y) and distance offset from the focus point.</summary>
    [Export] public Vector3 Offset { get; set; } = new Vector3(0, 16, 16);

    /// <summary>How fast the focus point pans horizontally (world units per second).</summary>
    [Export] public float PanSpeed { get; set; } = 12f;

    /// <summary>How quickly the camera smoothly catches up (higher is faster).</summary>
    [Export] public float SmoothSpeed { get; set; } = 8f;

    /// <summary>How quickly the camera rotates on Q/E (higher is faster).</summary>
    [Export] public float RotationSpeed { get; set; } = 8f;

    /// <summary>Minimum X position the focus point can pan to (left edge).</summary>
    public float RailMinX { get; set; } = 0f;

    /// <summary>Maximum X position the focus point can pan to (right edge).</summary>
    public float RailMaxX { get; set; } = 16f;

    /// <summary>The fixed Z position of the focus point (board center Z).</summary>
    public float RailZ { get; set; } = 6f;

    [ExportGroup("Zoom")]
    [Export] public float MinZoom { get; set; } = 0.4f;
    [Export] public float MaxZoom { get; set; } = 2.0f;
    [Export] public float ZoomSpeed { get; set; } = 0.15f;
    [Export] public float ZoomSmoothSpeed { get; set; } = 10f;

    // Internal state
    private float _targetX;
    private float _currentX;
    private float _targetYaw;
    private float _currentYaw;
    private float _baseYaw;
    private float _targetZoom = 1.0f;
    private float _currentZoom = 1.0f;

    public override void _Ready()
    {
        // Start at the left side of the rail
        _targetX = RailMinX;
        _currentX = _targetX;

        // Initialize yaw from the camera's initial rotation
        _baseYaw = RotationDegrees.Y;
        _targetYaw = _baseYaw;
        _currentYaw = _baseYaw;
    }

    public override void _Input(InputEvent @event)
    {
        // Q/E for orbital rotation (step-based, like before)
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

        // Mouse wheel for zoom
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
            {
                _targetZoom -= ZoomSpeed;
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
            {
                _targetZoom += ZoomSpeed;
            }
            _targetZoom = Mathf.Clamp(_targetZoom, MinZoom, MaxZoom);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // --- Rail panning (A/D) ---
        float panInput = 0f;
        if (Input.IsKeyPressed(Key.D)) panInput += 1f;
        if (Input.IsKeyPressed(Key.A)) panInput -= 1f;

        _targetX += panInput * PanSpeed * dt;
        _targetX = Mathf.Clamp(_targetX, RailMinX, RailMaxX);
        _currentX = Mathf.Lerp(_currentX, _targetX, dt * SmoothSpeed);

        // --- Orbital rotation (Q/E) ---
        float currentRad = Mathf.DegToRad(_currentYaw);
        float targetRad = Mathf.DegToRad(_targetYaw);
        _currentYaw = Mathf.RadToDeg(Mathf.LerpAngle(currentRad, targetRad, dt * RotationSpeed));

        // Apply yaw to the camera's rotation (keep pitch from initial setup)
        Vector3 rot = RotationDegrees;
        rot.Y = _currentYaw;
        RotationDegrees = rot;

        // Smooth zoom
        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, dt * ZoomSmoothSpeed);

        // --- Position: orbit the offset around the focus point ---
        // Focus point (the "pin") slides along the rail
        Vector3 focusPoint = new Vector3(_currentX, 0, RailZ);

        // Rotate the offset around Y by the yaw delta from base
        Basis yawRotation = Basis.FromEuler(new Vector3(0, Mathf.DegToRad(_currentYaw - _baseYaw), 0));
        
        // Apply zoom to the distance of the offset
        Vector3 rotatedOffset = yawRotation * (Offset * _currentZoom);

        GlobalPosition = focusPoint + rotatedOffset;
    }
}
