public static class AbilityCatalog
{
    public static readonly AbilityData Kick = new AbilityData
    {
        id = "kick",
        displayName = "Kick",
        description = "A basic unarmed kick.",
        archetype = AbilityData.Archetype.MeleeAttack,
        damage = 5,
        actionPointCost = 10,
    };

    public static readonly AbilityData Fireball = new AbilityData
    {
        id = "fireball",
        displayName = "Fireball",
        description = "Fireball deals fire damage to all enemies in a radius.",
        archetype = AbilityData.Archetype.GroundAttack,
        damage = 12,
        damageType = DamageType.Fire,
        minRange = 3,
        maxRange = 6,
        radius = 2,
        actionPointCost = 20,
        // Arcane spell: mana crystals + sanity
        manaCrystalCost = 4,
        sanityCost = 4,
        cooldown = 2,
    };

    public static readonly AbilityData Stealth = new AbilityData
    {
        id = "stealth",
        displayName = "Stealth",
        description = "Become invisible to enemies for several rounds. Canceled if attacking or an enemy moves directly into your square.",
        archetype = AbilityData.Archetype.SelfCast,
        statusEffect = StatusEffect.EffectType.HIDDEN,
        statusDuration = 3,
        cooldown = 3,
    };

    public static readonly AbilityData Bulwark = new AbilityData
    {
        id = "bulwark",
        displayName = "Bulwark",
        description = "Increases armor for several rounds, providing enhanced protection against attacks.",
        archetype = AbilityData.Archetype.SelfCast,
        statusEffect = StatusEffect.EffectType.BULWARK,
        statusDuration = 3,
        statusPowerLevel = 3,
        cooldown = 3,
    };

    public static readonly AbilityData[] All = new AbilityData[]
    {
        Kick, Fireball, Stealth, Bulwark,
    };
}
