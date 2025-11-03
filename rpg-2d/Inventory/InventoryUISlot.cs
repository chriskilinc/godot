using Godot;
using System;

public partial class InventoryUISlot : Panel
{
  public Sprite2D ItemDisplay;

  public override void _Ready()
  {
    ItemDisplay = GetNode<Sprite2D>("ItemDisplay");
  }

  public void Update(InventoryItem item)
  {
    if (item != null)
    {
      ItemDisplay.Visible = true;
      ItemDisplay.Texture = item.Icon;
    }
    else
    {
      ItemDisplay.Visible = false;
    }
  }
}
