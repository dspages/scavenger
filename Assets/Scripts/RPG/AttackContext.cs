public struct AttackContext
{
    public int distance;
    public EquippableHandheld weapon;
    public AbilityData abilityData;
    public bool isAoE;
    public bool isMelee;
    /// <summary>0 = normal hit, 1 = one backstab condition, 2 = both (hidden + behind). Each count adds bonus damage.</summary>
    public int backstabCount;

    public static AttackContext Melee(EquippableHandheld weapon, AbilityData ability = null, int backstabCount = 0)
    {
        return new AttackContext
        {
            distance = 1,
            weapon = weapon,
            abilityData = ability,
            isAoE = false,
            isMelee = true,
            backstabCount = backstabCount,
        };
    }

    public static AttackContext Ranged(EquippableHandheld weapon, int distance, AbilityData ability = null)
    {
        return new AttackContext
        {
            distance = distance,
            weapon = weapon,
            abilityData = ability,
            isAoE = false,
            isMelee = false,
        };
    }

    public static AttackContext AreaEffect(int distance, AbilityData ability = null)
    {
        return new AttackContext
        {
            distance = distance,
            weapon = null,
            abilityData = ability,
            isAoE = true,
            isMelee = false,
        };
    }
}
