using Godot;
using System;

public partial class InventorySlot : Resource
{
  [Export]
  public InventoryItem Item;

  [Export]
  public int Amount = 1;
}
