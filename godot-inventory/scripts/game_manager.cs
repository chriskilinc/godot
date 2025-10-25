using Godot;
using System;

public partial class game_manager : Node
{

	public int score = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void AddScore()
	{
		score += 1;
		GD.Print("Score: " + score);
	}
}
