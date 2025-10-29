using Godot;
using System;
using System.Threading.Tasks;

public partial class AppleTree : Node2D
{
  public string State = "empty";
  public bool PlayerInArea = false;

  public PackedScene AppleCollectableScene;

  private AnimatedSprite2D AnimatedSprite;
  private Timer GrowTimer;
  private Marker2D AppleDropMarker;

  public override void _Ready()
  {
    AnimatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    GrowTimer = GetNode<Timer>("growth_timer");
    AppleCollectableScene = GD.Load<PackedScene>("res://scenes/apple_collectable.tscn");
    AppleDropMarker = GetNode<Marker2D>("Marker2D");

    GD.Print("AppleTree ready");
    if (State == "empty")
    {
      GD.Print("AppleTree is empty");
      GrowTimer.Start();
    }
  }

  public override void _Process(double delta)
  {
    if (State == "empty")
    {
      AnimatedSprite.Play("empty");
    }
    else if (State == "full")
    {
      AnimatedSprite.Play("full");
      if (PlayerInArea && Input.IsActionJustPressed("player_interact"))
      {
        GD.Print("AppleTree picked");
        State = "empty";
        DropApple();
      }
    }
    else if (State == "dead")
    {
      AnimatedSprite.Play("dead");
    }
  }

  public void _on_pickable_area_body_entered(Node2D body)
  {
    if (body is Player)
    {
      PlayerInArea = true;
      GD.Print("Player entered pickable area");
    }
  }

  public void _on_pickable_area_body_exited(Node2D body)
  {
    if (body is Player)
    {
      PlayerInArea = false;
      GD.Print("Player exited pickable area");
    }
  }

  private void _on_growth_timer_timeout()
  {
    if (State == "empty")
    {
      GD.Print("AppleTree grew apples");
      State = "full";
    }
  }

  private async Task DropApple()
  {
    GD.Print("Apple dropped");
    var appleInstance = AppleCollectableScene.Instantiate<StaticBody2D>();
    appleInstance.GlobalPosition = AppleDropMarker.GlobalPosition;
    GetParent().AddChild(appleInstance);
    await ToSignal(GetTree().CreateTimer(3), "timeout");
    GrowTimer.Start();
  }
}
