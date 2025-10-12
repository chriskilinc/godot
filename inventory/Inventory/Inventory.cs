using Godot;
using System;

public partial class Inventory : Resource
{
  [Export]
  public int Capacity { get; set; } = 12;

  [Export]
  public Godot.Collections.Array<InventoryItem> Items { get; set; } = new Godot.Collections.Array<InventoryItem>();
}
