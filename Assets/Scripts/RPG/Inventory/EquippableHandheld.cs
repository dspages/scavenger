using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquippableHandheld : EquippableItem
{
    public enum WeaponType
    {
        OneHanded,    // Can be wielded in either hand, allows dual-wielding
        TwoHanded,    // Requires both hands, blocks other hand slots
        Shield        // Defensive item for off-hand
    }

    public enum RangeType
    {
        Melee,        // Close combat
        Ranged        // Distance attacks
    }

    public enum DamageType
    {
        Piercing,
        Bludgeoning,
        Slashing,
        Fire,
        Cold,
        Acid,
        Lightning,
        Holy,
        Dark
    }

    public WeaponType weaponType = WeaponType.OneHanded;
    public RangeType rangeType = RangeType.Melee;
    public int minDamage = 1;
    public int maxDamage = 3;
    public int minRange = 1;
    public int maxRange = 1;
    public int splashRadius = 0;
    public int actionPointCost = 1;
    public DamageType damageType = DamageType.Slashing;
    
    // For shields and defensive items
    public int armorBonus = 0;
    public int dodgeBonus = 0;
    
    // Ammo/consumable properties
    public bool isConsumable = false;        // Reduces stack when used
    public bool requiresAmmo = false;        // Needs separate ammo item
    public string ammoType = "";             // Type of ammo required

    public EquippableHandheld(string name, WeaponType type, int minDmg, int maxDmg)
        : base(name, EquippableItem.EquipmentSlot.RightHand) // Default slot, will be overridden when equipped
    {
        weaponType = type;
        minDamage = minDmg;
        maxDamage = maxDmg;
    }

    public EquippableHandheld(string name, WeaponType type, int minDmg, int maxDmg, int minRange, int maxRange, DamageType dmgType)
        : base(name, EquippableItem.EquipmentSlot.RightHand) // Default slot, will be overridden when equipped
    {
        weaponType = type;
        minDamage = minDmg;
        maxDamage = maxDmg;
        this.minRange = minRange;
        this.maxRange = maxRange;
        damageType = dmgType;
    }

    public int GetRandomDamage()
    {
        return Random.Range(minDamage, maxDamage + 1);
    }

    /// <summary>
    /// Check if this weapon can be used at the given range
    /// </summary>
    public bool CanUseAtRange(int range)
    {
        return range >= minRange && range <= maxRange;
    }

    /// <summary>
    /// Get a description of the weapon's range requirements
    /// </summary>
    public string GetRangeDescription()
    {
        if (minRange == maxRange)
            return $"Range: {minRange}";
        else
            return $"Range: {minRange}-{maxRange}";
    }

    /// <summary>
    /// Check if this weapon can be equipped alongside another weapon in the other hand
    /// </summary>
    public bool CanDualWieldWith(EquippableHandheld other)
    {
        if (other == null) return true;
        
        // Two-handed weapons block everything
        if (weaponType == WeaponType.TwoHanded || other.weaponType == WeaponType.TwoHanded)
            return false;
            
        // Shields can be paired with one-handed weapons (any range type)
        if (weaponType == WeaponType.Shield && other.weaponType == WeaponType.OneHanded)
            return true;
        if (weaponType == WeaponType.OneHanded && other.weaponType == WeaponType.Shield)
            return true;
            
        // One-handed weapons can be dual-wielded (any range type)
        if (weaponType == WeaponType.OneHanded && other.weaponType == WeaponType.OneHanded)
            return true;
            
        return false;
    }

    /// <summary>
    /// Get a display name that includes weapon type, range type, and range requirements
    /// </summary>
    public override string GetDisplayName()
    {
        string typeLabel = weaponType switch
        {
            WeaponType.OneHanded => "1H",
            WeaponType.TwoHanded => "2H", 
            WeaponType.Shield => "Shield",
            _ => ""
        };
        
        string rangeLabel = rangeType switch
        {
            RangeType.Melee => "Melee",
            RangeType.Ranged => "Ranged",
            _ => ""
        };
        
        string rangeInfo = GetRangeDescription();
        
        return $"{base.GetDisplayName()} ({typeLabel}/{rangeLabel} {rangeInfo})";
    }
}
