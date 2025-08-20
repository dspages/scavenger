public class EquipmentProxy : Equipment
{
    private readonly CharacterSheet character;

    public EquipmentProxy(CharacterSheet character)
    {
        this.character = character;
    }

    public override bool TryEquipToSlot(EquippableItem item, EquippableItem.EquipmentSlot slot, out EquippableItem previous)
    {
        previous = null;
        if (character == null) return false;
        // Capture currently equipped in that slot
        previous = character.GetEquippedItem(slot);
        // Equip
        return character.TryEquipItem(item);
    }

    public override EquippableItem Unequip(EquippableItem.EquipmentSlot slot)
    {
        if (character == null) return null;
        return character.UnequipItem(slot);
    }

    public override EquippableItem Get(EquippableItem.EquipmentSlot slot)
    {
        if (character == null) return null;
        return character.GetEquippedItem(slot);
    }
}


