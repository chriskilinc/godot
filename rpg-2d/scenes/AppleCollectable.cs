using Godot;
using System;
using System.Threading.Tasks;

public partial class AppleCollectable : StaticBody2D
{
  private AnimatedSprite2D animatedSprite;
  private AnimationPlayer animationPlayer;
  public override void _Ready()
  {
    animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    FallFromTree();
  }

  private async Task FallFromTree()
  {
    GD.Print("AppleCollectable falling");
    animationPlayer.Play("falling");
    await ToSignal(GetTree().CreateTimer(1), "timeout");
    animationPlayer.Play("fade");
    GD.Print("+1 apples");
    await ToSignal(GetTree().CreateTimer(0.3), "timeout");
    QueueFree();
  }
}
