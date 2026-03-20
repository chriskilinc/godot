using Godot;
using System;

public partial class Projectile : RigidBody2D
{
    private const float DefaultDamage = 10f; // TODO: Should be set in some central config

    [Export]
    public float Damage { get; set; } = DefaultDamage;

    [Export]
    public float Speed { get; set; } = 500f;

    [Export]
    public float Knockback { get; set; } = 500f;

    [Export]
    public float Lifetime { get; set; } = 200f; // Seconds before the projectile is destroyed

    [Export]
    public PackedScene ExplosionScene { get; set; } = GD.Load<PackedScene>("res://scenes/explosion.tscn");

    [Export]
    public float ScaleFactor { get; set; } = 0.15f;    // Growth per 10 damage (0.1 = 10%)

    private Sprite2D _sprite;
    private CollisionShape2D _collisionShape;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        ScaleProjectile();

        // Set the projectile's velocity in the direction it's facing
        Vector2 direction = new Vector2(Mathf.Cos(Rotation), Mathf.Sin(Rotation));
        LinearVelocity = direction * Speed;

        // Schedule the projectile to be freed after its lifetime expires
        GetTree().CreateTimer(Lifetime).Connect("timeout", Callable.From(() => QueueFree()));
    }

    private void ScaleProjectile()
    {
        GD.Print($"Scaling projectile with damage {Damage} using scale factor {ScaleFactor}");
        // 10 damage is baseline scale 1.0; growth starts at 20 damage.
        float damageSteps = Mathf.Max(0f, Mathf.Floor(Damage / DefaultDamage) - 1f);
        float scale = 1f + (damageSteps * ScaleFactor);
        _sprite.Scale = new Vector2(scale, scale);
        _collisionShape.Scale = new Vector2(scale, scale);
    }

    public void _on_body_entered(Node body)
    {

        // Apply damage
        if (body.HasMethod("TakeDamage"))
        {
            body.Call("TakeDamage", Damage);
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
