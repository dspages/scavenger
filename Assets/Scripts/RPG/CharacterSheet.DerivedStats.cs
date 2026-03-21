using UnityEngine;

/// <summary>Derived combat stats (single source of truth). Does not reference HitCalculator.</summary>
public partial class CharacterSheet
{
    public int GetMeleeDamageBonus()
    {
        return strength / 2;
    }

    /// <summary>Backstab bonus damage; currently equals agility (rebalance in one place).</summary>
    public int GetBackstabDamageBonus()
    {
        return agility;
    }

    public int GetTotalGearDodgeBonus()
    {
        int total = 0;
        foreach (var kvp in equipment.GetAll())
            total += kvp.Value.dodgeBonus;
        return total;
    }

    /// <summary>Total evasion points used for ranged hit penalty (agility + gear dodge).</summary>
    public int GetRangedEvasionPoints()
    {
        return agility + GetTotalGearDodgeBonus();
    }

    /// <summary>Attacker-side ranged hit total in percent before defender penalty (base + perception distance + blinded).</summary>
    public int GetRangedHitChanceAttackerPartsPercent(int distance)
    {
        int p = RpgCombatBalance.BaseRangedHitChancePercent;
        int overPerception = Mathf.Max(0, distance - perception);
        int underPerception = Mathf.Max(0, perception - distance);
        p -= overPerception * RpgCombatBalance.RangedHitPenaltyPerTileOverPerceptionPercent;
        p += underPerception * RpgCombatBalance.RangedHitBonusPerTileUnderPerceptionPercent;
        if (HasStatusEffect(StatusEffect.EffectType.BLINDED))
            p -= RpgCombatBalance.BlindedRangedHitPenaltyPercent;
        return p;
    }

    /// <summary>Defender-side subtraction from ranged hit (percent points).</summary>
    public int GetDefenderRangedHitPenaltyPercent()
    {
        return GetRangedEvasionPoints() * RpgCombatBalance.RangedHitPenaltyPerDefenderEvasionPointPercent;
    }

    /// <summary>Crit chance before merge clamp, as integer percent (0–50).</summary>
    public int GetCritChancePercent()
    {
        int p = RpgCombatBalance.BaseCritChancePercent
            + agility * RpgCombatBalance.CritChancePerAgilityPointPercent;
        if (HasStatusEffect(StatusEffect.EffectType.EMPOWER))
            p += RpgCombatBalance.EmpowerCritBonusPercent;
        return Mathf.Clamp(p, 0, RpgCombatBalance.MaxCritChancePercent);
    }

    /// <summary>Weapon range per hand for UI; unarmed uses 1–1 on primary hand.</summary>
    public struct WeaponRangeSummary
    {
        public int minRange;
        public int maxRange;
        public string label;
    }

    public void GetEquippedWeaponRangeSummaries(out WeaponRangeSummary? left, out WeaponRangeSummary? right)
    {
        left = null;
        right = null;
        var leftItem = equipment.Get(EquippableItem.EquipmentSlot.LeftHand) as EquippableHandheld;
        var rightItem = equipment.Get(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
        if (leftItem != null)
            left = new WeaponRangeSummary { minRange = leftItem.minRange, maxRange = leftItem.maxRange, label = "L" };
        if (rightItem != null)
            right = new WeaponRangeSummary { minRange = rightItem.minRange, maxRange = rightItem.maxRange, label = "R" };
        if (left == null && right == null)
            right = new WeaponRangeSummary { minRange = 1, maxRange = 1, label = "R" };
    }
}
