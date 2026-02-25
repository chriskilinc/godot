using Godot;
using System;

public partial class InventoryUISlot : Panel
{
  public Sprite2D ItemDisplay;
  public Label AmountLabel;

  public override void _Ready()
  {
    ItemDisplay = GetNode<Sprite2D>("ItemDisplay");
    AmountLabel = GetNode<Label>("AmountLabel");
  }

  public void Update(InventorySlot item)
  {
    if (item != null)
    {
      ItemDisplay.Visible = true;
      ItemDisplay.Texture = item.Item.Icon;
      AmountLabel.Text = item.Amount.ToString();
    }
    else
    {
      ItemDisplay.Visible = false;
    }
  }
}
