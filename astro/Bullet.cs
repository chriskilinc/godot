using Godot;
using System;

public partial class Bullet : Area2D
{
    float speed = 500f;
    float lifetime = 2f; // seconds
    float lifetimeRemaining;

    public override void _Ready()
    {
        lifetimeRemaining = lifetime;
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += Vector2.Up.Rotated(Rotation) * speed * (float)delta;

        lifetimeRemaining -= (float)delta;
        if (lifetimeRemaining <= 0)
        {
            QueueFree();
        }
    }

    public void _on_body_entered(Node body)
    {
        GD.Print("Bullet hit " + body.Name + "!");
        if (body.HasMethod("_on_astroid_hit"))
        {
            body.CallDeferred("_on_astroid_hit");
            QueueFree();
        }
    }
}
