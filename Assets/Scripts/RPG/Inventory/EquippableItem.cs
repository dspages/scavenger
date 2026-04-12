public class EquippableItem : InventoryItem
{
    public EquipmentSlot slot;
    
    public enum EquipmentSlot
    {
        LeftHand,
        RightHand,
        ExtraHand1,
        ExtraHand2,
        Armor,
        Helmet,
        Boots,
        Gloves,
        LeftRing,
        RightRing,
        Goggles,
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

    /// <summary>Player-facing label for equipment slots (tooltips, empty-slot hints).</summary>
    public static string GetEquipmentSlotDisplayName(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.LeftHand => "Left hand",
            EquipmentSlot.RightHand => "Right hand",
            EquipmentSlot.ExtraHand1 => "Prehensile Tail",
            EquipmentSlot.ExtraHand2 => "Extra hand",
            EquipmentSlot.Armor => "Armor",
            EquipmentSlot.Helmet => "Helmet",
            EquipmentSlot.Boots => "Boots",
            EquipmentSlot.Gloves => "Gloves",
            EquipmentSlot.LeftRing => "Left ring",
            EquipmentSlot.RightRing => "Right ring",
            EquipmentSlot.Goggles => "Goggles",
            EquipmentSlot.Amulet => "Amulet",
            _ => slot.ToString()
        };
    }
}
