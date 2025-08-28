using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CharacterSetup
{
    public static void AssignStartingAbilities(CharacterSheet sheet) {
        if (sheet == null) return;
        switch (sheet.characterClass) {
            case CharacterSheet.CharacterClass.CLASS_FIREMAGE:
                sheet.LearnSpecialAction<ActionFireball>();
                break;
            case CharacterSheet.CharacterClass.CLASS_ROGUE:
                sheet.LearnSpecialAction<ActionStealth>();
                break;
            default:
                // Treat all others as warrior-like for now
                sheet.LearnSpecialAction<ActionBulwark>();
                break;
        }
    }

    public static void AssignStartingGear(CharacterSheet sheet)
    {
        if (sheet == null) return;

        var inventory = sheet.inventory;

        // Add some test inventory items
        inventory.TryAddItem(new InventoryItem("Health Potion") {
            description = "Restores 50 HP",
            stackSize = 10
        });
        inventory.TryAddItem(new InventoryItem("Mana Potion") {
            description = "Restores 30 MP",
            stackSize = 5
        });
        inventory.TryAddItem(new InventoryItem("Bread") {
            description = "Basic food item",
            stackSize = 20
        });
        inventory.TryAddItem(new InventoryItem("Gold Coin") {
            description = "Currency",
            stackSize = 99
        });

        // Add various weapon types for testing
        var cutlass = new EquippableHandheld(
            name: "Cutlass",
            type: EquippableHandheld.WeaponType.OneHanded,
            dmg: 5,
            minRange: 1,
            maxRange: 1,
            actionPointCost: 10,
            dmgType: EquippableHandheld.DamageType.Slashing
        ) {
            description = "A basic sword",
            rangeType = EquippableHandheld.RangeType.Melee
        };

        var steelShield = new EquippableHandheld(
            name: "Steel Shield",
            type: EquippableHandheld.WeaponType.Shield,
            dmg: 1, // Shield bash
            actionPointCost: 8,
            minRange: 1,
            maxRange: 1,
            dmgType: EquippableHandheld.DamageType.Bludgeoning
        ) {
            description = "A sturdy steel shield",
            armorBonus = 2,
            dodgeBonus = 1,
            rangeType = EquippableHandheld.RangeType.Melee
        };

        var pike = new EquippableHandheld(
            name: "Reach Pike",
            type: EquippableHandheld.WeaponType.TwoHanded,
            dmg: 10,
            actionPointCost: 15,
            minRange: 2,
            maxRange: 2,
            dmgType: EquippableHandheld.DamageType.Piercing
        ) {
            description = "A long pike that requires distance to use effectively",
            rangeType = EquippableHandheld.RangeType.Melee
        };
        inventory.TryAddItem(pike);

        var musket = new EquippableHandheld(
            name: "Long Musket",
            type: EquippableHandheld.WeaponType.TwoHanded,
            dmg: 12,
            actionPointCost: 30,
            minRange: 2,
            maxRange: 10,
            dmgType: EquippableHandheld.DamageType.Bludgeoning
        ) {
            description = "A musket that requires distance to avoid muzzle flash",
            rangeType = EquippableHandheld.RangeType.Ranged,
            requiresAmmo = true,
            ammoType = "Musket Ball",
            associatedActionClass = nameof(ActionRangedAttack)
        };
        inventory.TryAddItem(musket);

        var dagger = new EquippableHandheld(
            name: "Iron Dagger",
            type: EquippableHandheld.WeaponType.OneHanded,
            dmg: 2,
            actionPointCost: 4,
            minRange: 1,
            maxRange: 1,
            dmgType: EquippableHandheld.DamageType.Piercing
        ) {
            description = "A quick dagger",
            rangeType = EquippableHandheld.RangeType.Melee
        };
        inventory.TryAddItem(dagger);

        var grenade = new EquippableHandheld(
            name: "Frag Grenade",
            type: EquippableHandheld.WeaponType.OneHanded,
            dmg: 12,
            actionPointCost: 20,
            minRange: 2,
            maxRange: 8,
            dmgType: EquippableHandheld.DamageType.Fire
        ) {
            description = "A grenade that explodes on impact - keep your distance!",
            splashRadius = 2,
            rangeType = EquippableHandheld.RangeType.Ranged,
            isConsumable = true
        };
        // Map grenade to a ground attack action by default
        grenade.associatedActionClass = nameof(ActionGroundAttack);
        inventory.TryAddItem(grenade);

        // Add ammo for ranged weapons
        inventory.TryAddItem(new InventoryItem("Musket Ball") {
            description = "Ammunition for muskets",
            stackSize = 50
        });

        // Equip starting gear (these items go directly to equipment, not inventory)
        sheet.TryEquipItem(cutlass);
        sheet.TryEquipItem(steelShield);

        // Add a basic torch as equippable handheld: 1H, does small fire damage, provides light
        var torch = new EquippableHandheld(
            name: "Torch",
            type: EquippableHandheld.WeaponType.OneHanded,
            dmg: 2,
            actionPointCost: 10,
            minRange: 1,
            maxRange: 1,
            dmgType: EquippableHandheld.DamageType.Fire
        ) {
            description = "A basic torch. Provides light and small fire damage.",
            rangeType = EquippableHandheld.RangeType.Melee,
            providesIllumination = true,
            illuminationRange = 12
        };
        sheet.TryEquipItem(torch);
        // If this character already has an avatar and a combat controller, ensure vision updates
        if (sheet.avatar != null)
        {
            CombatController cc = sheet.avatar.GetComponent<CombatController>();
            if (cc != null)
            {
                VisionSystem vs = GameObject.FindObjectOfType<VisionSystem>();
                if (vs != null) vs.UpdateVision();
            }
        }

        // Add test armor
        var leatherArmor = new EquippableItem(
            name: "Leather Armor",
            equipSlot: EquippableItem.EquipmentSlot.Armor
        ) {
            description = "Basic protection",
            armorBonus = 2,
        };
        sheet.TryEquipItem(leatherArmor);
    }
}


