using Godot;
using System;

public partial class Player : RigidBody2DWrap
{
    [Signal]
    public delegate void PlayerDiedEventHandler();

    int force = 400;
    float rotationSpeed = MathF.Tau * 0.75f; // 270 degrees per second

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _PhysicsProcess(double delta)
    {
        // WASD movement
        // W = thrust forward
        // A = rotate left
        // D = rotate right
        AngularVelocity = 0; // Reset angular velocity each frame
        if (Input.IsActionPressed("thrust_forward"))
        {
            // Translate(new Vector2(0, -speed * (float)delta).Rotated(Rotation));
            ApplyForce(Vector2.Up.Rotated(Rotation) * force, Vector2.Zero);
        }

        if (Input.IsActionPressed("rotate_left"))
        {
            // Rotate(-rotationSpeed * (float)delta);
            AngularVelocity = -rotationSpeed;
        }

        if (Input.IsActionPressed("rotate_right"))
        {
            // Rotate(rotationSpeed * (float)delta);
            AngularVelocity = rotationSpeed;
        }

        if (Input.IsActionJustPressed("fire"))
        {
        }
    }

    public void OnHit(Node body)
    {
        GD.Print("Player hit by " + body.Name + "!");
        EmitSignal("PlayerDied");
    }
}