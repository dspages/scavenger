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
    
    // For shields and defensive items
    public int armorBonus = 0;
    public int dodgeBonus = 0;

    public EquippableItem(string name, EquipmentSlot equipSlot) 
        : base(name)
    {
        slot = equipSlot;
    }
}
