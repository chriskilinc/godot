using Godot;
using System;

public partial class Player : RigidBody2D
{
	[Export]
	float speed = 300f;

	[Export]
	float acceleration = 15f;

	[Export]
	public float TurnSpeed = 12.5f; // rad/s

	float fireCooldown = 0.5f; // Seconds between shots
	float fireCooldownRemaining = 0f;

	private Node2D _pivot;

	public PackedScene ProjectileScene { get; set; } = GD.Load<PackedScene>("res://scenes/projectile.tscn");

	public override void _Ready()
	{
		_pivot = GetNode<Node2D>("Pivot");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDirection = Input.GetVector(
			"player_left",
			"player_right",
			"player_up",
			"player_down");

		Vector2 targetVelocity = inputDirection * speed;
		LinearVelocity = LinearVelocity.Lerp(targetVelocity, (float)(acceleration * delta));

		// Rotate toward the mouse position (sprite drawn facing right)
		Vector2 mouseGlobalPos = GetGlobalMousePosition();
		Vector2 toMouse = mouseGlobalPos - GlobalPosition;
		if (toMouse.LengthSquared() > 0.0001f)
		{
			float targetAngle = toMouse.Angle();
			float angleDiff = Mathf.AngleDifference(Rotation, targetAngle); // [-π, π]

			// Proportional controller: angular velocity proportional to angle error
			AngularVelocity = Mathf.Clamp(angleDiff * TurnSpeed, -TurnSpeed, TurnSpeed);
		}

		if (fireCooldownRemaining > 0)
		{
			fireCooldownRemaining -= (float)delta;
		}

		if (Input.IsActionPressed("player_fire") && fireCooldownRemaining <= 0)
		{
			fireCooldownRemaining = fireCooldown;
			FireProjectile();
		}
	}

	private void FireProjectile()
	{
		if (ProjectileScene == null || _pivot == null)
		{
			return;
		}

		Projectile projectileInstance = ProjectileScene.Instantiate<Projectile>();
		projectileInstance.GlobalPosition = _pivot.GlobalPosition;
		projectileInstance.Rotation = _pivot.GlobalRotation;
		GetTree().CurrentScene.AddChild(projectileInstance);
	}
}
