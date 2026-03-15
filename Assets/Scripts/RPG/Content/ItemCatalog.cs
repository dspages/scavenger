public static class ItemCatalog
{
    // ---- Consumables & Sundries ----

    public static readonly ItemData HealthPotion = new ItemData
    {
        id = "health_potion",
        displayName = "Health Potion",
        description = "Restores 50 HP",
        stackSize = 10,
    };

    public static readonly ItemData ManaPotion = new ItemData
    {
        id = "mana_potion",
        displayName = "Mana Potion",
        description = "Restores 30 MP",
        stackSize = 5,
    };

    public static readonly ItemData Bread = new ItemData
    {
        id = "bread",
        displayName = "Bread",
        description = "Basic food item",
        stackSize = 20,
    };

    public static readonly ItemData GoldCoin = new ItemData
    {
        id = "gold_coin",
        displayName = "Gold Coin",
        description = "Currency",
        stackSize = 99,
    };

    public static readonly ItemData MusketBall = new ItemData
    {
        id = "musket_ball",
        displayName = "Musket Ball",
        description = "Ammunition for muskets",
        stackSize = 50,
    };

    // ---- Melee Weapons ----

    public static readonly WeaponData Cutlass = new WeaponData
    {
        id = "cutlass",
        displayName = "Cutlass",
        description = "A basic sword",
        weaponType = EquippableHandheld.WeaponType.OneHanded,
        rangeType = EquippableHandheld.RangeType.Melee,
        damageType = EquippableHandheld.DamageType.Slashing,
        damage = 5,
        actionPointCost = 10,
    };

    public static readonly WeaponData IronDagger = new WeaponData
    {
        id = "iron_dagger",
        displayName = "Iron Dagger",
        description = "A quick dagger",
        weaponType = EquippableHandheld.WeaponType.OneHanded,
        rangeType = EquippableHandheld.RangeType.Melee,
        damageType = EquippableHandheld.DamageType.Piercing,
        damage = 2,
        actionPointCost = 4,
    };

    public static readonly WeaponData ReachPike = new WeaponData
    {
        id = "reach_pike",
        displayName = "Reach Pike",
        description = "A long pike that requires distance to use effectively",
        weaponType = EquippableHandheld.WeaponType.TwoHanded,
        rangeType = EquippableHandheld.RangeType.Melee,
        damageType = EquippableHandheld.DamageType.Piercing,
        damage = 10,
        minRange = 2,
        maxRange = 2,
        actionPointCost = 15,
    };

    // ---- Shields ----

    public static readonly WeaponData SteelShield = new WeaponData
    {
        id = "steel_shield",
        displayName = "Steel Shield",
        description = "A sturdy steel shield",
        weaponType = EquippableHandheld.WeaponType.Shield,
        rangeType = EquippableHandheld.RangeType.Melee,
        damageType = EquippableHandheld.DamageType.Bludgeoning,
        damage = 1,
        actionPointCost = 8,
        armorBonus = 2,
        dodgeBonus = 1,
    };

    // ---- Ranged Weapons ----

    public static readonly WeaponData LongMusket = new WeaponData
    {
        id = "long_musket",
        displayName = "Long Musket",
        description = "A musket that requires distance to avoid muzzle flash",
        weaponType = EquippableHandheld.WeaponType.TwoHanded,
        rangeType = EquippableHandheld.RangeType.Ranged,
        damageType = EquippableHandheld.DamageType.Bludgeoning,
        damage = 12,
        minRange = 2,
        maxRange = 10,
        actionPointCost = 30,
        requiresAmmo = true,
        ammoType = "Musket Ball",
        associatedActionClass = nameof(ActionRangedAttack),
        attackLungeDistance = -0.2f,
    };

    public static readonly WeaponData FragGrenade = new WeaponData
    {
        id = "frag_grenade",
        displayName = "Frag Grenade",
        description = "A grenade that explodes on impact - keep your distance!",
        weaponType = EquippableHandheld.WeaponType.OneHanded,
        rangeType = EquippableHandheld.RangeType.Ranged,
        damageType = EquippableHandheld.DamageType.Fire,
        damage = 12,
        minRange = 2,
        maxRange = 8,
        actionPointCost = 20,
        splashRadius = 2,
        isConsumable = true,
        attackLungeDistance = 0.1f,
        associatedActionClass = nameof(ActionGroundAttack),
    };

    // ---- Light Sources ----

    public static readonly WeaponData Torch = new WeaponData
    {
        id = "torch",
        displayName = "Torch",
        description = "A basic torch. Provides light and small fire damage.",
        weaponType = EquippableHandheld.WeaponType.OneHanded,
        rangeType = EquippableHandheld.RangeType.Melee,
        damageType = EquippableHandheld.DamageType.Fire,
        damage = 2,
        actionPointCost = 10,
        providesIllumination = true,
        illuminationRange = 12,
    };

    // ---- Armor ----

    public static readonly ArmorData LeatherArmor = new ArmorData
    {
        id = "leather_armor",
        displayName = "Leather Armor",
        description = "Basic protection",
        slot = EquippableItem.EquipmentSlot.Armor,
        armorBonus = 2,
    };

    // ---- Registry ----

    public static readonly ItemData[] All = new ItemData[]
    {
        HealthPotion, ManaPotion, Bread, GoldCoin, MusketBall,
        Cutlass, IronDagger, ReachPike,
        SteelShield,
        LongMusket, FragGrenade,
        Torch,
        LeatherArmor,
    };
}
