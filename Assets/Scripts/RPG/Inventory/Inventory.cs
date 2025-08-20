using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Inventory
{
    public delegate void InventoryChangedDelegate();
    
    private List<InventoryItem> inventory = new List<InventoryItem>();
    public const int MaxSlots = 24;
    
    public event InventoryChangedDelegate OnInventoryChanged;
    
    public IReadOnlyList<InventoryItem> Items => inventory.AsReadOnly();
    
    public Inventory()
    {
        // Initialize with empty slots
        for (int i = 0; i < MaxSlots; i++)
        {
            inventory.Add(null);
        }
    }
    
    public bool TryAddItem(InventoryItem item)
    {
        if (item == null) return false;
        
        // Try to stack with existing items first
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i] != null && inventory[i].CanStackWith(item))
            {
                int transferred = inventory[i].TryStackWith(item);
                if (item.currentStack <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Find first empty slot
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i] == null)
            {
                inventory[i] = item;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        
        return false; // Inventory full
    }
    
    public InventoryItem GetItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventory.Count) return null;
        return inventory[slotIndex];
    }
    
    public bool RemoveItem(int slotIndex, int count = 1)
    {
        if (slotIndex < 0 || slotIndex >= inventory.Count) return false;
        if (inventory[slotIndex] == null) return false;
        
        InventoryItem item = inventory[slotIndex];
        item.currentStack -= count;
        
        if (item.currentStack <= 0)
        {
            inventory[slotIndex] = null;
        }
        
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    public bool SwapItems(int slotA, int slotB)
    {
        if (slotA < 0 || slotA >= inventory.Count) return false;
        if (slotB < 0 || slotB >= inventory.Count) return false;
        
        InventoryItem temp = inventory[slotA];
        inventory[slotA] = inventory[slotB];
        inventory[slotB] = temp;
        
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool SetItemAt(int slotIndex, InventoryItem item)
    {
        if (slotIndex < 0 || slotIndex >= inventory.Count) return false;
        inventory[slotIndex] = item;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventory.Count) return false;
        return inventory[slotIndex] == null;
    }

    public int FindFirstEmptySlot()
    {
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i] == null) return i;
        }
        return -1;
    }
}
