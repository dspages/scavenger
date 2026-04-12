using System.Collections.Generic;

public static class SpeciesRules
{
    public const string Human = "human";
    public const string Langurii = "langurii";

    static readonly EquippableItem.EquipmentSlot[] NoExtraHandSlots = { };
    static readonly EquippableItem.EquipmentSlot[] LanguriiExtraHandSlots =
    {
        EquippableItem.EquipmentSlot.ExtraHand1,
    };

    public static bool IsLangurii(string species)
    {
        return string.Equals(NormalizeSpecies(species), Langurii, System.StringComparison.Ordinal);
    }

    public static string NormalizeSpecies(string species)
    {
        return string.IsNullOrWhiteSpace(species)
            ? Human
            : species.Trim().ToLowerInvariant();
    }

    public static IReadOnlyList<EquippableItem.EquipmentSlot> GetExtraHandSlots(string species)
    {
        return IsLangurii(species) ? LanguriiExtraHandSlots : NoExtraHandSlots;
    }

    public static bool CanUseEquipmentSlot(string species, EquippableItem.EquipmentSlot slot)
    {
        if (IsLangurii(species) && slot == EquippableItem.EquipmentSlot.Boots)
            return false;
        return true;
    }

    public static bool CanEquipItemInSlot(string species, EquippableItem.EquipmentSlot slot, EquippableItem item)
    {
        if (!CanUseEquipmentSlot(species, slot))
            return false;
        if (item == null)
            return false;

        if (slot == EquippableItem.EquipmentSlot.ExtraHand1 || slot == EquippableItem.EquipmentSlot.ExtraHand2)
        {
            if (!IsLangurii(species))
                return false;
            if (item is not EquippableHandheld weapon)
                return false;
            return weapon.tag == EquippableHandheld.HandheldTag.Light;
        }

        return true;
    }

    public static void ApplyRecruitStatModifiers(CharacterSheet sheet)
    {
        if (sheet == null) return;
        if (!IsLangurii(sheet.species)) return;
        sheet.agility += 2;
        sheet.willpower -= 2;
    }
}
