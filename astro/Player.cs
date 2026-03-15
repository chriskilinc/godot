using Godot;
using System;

public partial class Player : RigidBody2DWrap
{
    [Signal]
    public delegate void PlayerDiedEventHandler();

    public Node2D Pivot { get; set; }   // Used to spawn bullet

    int force = 400;
    float rotationSpeed = MathF.Tau * 0.75f; // 270 degrees per second
    float fireCooldown = 0.25f; // seconds
    float fireCooldownRemaining = 0f;
    PackedScene bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");

    public override void _Ready()
    {
        base._Ready();
        Pivot = GetNodeOrNull<Node2D>("Pivot");
    }

    public override void _PhysicsProcess(double delta)
    {
        // WASD movement
        // W = thrust forward
        // A = rotate left
        // D = rotate right
        // space = fire bullet
        AngularVelocity = 0; // Reset angular velocity each frame
        if (Input.IsActionPressed("thrust_forward"))
        {
            ApplyForce(Vector2.Up.Rotated(Rotation) * force, Vector2.Zero);
        }

        if (Input.IsActionPressed("rotate_left"))
        {
            AngularVelocity = -rotationSpeed;
        }

        if (Input.IsActionPressed("rotate_right"))
        {
            AngularVelocity = rotationSpeed;
        }

        if (fireCooldownRemaining > 0)
        {
            fireCooldownRemaining -= (float)delta;
        }
        if (Input.IsActionPressed("fire") && fireCooldownRemaining <= 0)
        {
            fireCooldownRemaining = fireCooldown;
            ShootBullet();
        }
    }

    public void OnHit(Node body)
    {
        GD.Print("Player hit by " + body.Name + "!");
        EmitSignal("PlayerDied");
    }

    private void ShootBullet()
    {
        GD.Print("[Player] Shoot bullet");
        var bullet = bulletScene.Instantiate<Bullet>();
        bullet.Rotation = Rotation;
        bullet.Position = Pivot.GlobalPosition;
        GetParent().AddChild(bullet);
    }
}