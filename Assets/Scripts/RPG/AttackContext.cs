public struct AttackContext
{
    public int distance;
    public EquippableHandheld weapon;
    public AbilityData abilityData;
    public bool isAoE;
    public bool isMelee;

    public static AttackContext Melee(EquippableHandheld weapon, AbilityData ability = null)
    {
        return new AttackContext
        {
            distance = 1,
            weapon = weapon,
            abilityData = ability,
            isAoE = false,
            isMelee = true,
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
