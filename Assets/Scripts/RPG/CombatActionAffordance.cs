using UnityEngine;

/// <summary>
/// Read-only affordability for combat actions. Mirrors <see cref="CombatItemSpend"/> commit rules.
/// Sanity is intentionally not a hard gate (player may choose the tradeoff).
/// </summary>
public static class CombatActionAffordance
{
    public const string ManaCrystalRegistryId = "mana_crystal";
    public const string TechComponentRegistryId = "tech_component";

    public static bool CanAffordExtraItemCosts(AbilityData data, CharacterSheet sheet)
    {
        if (data?.extraItemCosts == null || sheet == null) return true;
        foreach (var c in data.extraItemCosts)
        {
            if (c.amount <= 0 || string.IsNullOrEmpty(c.registryId)) continue;
            if (!CombatItemSpend.HasStackInInventory(sheet, c.registryId, c.amount))
                return false;
        }
        return true;
    }

    public static bool CanAffordManaAndTechCosts(AbilityData data, CharacterSheet sheet)
    {
        if (data == null || sheet == null) return true;
        if (data.manaCrystalCost > 0 &&
            !CombatItemSpend.HasStackInInventory(sheet, ManaCrystalRegistryId, data.manaCrystalCost))
            return false;
        if (data.techComponentsCost > 0 &&
            !CombatItemSpend.HasStackInInventory(sheet, TechComponentRegistryId, data.techComponentsCost))
            return false;
        return true;
    }

    /// <summary>Hard inventory costs for an ability (extra stacks, mana crystals, tech components). Excludes weapon ammo/consumable.</summary>
    public static bool CanAffordAbilityHardInventoryCosts(AbilityData data, CharacterSheet sheet)
    {
        if (!CanAffordExtraItemCosts(data, sheet)) return false;
        return CanAffordManaAndTechCosts(data, sheet);
    }

    public static bool CanAffordWeaponAttackCosts(CombatController controller, EquippableHandheld weapon, AbilityData rangedAbilityOverride)
    {
        if (controller == null || weapon == null) return true;
        var sheet = controller.characterSheet;
        string key = controller.GetSelectedActionKey();

        if (weapon.requiresAmmo && !string.IsNullOrEmpty(weapon.ammoType))
        {
            string ammoReg = rangedAbilityOverride != null && !string.IsNullOrEmpty(rangedAbilityOverride.ammoTypeOverride)
                ? rangedAbilityOverride.ammoTypeOverride
                : weapon.ammoType;
            return CombatItemSpend.HasStackInInventory(sheet, ammoReg, 1);
        }

        if (weapon.isConsumable)
        {
            if (!CombatItemSpend.TryGetHandSlotFromActionKey(key, out var handSlot))
                return false;
            var inHand = sheet.GetEquippedItem(handSlot) as EquippableHandheld;
            if (inHand == null || !ReferenceEquals(inHand, weapon))
                return false;
            return inHand.PeekStackSize() >= 1;
        }

        return true;
    }

    public static bool CanAffordFullAttackAction(CombatController controller, EquippableHandheld weapon, AbilityData ability)
    {
        if (controller?.characterSheet == null) return true;
        if (!CanAffordAbilityHardInventoryCosts(ability, controller.characterSheet)) return false;
        return CanAffordWeaponAttackCosts(controller, weapon, ability);
    }

    public static bool CanAffordAttackFromSearchTile(CombatController controller, Tile attackTargetTile, ActionAttack atk)
    {
        if (controller == null || attackTargetTile == null || atk == null) return true;
        if (attackTargetTile.searchAttackParent == null) return true;
        var weapon = controller.GetEquippedWeaponForSelectedAction();
        var ability = atk.GetAbilityDataForCosts();
        return CanAffordFullAttackAction(controller, weapon, ability);
    }

    /// <summary>True if sanity cost would take current sanity below zero (warning only; does not block actions).</summary>
    public static bool ShouldWarnSanityRisk(AbilityData data, CharacterSheet sheet)
    {
        if (data == null || sheet == null || data.sanityCost <= 0) return false;
        return data.sanityCost > sheet.currentSanity;
    }
}
