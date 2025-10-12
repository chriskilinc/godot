using Godot;
using System;

public partial class InventoryUI : Control
{
  public bool IsOpen { get; set; }

  public override void _Ready()
  {
    CloseInventory();
  }

  public override void _Process(double delta)
  {
    if (Input.IsActionJustPressed("ui_inventory"))
    {
      if (IsOpen)
      {
        CloseInventory();
      }
      else
      {
        OpenInventory();
      }
    }
  }

  public void CloseInventory()
  {
    IsOpen = false;
    Hide();
  }
  public void OpenInventory()
  {
    IsOpen = true;
    Show();
  }
}
