using Godot;
using System;

public partial class InventoryUI : Control
{
  public override void _Ready()
  {
    this.HideInventory();
  }

  public void ShowInventory()
  {
    this.Visible = true;
  }

  public void HideInventory()
  {
    this.Visible = false;
  }
}
