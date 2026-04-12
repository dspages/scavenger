public class AbilityData
{
    public enum Theme
    {
        Physical,
        Magic,
        Gadget,
    }

    public enum Archetype
    {
        MeleeAttack,
        RangedAttack,
        GroundAttack,
        SelfCast,
        AllyBuff,
    }

    public string id;
    public string displayName;
    public string description;

    /// <summary>Primary balance theme for this ability (used by UI and future per-theme systems).</summary>
    public Theme theme = Theme.Physical;
    public Archetype archetype;
    public int actionPointCost = 10;

    /// <summary>Arcane-style spend (morale / sanity bar).</summary>
    public int sanityCost;
    /// <summary>Tech ability spend (inventory stackables).</summary>
    public int techComponentsCost;
    /// <summary>Divine-style spend (mana crystals, inventory stackables).</summary>
    public int manaCrystalCost;
    /// <summary>Turns before the ability can be used again; 0 = no cooldown.</summary>
    public int cooldown = 0;

    public int damage;
    public DamageType damageType = DamageType.Bludgeoning;
    public int minRange = 1;
    public int maxRange = 1;
    public int radius;
    public float lungeDistance = 0.3f; // Cosmetic jolt of the character when they attack

    /// <summary>Optional override for ammo registry id when this ability supplies a different ammo type than the weapon default.</summary>
    public string ammoTypeOverride;

    /// <summary>Extra inventory items consumed when this ability resolves (registry id + amount).</summary>
    public ItemStackCost[] extraItemCosts;

    public StatusEffect.EffectType statusEffect;
    public int statusDuration;
    public int statusPowerLevel = -1;

    public string ArchetypeClassName()
    {
        return archetype switch
        {
            Archetype.MeleeAttack => nameof(ActionMeleeAttack),
            Archetype.RangedAttack => nameof(ActionRangedAttack),
            Archetype.GroundAttack => nameof(ActionGroundAttack),
            Archetype.SelfCast => nameof(ActionSelfCast),
            Archetype.AllyBuff => nameof(ActionAllyBuff),
            _ => nameof(ActionMeleeAttack),
        };
    }
}
