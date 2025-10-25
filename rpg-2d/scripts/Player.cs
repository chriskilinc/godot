using Godot;
using System;

public partial class Player : CharacterBody2D
{
  [Export]
  public int Speed = 100;

  public string PlayerState = "Idle"; // TODO

  private AnimatedSprite2D animatedSprite;

  public override void _Ready()
  {
    animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    GD.Print(animatedSprite);
  }

  public override void _PhysicsProcess(double delta)
  {
    Vector2 velocity = Velocity;

    // Get the input direction and normalize it to have consistent speed in all directions
    Vector2 inputDirection = Input.GetVector("player_left", "player_right", "player_up", "player_down");
    if (inputDirection != Vector2.Zero)
    {
      PlayerState = "walking";
      velocity = inputDirection.Normalized() * Speed;
    }
    else
    {
      PlayerState = "idle";
      velocity = Vector2.Zero;
    }

    Velocity = velocity;

    MoveAndSlide();
    PlayAnimation(inputDirection);
  }

  private void PlayAnimation(Vector2 direction)
  {
    string animation = "idle";
    if (PlayerState == "walking")
    {
      if (direction.X < 0 && direction.Y < 0)
        animation = "walk-north-west";
      else if (direction.X > 0 && direction.Y < 0)
        animation = "walk-north-east";
      else if (direction.X < 0 && direction.Y > 0)
        animation = "walk-south-west";
      else if (direction.X > 0 && direction.Y > 0)
        animation = "walk-south-east";
      else if (direction.X > 0)
        animation = "walk-east";
      else if (direction.X < 0)
        animation = "walk-west";
      else if (direction.Y > 0)
        animation = "walk-south";
      else if (direction.Y < 0)
        animation = "walk-north";
    }
    animatedSprite.Play(animation);
  }
}
