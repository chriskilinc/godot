using Godot;
using System;

public partial class CharacterBody3d : CharacterBody3D
{
    [Export]
    public float Speed = 5.0f;

    public override void _PhysicsProcess(double delta)
    {
        Vector2 input = Input.GetVector(
            "move_left",
            "move_right",
            "move_forward",
            "move_backward"
        );

        Vector3 direction = (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();

        Velocity = new Vector3(
            direction.X * Speed,
            Velocity.Y,
            direction.Z * Speed
        );

        MoveAndSlide();
    }
}

