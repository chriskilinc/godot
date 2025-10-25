using Godot;
using System;

public partial class killzone : Area2D
{
    Timer _myTimer;
	private AnimationPlayer _AnimationPlayer;


    public override void _Ready()
    {
        _myTimer = GetNode<Timer>("Timer");
        _AnimationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }
    
    public void _on_body_entered(Node body)
    {
        GD.Print("Player died");
        _AnimationPlayer.Play("death");
        Engine.TimeScale = 0.5;
        body.GetNode<CollisionShape2D>("CollisionShape2D").QueueFree();
        _myTimer.Start();
    }

    public void _on_timer_timeout()
    {
        GD.Print("Reloading scene");
        Engine.TimeScale = 1;
        GetTree().ReloadCurrentScene();
    }
}
