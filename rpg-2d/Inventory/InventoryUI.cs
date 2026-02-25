using Godot;
using Godot.Collections;
using System;

public partial class InventoryUI : Control
{
  [Export]
  public bool IsOpen = false;
  private Inventory inventory;
  private Array<InventoryUISlot> slots;

  public override void _Ready()
  {
    // add signal listener
    inventory.Connect(nameof(Inventory.ItemInserted), new Callable(this, nameof(OnItemInserted)));

    Visible = IsOpen;
    // preload player inventory
    inventory = GD.Load<Inventory>("res://Inventory/player_inventory.tres");

    // Get the GridContainer inside NinePatchRect
    var ninePatchRect = GetNode<NinePatchRect>("NinePatchRect");
    var gridContainer = ninePatchRect.GetNode<GridContainer>("GridContainer");

    // Get all children of GridContainer that are InventoryUISlot
    slots = new Array<InventoryUISlot>();
    foreach (Node child in gridContainer.GetChildren())
    {
      if (child is InventoryUISlot slot)
      {
        slots.Add(slot);
      }
    }
    GD.Print($"InventoryUI: Found {slots.Count} slots.");
    UpdateSlots();
  }

  public override void _Process(double delta)
  {
    if (Input.IsActionJustPressed("player_inventory"))
    {
      if (IsOpen)
        Close();
      else
        Open();
    }
  }

  private void OnItemInserted()
  {
    GD.Print($"InventoryUI: ItemInserted signal received. Updating slots.");
    UpdateSlots();
  }

  private void Close()
  {
    IsOpen = false;
    Visible = IsOpen;
  }

  private void Open()
  {
    IsOpen = true;
    Visible = IsOpen;
  }

  // TODO: Refactor this
  private void UpdateSlots()
  {
    for (int i = 0; i < slots.Count; i++)
    {
      if (i < inventory.Slots.Count)
      {
        // GD.Print($"Updating slot {i} with item {inventory.Items[i].Name}");
        slots[i].Update(inventory.Slots[i]);
      }
      else
      {
        GD.Print($"Updating slot {i} with no item");
        slots[i].Update(null);
      }
    }
  }
}


// https://youtu.be/fyRcR6C5H2g?si=FCao4mGFZmKLLPvs&t=871