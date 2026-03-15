using Godot;
using System;

public partial class Main : Node
{
    [Signal]
    public delegate void ScoreUpdatedEventHandler(int newScore);

    [Signal]
    public delegate void LivesUpdatedEventHandler(int newLives);

    [Signal]
    public delegate void LevelUpdatedEventHandler(int newLevel);

    [Signal]
    public delegate void GameOverEventHandler();

    public int numAstroids { get; set; } = 3;
    public Vector2 viewportSize { get; set; }
    public float astroidSpawnRangeMin { get; set; } = 175f;
    public float astroidSpawnRangeMax { get; set; } = 300f;

    private int _score;
    public int Score { get { return _score; } set { _score = value; EmitSignal(SignalName.ScoreUpdated, value); } }
    private int _lives;
    public int Lives { get { return _lives; } set { _lives = value; EmitSignal(SignalName.LivesUpdated, value); } }
    private int _level;
    public int Level { get { return _level; } set { _level = value; EmitSignal(SignalName.LevelUpdated, value); } }

    private PackedScene playerScene = GD.Load<PackedScene>("res://scenes/player.tscn");
    private PackedScene astroidScene = GD.Load<PackedScene>("res://scenes/astroid_big.tscn");

    private Player playerNode;
    private UI uiNode;

    public override void _Ready()
    {
        viewportSize = GetViewport().GetVisibleRect().Size;
        SetupNewGame();
    }

    public void SetupNewGame()
    {
        CleanGame();
        Score = 0;
        Lives = 3;
        Level = 0;
        SetUp();
    }

    private void SetUp()
    {
        Level += 1;

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
        GetNode("Astroids").AddChild(astroid);
    }

    private void OnPlayerDied()
    {
        GD.Print("[main] Player died!");
        RemoveLife();
        DeletePlayer();

        if (Lives <= 0)
        {
            OnGameOver();
            return;
        }
        CallDeferred(nameof(SpawnPlayer));
    }

    private void DeletePlayer()
    {
        if (playerNode != null)
        {
            playerNode.QueueFree();
            playerNode = null;
        }
    }

    private void CleanGame()
    {
        // Delete all astroids
        foreach (var astroid in GetNode("Astroids").GetChildren())
        {
            if (astroid is Node node)
            {
                node.QueueFree();
            }
        }

        DeletePlayer();
    }

    private void OnGameOver()
    {
        GD.Print("[main] Game over!");
        EmitSignal(SignalName.GameOver);
    }

    private void AddToScore(int points)
    {
        Score += points;
    }

    private void RemoveLife()
    {
        Lives--;
    }

    private void NextLevel()
    {
        GD.Print("[main] Next level!");
        Level++;
    }
}
