public class ArmorData : ItemData
{
    public EquippableItem.EquipmentSlot slot = EquippableItem.EquipmentSlot.Armor;
    public int armorBonus = 0;
    public int dodgeBonus = 0;

    public override InventoryItem CreateInstance()
    {
        var eq = new EquippableItem(displayName, slot)
        {
            description = description,
            weight = weight,
            rarity = rarity,
            armorBonus = armorBonus,
            dodgeBonus = dodgeBonus,
        };
        eq.ConfigureStacks(MaxStack, MaxStack);
        return eq;
    }
}
