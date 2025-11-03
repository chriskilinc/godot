using Godot;
using Godot.Collections;
using System;

public partial class Inventory : Resource
{
  [Export]
  public Array<InventoryItem> Items = [];
}
