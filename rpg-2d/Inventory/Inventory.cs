using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class Inventory : Resource
{

  [Signal]
  public delegate void ItemInsertedEventHandler();

  [Export]
  public Array<InventorySlot> Slots { get; set; } = [];

  public void InsertItem(InventoryItem item, int amount = 1)
  {
    var itemSlots = Slots.Where(s => s != null && s.Item == item).ToArray();
    if (itemSlots.Length > 0)
    {
      // Item already exists in inventory, increase amount
      itemSlots[0].Amount += amount;
    }
    else
    {
      var emptySlot = Slots.FirstOrDefault(s => s == null);
      if (emptySlot != null)
      {
        emptySlot = new InventorySlot
        {
          Item = item,
          Amount = amount
        };
        int slotIndex = Slots.IndexOf(emptySlot);
        Slots[slotIndex] = emptySlot;
        GD.Print($"Inserted {item.Name} x{amount} into slot {slotIndex}");
        EmitSignal(nameof(ItemInserted));
      }
      else
      {
        GD.Print("No empty slots available in inventory.");
      }
    }
  }
}
