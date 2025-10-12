using Godot;
using System;

public partial class Player : CharacterBody2D
{
  [Export]
  public Inventory PlayerInventory { get; set; }

  [Export]
  public float Speed { get; set; } = 125f;

  public override void _Ready()
  {
  }

  public override void _Process(double delta)
  {
    Vector2 inputVector = Input.GetVector("player_left", "player_right", "player_up", "player_down");
    if (inputVector != Vector2.Zero)
    {
      inputVector = inputVector.Normalized();
      Position += inputVector * Speed * (float)delta;
    }
  }
}
