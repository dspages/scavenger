using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryItem
{
    public string itemName = "Unnamed Item";
    public string description = "";
    public Sprite icon = null;
    public int stackSize = 1;
    public int currentStack = 1;
    public int weight = 0;
    public ItemRarity rarity = ItemRarity.Common;
    
    public enum ItemRarity
    {
        Common,
        Uncommon, 
        Rare,
        Epic,
        Legendary
    }

    public InventoryItem(string name, string desc = "", Sprite itemIcon = null, int maxStack = 1)
    {
        itemName = name;
        description = desc;
        icon = itemIcon;
        stackSize = maxStack;
        currentStack = 1;
    }

    public bool CanStackWith(InventoryItem other)
    {
        if (other == null) return false;
        return itemName == other.itemName && currentStack < stackSize && other.currentStack < other.stackSize;
    }

    public int TryStackWith(InventoryItem other)
    {
        if (!CanStackWith(other)) return 0;
        
        int spaceAvailable = stackSize - currentStack;
        int toTransfer = Mathf.Min(spaceAvailable, other.currentStack);
        
        currentStack += toTransfer;
        other.currentStack -= toTransfer;
        
        return toTransfer;
    }

    public string GetDisplayName()
    {
        if (currentStack > 1)
            return $"{itemName} ({currentStack})";
        return itemName;
    }
}
