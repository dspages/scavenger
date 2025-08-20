using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquippableItem : InventoryItem
{
    public EquipmentSlot slot;
    
    public enum EquipmentSlot
    {
        LeftHand,
        RightHand,
        Armor,
        Helmet,
        Boots,
        Ring,
        Amulet
    }

    public EquippableItem(string name, EquipmentSlot equipSlot, string desc = "", Sprite itemIcon = null) 
        : base(name, desc, itemIcon, 1) // Equipment items don't stack
    {
        slot = equipSlot;
    }
}
