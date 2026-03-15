public static class StatusEffectCatalog
{
    public static readonly StatusEffectData Regeneration = new StatusEffectData
    {
        id = "regeneration",
        effectType = StatusEffect.EffectType.REGENERATION,
        displayName = "Regeneration",
        healPerRound = 5,
    };

    public static readonly StatusEffectData Knockdown = new StatusEffectData
    {
        id = "knockdown",
        effectType = StatusEffect.EffectType.KNOCKDOWN,
        displayName = "Knockdown",
        apEffect = StatusEffectData.APEffect.Zero,
        popupText = "Knockdown",
    };

    public static readonly StatusEffectData Petrified = new StatusEffectData
    {
        id = "petrified",
        effectType = StatusEffect.EffectType.PETRIFIED,
        displayName = "Petrified",
        apEffect = StatusEffectData.APEffect.Zero,
        popupText = "Petrified",
    };

    public static readonly StatusEffectData Poisoned = new StatusEffectData
    {
        id = "poisoned",
        effectType = StatusEffect.EffectType.POISONED,
        displayName = "Poisoned",
        damagePerRound = 5,
        isPureDamage = true,
    };

    public static readonly StatusEffectData Slowed = new StatusEffectData
    {
        id = "slowed",
        effectType = StatusEffect.EffectType.SLOWED,
        displayName = "Slowed",
        apEffect = StatusEffectData.APEffect.Modify,
        apModifier = -4,
        apFloor = 2,
        popupText = "Slowed",
    };

    public static readonly StatusEffectData Frozen = new StatusEffectData
    {
        id = "frozen",
        effectType = StatusEffect.EffectType.FROZEN,
        displayName = "Frozen",
        apEffect = StatusEffectData.APEffect.Zero,
        popupText = "Frozen",
    };

    public static readonly StatusEffectData Mobility = new StatusEffectData
    {
        id = "mobility",
        effectType = StatusEffect.EffectType.MOBILITY,
        displayName = "Mobility",
        apEffect = StatusEffectData.APEffect.Modify,
        apModifier = 2,
    };

    public static readonly StatusEffectData Burning = new StatusEffectData
    {
        id = "burning",
        effectType = StatusEffect.EffectType.BURNING,
        displayName = "Burning",
        damagePerRound = 10,
    };

    public static readonly StatusEffectData Rage = new StatusEffectData
    {
        id = "rage",
        effectType = StatusEffect.EffectType.RAGE,
        displayName = "Rage",
    };

    public static readonly StatusEffectData CannotDie = new StatusEffectData
    {
        id = "cannot_die",
        effectType = StatusEffect.EffectType.CANNOT_DIE,
        displayName = "Cannot Die",
    };

    public static readonly StatusEffectData Empower = new StatusEffectData
    {
        id = "empower",
        effectType = StatusEffect.EffectType.EMPOWER,
        displayName = "Empower",
    };

    public static readonly StatusEffectData Blinded = new StatusEffectData
    {
        id = "blinded",
        effectType = StatusEffect.EffectType.BLINDED,
        displayName = "Blinded",
    };

    public static readonly StatusEffectData Perfidy = new StatusEffectData
    {
        id = "perfidy",
        effectType = StatusEffect.EffectType.PERFIDY,
        displayName = "Perfidy",
    };

    public static readonly StatusEffectData Bulwark = new StatusEffectData
    {
        id = "bulwark",
        effectType = StatusEffect.EffectType.BULWARK,
        displayName = "Bulwark",
    };

    public static readonly StatusEffectData Hidden = new StatusEffectData
    {
        id = "hidden",
        effectType = StatusEffect.EffectType.HIDDEN,
        displayName = "Hidden",
    };

    public static readonly StatusEffectData[] All = new StatusEffectData[]
    {
        Regeneration, Knockdown, Petrified, Poisoned, Slowed,
        Frozen, Mobility, Burning, Rage, CannotDie, Empower,
        Blinded, Perfidy, Bulwark, Hidden,
    };
}
