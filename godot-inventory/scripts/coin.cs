using Godot;
using System;

public partial class coin : Area2D
{
	private Node GameManager;
	private AnimationPlayer _AnimationPlayer;

	public override void _Ready()
	{
		GameManager = GetNode<Node>("%GameManager");
		_AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
	}

	public void _on_body_entered(Node body)
	{
		GD.Print("Coin collected");
		GameManager.Call("AddScore");
		_AnimationPlayer.Play("collected");
	}
}
