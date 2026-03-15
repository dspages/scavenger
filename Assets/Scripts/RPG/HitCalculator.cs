using UnityEngine;

public struct HitResult
{
    public float hitChance;
    public float critChance;
    public bool hit;
    public bool critical;
}

public static class HitCalculator
{
    public const float BASE_HIT_CHANCE = 0.75f;
    public const float PENALTY_PER_TILE_OVER_PERCEPTION = 0.20f;
    public const float BONUS_PER_TILE_UNDER_PERCEPTION = 0.05f;
    public const float PENALTY_PER_DEFENDER_AGILITY = 0.05f;

    public const float BASE_CRIT_CHANCE = 0.02f;
    public const float CRIT_PER_AGILITY = 0.02f;

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

        float chance = BASE_HIT_CHANCE;

        int overPerception = Mathf.Max(0, context.distance - attacker.perception);
        int underPerception = Mathf.Max(0, attacker.perception - context.distance);

        chance -= overPerception * PENALTY_PER_TILE_OVER_PERCEPTION;
        chance += underPerception * BONUS_PER_TILE_UNDER_PERCEPTION;
        chance -= defender.agility * PENALTY_PER_DEFENDER_AGILITY;

        // Situational modifiers from status effects
        if (attacker.HasStatusEffect(StatusEffect.EffectType.BLINDED))
            chance -= 0.40f;

        return Mathf.Clamp(chance, MIN_HIT_CHANCE, MAX_HIT_CHANCE);
    }

    public static float CalculateCritChance(CharacterSheet attacker, CharacterSheet defender,
        AttackContext context)
    {
        float chance = BASE_CRIT_CHANCE + CRIT_PER_AGILITY * attacker.agility;

        // Situational modifiers from status effects
        if (attacker.HasStatusEffect(StatusEffect.EffectType.EMPOWER))
            chance += 0.10f;

        return Mathf.Clamp(chance, 0f, MAX_CRIT_CHANCE);
    }
}
