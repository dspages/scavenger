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

    public EquippableItem(string name, EquipmentSlot equipSlot) 
        : base(name)
    {
        slot = equipSlot;
    }
}
