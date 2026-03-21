using UnityEngine;

public struct HitResult
{
    public float hitChance;
    public float critChance;
    public bool hit;
    public bool critical;
}

/// <summary>Merges derived stats from two CharacterSheets + AttackContext. Numeric rules live on CharacterSheet / RpgCombatBalance.</summary>
public static class HitCalculator
{
    public const float MIN_HIT_CHANCE = 0.05f;
    public const float MAX_HIT_CHANCE = 0.95f;
    public const float MAX_CRIT_CHANCE = 0.50f;

    public static HitResult CalculateHit(CharacterSheet attacker, CharacterSheet defender,
        AttackContext context)
    {
        float hitChance = CalculateHitChance(attacker, defender, context);
        bool hit;

        if (context.isMelee || context.isAoE)
        {
            hit = true;
        }
        else
        {
            hit = Random.value <= hitChance;
        }

        float critChance = 0f;
        bool critical = false;
        if (hit)
        {
            critChance = CalculateCritChance(attacker, defender, context);
            critical = Random.value <= critChance;
        }

        return new HitResult
        {
            hitChance = hitChance,
            critChance = critChance,
            hit = hit,
            critical = critical,
        };
    }

    public static float CalculateHitChance(CharacterSheet attacker, CharacterSheet defender,
        AttackContext context)
    {
        if (context.isMelee || context.isAoE)
            return 1f;

        int totalPercent = attacker.GetRangedHitChanceAttackerPartsPercent(context.distance)
            - defender.GetDefenderRangedHitPenaltyPercent();
        totalPercent = Mathf.Clamp(totalPercent, RpgCombatBalance.MinHitChancePercent, RpgCombatBalance.MaxHitChancePercent);
        return totalPercent / 100f;
    }

    public static float CalculateCritChance(CharacterSheet attacker, CharacterSheet defender,
        AttackContext context)
    {
        int p = attacker.GetCritChancePercent();
        return Mathf.Clamp(p / 100f, 0f, MAX_CRIT_CHANCE);
    }
}
