public class AbilityData
{
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

    public Archetype archetype;
    public int manaCost;
    public int actionPointCost = 10;

    public int damage;
    public EquippableHandheld.DamageType damageType = EquippableHandheld.DamageType.Bludgeoning;
    public int minRange = 1;
    public int maxRange = 1;
    public int radius;
    public float lungeDistance = 0.3f;

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
