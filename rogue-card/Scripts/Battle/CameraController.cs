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
    [Export] public Vector3 Offset { get; set; } = new Vector3(0, 8, 8);

    /// <summary>How quickly the camera catches up to the target (higher is faster).</summary>
    [Export] public float FollowSpeed { get; set; } = 5f;

    public override void _PhysicsProcess(double delta)
    {
        if (Target == null) return;

        // Calculate the desired position
        Vector3 targetPos = Target.GlobalPosition + Offset;

        // Smoothly interpolate current position toward the target position
        GlobalPosition = GlobalPosition.Lerp(targetPos, (float)delta * FollowSpeed);

        // Always look at the target (or a point slightly offset if desired)
        // For a fixed isometric view, we might not want it to literally "LookAt" every frame,
        // but since we are locking offset rotation locally, LookAt keeps the target centered.
        // If the MainCamera already has a fixed baked rotation you like, you can comment this out.
        // LookAt(Target.GlobalPosition, Vector3.Up);
    }
}
