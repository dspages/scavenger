using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquippableWeapon : EquippableItem
{
    public int minDamage = 1;
    public int maxDamage = 3;
    public int range = 1;
    public DamageType damageType = DamageType.Physical;
    
    public enum DamageType
    {
        Physical,
        Fire,
        Ice,
        Lightning,
        Poison,
        Holy,
        Dark
    }

    public EquippableWeapon(string name, int minDmg, int maxDmg, EquipmentSlot weaponSlot = EquipmentSlot.RightHand, string desc = "", Sprite itemIcon = null)
        : base(name, weaponSlot, desc, itemIcon)
    {
        minDamage = minDmg;
        maxDamage = maxDmg;
    }

    public int GetRandomDamage()
    {
        return Random.Range(minDamage, maxDamage + 1);
    }
}
