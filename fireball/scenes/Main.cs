using Godot;
using System;

public partial class Main : Node
{
    public int Enemies { get; set; } = 5;
    public float EnemySpawnRadius { get; set; } = 400f;
    private PackedScene playerScene = GD.Load<PackedScene>("res://scenes/player.tscn");
    private PackedScene enemyScene = GD.Load<PackedScene>("res://scenes/enemy.tscn");
    private bool _isShuttingDown;
    private Player _player;

    public override void _Ready()
    {
        Setup();
    }

    private void Setup()
    {
        SpawnPlayer();
        SpawnEnemies();
    }

    private void SpawnPlayer()
    {
        _player = playerScene.Instantiate<Player>();
        _player.Position = Vector2.Zero;
        AddChild(_player);
    }

    private void SpawnEnemies()
    {
        for (int i = 0; i < Enemies; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        Enemy newEnemy = enemyScene.Instantiate<Enemy>();
        float angle = GD.Randf() * Mathf.Tau;
        float distance = Mathf.Sqrt(GD.Randf()) * EnemySpawnRadius;
        Vector2 spawnCenter = (_player != null && IsInstanceValid(_player))
            ? _player.GlobalPosition
            : Vector2.Zero;
        newEnemy.Position = spawnCenter + Vector2.Right.Rotated(angle) * distance;
        newEnemy.TreeExited += OnEnemyTreeExited;
        AddChild(newEnemy);
    }

    private void OnEnemyTreeExited()
    {
        if (_isShuttingDown || !IsInsideTree())
        {
            return;
        }

        SpawnEnemy();
    }

    public override void _ExitTree()
    {
        _isShuttingDown = true;
    }

    public override void _Process(double delta)
    {
        // Nothing needed here for now
    }
}
