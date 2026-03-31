using UnityEngine;

public static class AttackResolver
{
    public const int UNARMED_DAMAGE = 3;

    public static AttackResult Resolve(CharacterSheet attacker, CharacterSheet defender, AttackContext context)
    {
        var hitResult = HitCalculator.CalculateHit(attacker, defender, context);

        if (!hitResult.hit)
        {
            string missLog = FormatMissMessage(attacker.firstName, defender.firstName, context);
            return new AttackResult
            {
                hit = false,
                critical = false,
                defenderDied = false,
                damageDealt = 0,
                damageType = GetDamageType(context),
                hitChance = hitResult.hitChance,
                critChance = 0f,
                attackerName = attacker.firstName,
                defenderName = defender.firstName,
                weaponName = context.weapon?.itemName,
                logMessage = missLog,
            };
        }

        int damage = GetFinalDamage(attacker, defender, context, hitResult.critical);
        var dmgType = GetDamageType(context);

        bool wasDead = defender.dead;
        defender.ReceiveDamage(damage);
        bool killed = defender.dead && !wasDead;

        string weaponName = context.weapon?.itemName;
        string logMessage = FormatHitMessage(attacker.firstName, defender.firstName, weaponName,
            damage, dmgType, hitResult.critical, killed);

        return new AttackResult
        {
            hit = true,
            critical = hitResult.critical,
            defenderDied = killed,
            damageDealt = damage,
            damageType = dmgType,
            hitChance = hitResult.hitChance,
            critChance = hitResult.critChance,
            attackerName = attacker.firstName,
            defenderName = defender.firstName,
            weaponName = weaponName,
            logMessage = logMessage,
        };
    }

    private static int GetBaseDamage(AttackContext context)
    {
        if (context.weapon != null)
            return context.weapon.damage;

        if (context.abilityData != null && context.abilityData.damage > 0)
            return context.abilityData.damage;

        return UNARMED_DAMAGE;
    }

    private static DamageType GetDamageType(AttackContext context)
    {
        if (context.weapon != null)
            return context.weapon.damageType;

        if (context.abilityData != null)
            return context.abilityData.damageType;

        return DamageType.Bludgeoning;
    }

    private static int GetFinalDamage(CharacterSheet attacker, CharacterSheet defender,
        AttackContext context, bool isCrit)
    {
        int baseDmg = GetBaseDamage(context);

        // Stat bonus: melee damage from CharacterSheet (backstab etc. can be added when AttackContext supports it)
        int statBonus = 0;
        if (context.isMelee)
            statBonus = attacker.GetMeleeDamageBonus();
        float damage = baseDmg + statBonus;

        // Status effect multipliers on the attacker
        if (attacker.HasStatusEffect(StatusEffect.EffectType.EMPOWER))
            damage *= 1.25f;
        if (attacker.HasStatusEffect(StatusEffect.EffectType.RAGE))
            damage *= 1.5f;

        // Random variance: base ± 1
        damage += Random.Range(-1, 2);

        // Critical hit multiplier
        if (isCrit)
            damage *= 1.5f;

        // Armor reduction from defender equipment + BULWARK (CharacterSheet is single source of truth)
        int armor = defender.GetTotalArmor();
        damage -= armor;

        return Mathf.Max(0, Mathf.RoundToInt(damage));
    }

    private static string FormatHitMessage(string attackerName, string defenderName,
        string weaponName, int damage, DamageType dmgType, bool crit, bool killed)
    {
        string verb = crit ? "critically strikes" : "strikes";
        string dmgTypeName = dmgType.ToString();
        string msg;

        if (!string.IsNullOrEmpty(weaponName))
            msg = $"{attackerName} {verb} {defenderName} with {weaponName} for {damage} {dmgTypeName} damage.";
        else
            msg = $"{attackerName} {verb} {defenderName} for {damage} {dmgTypeName} damage.";

        if (killed)
            msg += $" {defenderName} is slain!";

        return msg;
    }

    private static string FormatMissMessage(string attackerName, string defenderName, AttackContext context)
    {
        string weaponName = context.weapon?.itemName;
        if (!string.IsNullOrEmpty(weaponName))
            return $"{attackerName} fires at {defenderName} with {weaponName} and misses.";
        return $"{attackerName} attacks {defenderName} and misses.";
    }
}
