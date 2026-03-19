using Godot;
using System;

public partial class Projectile : RigidBody2D
{

    [Export]
    public float Damage { get; set; }

    [Export]
    public float Speed { get; set; } = 500f;

    [Export]
    public float Knockback { get; set; } = 500f;

    [Export]
    public float Lifetime { get; set; } = 1.5f; // Seconds before the projectile is destroyed

    [Export]
    public PackedScene ExplosionScene { get; set; } = GD.Load<PackedScene>("res://scenes/explosion.tscn");

    public override void _Ready()
    {
        // Set the projectile's velocity in the direction it's facing
        Vector2 direction = new Vector2(Mathf.Cos(Rotation), Mathf.Sin(Rotation));
        LinearVelocity = direction * Speed;

        // Schedule the projectile to be freed after its lifetime expires
        GetTree().CreateTimer(Lifetime).Connect("timeout", Callable.From(() => QueueFree()));
    }

    public void _on_body_entered(Node body)
    {

        // Apply damage
        if (body.HasMethod("ApplyDamage"))
        {
            body.Call("ApplyDamage", Damage);
        }

        if (ExplosionScene != null)
        {
            var explosionInstance = ExplosionScene.Instantiate<Explosion>();
            explosionInstance.GlobalPosition = GlobalPosition;
            GetTree().CurrentScene.AddChild(explosionInstance);
        }
        else
        {
            GD.PrintErr("ExplosionScene is not set on Projectile.");
        }

        QueueFree();
    }
}
