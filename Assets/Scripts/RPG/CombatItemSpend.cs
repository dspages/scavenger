using UnityEngine;

/// <summary>
/// Combat-time spending of inventory stacks (ammo, consumable weapons) by registry id.
/// </summary>
public static class CombatItemSpend
{
    public static bool TryGetHandSlotFromActionKey(string key, out EquippableItem.EquipmentSlot slot)
    {
        slot = default;
        if (string.IsNullOrEmpty(key)) return false;
        if (key.EndsWith(":ExtraHand2"))
        {
            slot = EquippableItem.EquipmentSlot.ExtraHand2;
            return true;
        }
        if (key.EndsWith(":ExtraHand1"))
        {
            slot = EquippableItem.EquipmentSlot.ExtraHand1;
            return true;
        }
        if (key.EndsWith(":LeftHand"))
        {
            slot = EquippableItem.EquipmentSlot.LeftHand;
            return true;
        }
        if (key.EndsWith(":RightHand"))
        {
            slot = EquippableItem.EquipmentSlot.RightHand;
            return true;
        }
        return false;
    }

    public static bool HasStackInInventory(CharacterSheet sheet, string registryId, int amount)
    {
        if (sheet == null || string.IsNullOrEmpty(registryId) || amount <= 0) return false;
        var data = ContentRegistry.GetItemData(registryId);
        if (data == null) return false;
        string matchName = data.displayName;
        for (int i = 0; i < Inventory.MaxSlots; i++)
        {
            var item = sheet.inventory.GetItem(i);
            if (item != null && item.itemName == matchName && item.PeekStackSize() >= amount)
                return true;
        }
        return false;
    }

    /// <summary>Total stack count across inventory slots for a registry item id. Returns 0 if none.</summary>
    public static int CountStackInInventory(CharacterSheet sheet, string registryId)
    {
        if (sheet == null || string.IsNullOrEmpty(registryId)) return 0;
        var data = ContentRegistry.GetItemData(registryId);
        if (data == null) return 0;
        string matchName = data.displayName;
        int total = 0;
        for (int i = 0; i < Inventory.MaxSlots; i++)
        {
            var item = sheet.inventory.GetItem(i);
            if (item != null && item.itemName == matchName)
                total += Mathf.Max(0, item.PeekStackSize());
        }
        return total;
    }

    /// <summary>Removes stack from first inventory slot that matches the registry item. Returns false if not enough.</summary>
    public static bool TryConsumeStackByRegistryId(CharacterSheet sheet, string registryId, int amount)
    {
        if (sheet == null || string.IsNullOrEmpty(registryId) || amount <= 0) return false;
        var data = ContentRegistry.GetItemData(registryId);
        if (data == null) return false;
        string matchName = data.displayName;
        for (int i = 0; i < Inventory.MaxSlots; i++)
        {
            var item = sheet.inventory.GetItem(i);
            if (item == null || item.itemName != matchName) continue;
            if (item.PeekStackSize() < amount) continue;
            if (!item.AttemptDecrementStackSize(amount)) continue;
            if (item.PeekStackSize() <= 0)
                sheet.inventory.SetItemAt(i, null);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Weapon attacks: spend ammo from inventory, or decrement consumable equipped weapon stack.
    /// </summary>
    public static bool TryCommitWeaponAttackCosts(CombatController controller, EquippableHandheld weapon, AbilityData rangedAbilityOverride)
    {
        if (controller == null) return true;
        if (weapon == null) return true;
        var sheet = controller.characterSheet;
        string key = controller.GetSelectedActionKey();

        if (weapon.requiresAmmo && !string.IsNullOrEmpty(weapon.ammoType))
        {
            string ammoReg = rangedAbilityOverride != null && !string.IsNullOrEmpty(rangedAbilityOverride.ammoTypeOverride)
                ? rangedAbilityOverride.ammoTypeOverride
                : weapon.ammoType;
            if (!TryConsumeStackByRegistryId(sheet, ammoReg, 1))
            {
                sheet.DisplayPopup("Not enough ammunition");
                return false;
            }
            return true;
        }

        if (weapon.isConsumable)
        {
            if (!TryGetHandSlotFromActionKey(key, out var handSlot))
            {
                sheet.DisplayPopup("No weapon hand");
                return false;
            }
            var inHand = sheet.GetEquippedItem(handSlot) as EquippableHandheld;
            if (inHand == null || !ReferenceEquals(inHand, weapon))
                return false;
            if (!inHand.AttemptDecrementStackSize(1))
            {
                sheet.DisplayPopup("Cannot use");
                return false;
            }
            if (inHand.PeekStackSize() <= 0)
                sheet.UnequipItem(handSlot);
            return true;
        }

        return true;
    }

    /// <summary>Optional per-ability inventory costs (reagents, etc.).</summary>
    public static bool TrySpendExtraItemCosts(AbilityData data, CharacterSheet sheet)
    {
        if (data?.extraItemCosts == null || sheet == null) return true;
        foreach (var c in data.extraItemCosts)
        {
            if (c.amount <= 0 || string.IsNullOrEmpty(c.registryId)) continue;
            if (!TryConsumeStackByRegistryId(sheet, c.registryId, c.amount))
            {
                sheet.DisplayPopup("Not enough materials");
                return false;
            }
        }
        return true;
    }

    /// <summary>Inventory costs from <see cref="AbilityData"/> (extra stacks, mana crystals, tech components). Sanity is handled separately.</summary>
    public static bool TrySpendAbilityHardCosts(AbilityData data, CharacterSheet sheet)
    {
        if (data == null || sheet == null) return true;
        if (!TrySpendExtraItemCosts(data, sheet)) return false;
        if (data.manaCrystalCost > 0 &&
            !TryConsumeStackByRegistryId(sheet, CombatActionAffordance.ManaCrystalRegistryId, data.manaCrystalCost))
        {
            sheet.DisplayPopup("Not enough mana crystals");
            return false;
        }
        if (data.techComponentsCost > 0 &&
            !TryConsumeStackByRegistryId(sheet, CombatActionAffordance.TechComponentRegistryId, data.techComponentsCost))
        {
            sheet.DisplayPopup("Not enough tech components");
            return false;
        }
        return true;
    }

    /// <summary>Sanity is a soft resource: always applies when an ability resolves; can go negative by design.</summary>
    public static void ApplySanityCost(AbilityData data, CharacterSheet sheet)
    {
        if (data == null || sheet == null || data.sanityCost <= 0) return;
        sheet.currentSanity -= data.sanityCost;
    }

    public static void LogEditorInvariantFailed(string message)
    {
#if UNITY_EDITOR
        Debug.LogError("[CombatItemSpend] " + message);
#endif
    }
}
