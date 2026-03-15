public class WeaponData : ItemData
{
    public EquippableHandheld.WeaponType weaponType = EquippableHandheld.WeaponType.OneHanded;
    public EquippableHandheld.RangeType rangeType = EquippableHandheld.RangeType.Melee;
    public EquippableHandheld.DamageType damageType = EquippableHandheld.DamageType.Slashing;
    public int damage = 1;
    public int minRange = 1;
    public int maxRange = 1;
    public int actionPointCost = 10;
    public int splashRadius = 0;
    public float attackLungeDistance = 0.3f;
    public string associatedActionClass = nameof(ActionMeleeAttack);

    public int armorBonus = 0;
    public int dodgeBonus = 0;

    public bool providesIllumination = false;
    public int illuminationRange = 0;

    public bool isConsumable = false;
    public bool requiresAmmo = false;
    public string ammoType = "";

    public override InventoryItem CreateInstance()
    {
        return new EquippableHandheld(
            displayName, weaponType, damage, minRange, maxRange, actionPointCost, damageType)
        {
            description = description,
            stackSize = stackSize,
            weight = weight,
            rarity = rarity,
            rangeType = rangeType,
            splashRadius = splashRadius,
            attackLungeDistance = attackLungeDistance,
            associatedActionClass = associatedActionClass,
            armorBonus = armorBonus,
            dodgeBonus = dodgeBonus,
            providesIllumination = providesIllumination,
            illuminationRange = illuminationRange,
            isConsumable = isConsumable,
            requiresAmmo = requiresAmmo,
            ammoType = ammoType,
        };
    }
}
