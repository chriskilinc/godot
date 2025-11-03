using Godot;
using System;

public partial class InventoryItem : Resource
{
  [Export]
  public string Name = "Item";

  [Export]
  public string Description = "Description";

  [Export]
  public Texture2D Icon;
}
