using Godot;
using System;

public partial class Main : Node
{
    public int numAstroids { get; set; } = 3;
    public Vector2 viewportSize { get; set; }
    public float astroidSpawnRangeMin { get; set; } = 175f;
    public float astroidSpawnRangeMax { get; set; } = 300f;

    private PackedScene playerScene = GD.Load<PackedScene>("res://scenes/player.tscn");
    private PackedScene astroidScene = GD.Load<PackedScene>("res://scenes/astroid_big.tscn");

    private Player playerNode;

    public override void _Ready()
    {
        viewportSize = GetViewport().GetVisibleRect().Size;
        SetUp();
    }

    private void SetUp()
    {
        SpawnPlayer();

        for (int i = 0; i < numAstroids; i++)
        {
            SpawnAstroid();
        }
    }

    private void SpawnPlayer()
    {
        GD.Print("[main] Spawning player...");

        if (playerNode != null)
        {
            playerNode.QueueFree();
        }

        playerNode = playerScene.Instantiate<Player>();
        playerNode.Position = viewportSize / 2; // spawn in the center of the screen
        playerNode.Connect("PlayerDied", new Callable(this, nameof(OnPlayerDied)));
        AddChild(playerNode);
        // TODO: add invulnerability timer to player after respawn so they don't immediately die again if they spawn on top of an astroid
    }

    private void SpawnAstroid()
    {
        GD.Print("[main] Spawning astroid...");
        var astroid = astroidScene.Instantiate<Astroid>();
        // spawn somewhere around the center of the screen
        astroid.Position = viewportSize / 2 + Utility.RandomUnitVector() * Utility.RandomRange(astroidSpawnRangeMin, astroidSpawnRangeMax);
        AddChild(astroid);
    }

    private void OnPlayerDied()
    {
        GD.Print("[main] Player died!");
        CallDeferred(nameof(SpawnPlayer));
    }
}
