using Godot;
using System;

public partial class Enemy : RigidBody2D
{
    public float Health { get; set; } = 100f;
    private ProgressBar _healthBar;

    public override void _Ready()
    {
        _healthBar = GetNode<ProgressBar>("HealthBar");
        _healthBar.MaxValue = Health;
        _healthBar.Value = Health;
    }

    public void TakeDamage(float damage)
    {
        Health -= damage;
        _healthBar.Value = Health;
        if (Health <= 0)
        {
            QueueFree();
        }
    }
}
