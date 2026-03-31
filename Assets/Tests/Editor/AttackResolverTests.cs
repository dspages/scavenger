using NUnit.Framework;

public class AttackResolverTests
{
    private CharacterSheet MakeSheet(string name = "Test")
    {
        return new CharacterSheet(name, CharacterSheet.CharacterClass.CLASS_SOLDIER);
    }

    private EquippableHandheld MakeSword(int damage = 5)
    {
        return new EquippableHandheld("Cutlass", EquippableHandheld.WeaponType.OneHanded,
            damage, 1, 1, 10, DamageType.Slashing);
    }

    // --- Basic damage ---

    [Test]
    public void Resolve_WithWeapon_UsesWeaponDamage()
    {
        var attacker = MakeSheet("Attacker");
        var defender = MakeSheet("Defender");
        var weapon = MakeSword(damage: 8);
        var context = AttackContext.Melee(weapon);

        int hpBefore = defender.currentHealth;
        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.IsTrue(result.hit);
        Assert.Greater(result.damageDealt, 0);
        Assert.AreEqual(hpBefore - result.damageDealt, defender.currentHealth);
    }

    [Test]
    public void Resolve_WithoutWeapon_UsesUnarmedDamage()
    {
        var attacker = MakeSheet("Attacker");
        var defender = MakeSheet("Defender");
        var context = AttackContext.Melee(weapon: null);

        int hpBefore = defender.currentHealth;
        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.IsTrue(result.hit);
        Assert.Greater(result.damageDealt, 0);
        Assert.AreEqual(hpBefore - result.damageDealt, defender.currentHealth);
    }

    [Test]
    public void Resolve_WeaponTakesPriority_OverUnarmed()
    {
        var attacker = MakeSheet("Attacker");
        var defender = MakeSheet("Defender");
        var weapon = MakeSword(damage: 20);
        var context = AttackContext.Melee(weapon);

        var result = AttackResolver.Resolve(attacker, defender, context);

        // Weapon damage 20 should produce much higher output than unarmed (3)
        Assert.GreaterOrEqual(result.damageDealt, 10);
    }

    // --- Death ---

    [Test]
    public void Resolve_KillsDefender_SetsDefenderDied()
    {
        var attacker = MakeSheet("Attacker");
        var defender = MakeSheet("Defender");
        defender.ReceiveDamage(defender.currentHealth - 1);
        var context = AttackContext.Melee(MakeSword(damage: 50));

        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.IsTrue(result.defenderDied);
        Assert.IsTrue(defender.dead);
    }

    [Test]
    public void Resolve_DoesNotKill_DefenderDiedIsFalse()
    {
        var attacker = MakeSheet("Attacker");
        var defender = MakeSheet("Defender");
        var context = AttackContext.Melee(MakeSword(damage: 1));

        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.IsFalse(result.defenderDied);
        Assert.IsFalse(defender.dead);
    }

    // --- Log messages ---

    [Test]
    public void Resolve_WithWeapon_LogIncludesWeaponName()
    {
        var attacker = MakeSheet("Rook");
        var defender = MakeSheet("Goblin");
        var weapon = MakeSword();
        var context = AttackContext.Melee(weapon);

        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.IsTrue(result.logMessage.Contains("Rook"));
        Assert.IsTrue(result.logMessage.Contains("Goblin"));
        Assert.IsTrue(result.logMessage.Contains("Cutlass"));
    }

    [Test]
    public void Resolve_Hit_LogIncludesDamageType()
    {
        var attacker = MakeSheet("Rook");
        var defender = MakeSheet("Goblin");
        var weapon = MakeSword();
        var context = AttackContext.Melee(weapon);

        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.IsTrue(result.logMessage.Contains("Slashing"));
    }

    [Test]
    public void Resolve_Kill_LogIncludesSlainMessage()
    {
        var attacker = MakeSheet("Rook");
        var defender = MakeSheet("Goblin");
        defender.ReceiveDamage(defender.currentHealth - 1);
        var context = AttackContext.Melee(MakeSword(damage: 50));

        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.IsTrue(result.logMessage.Contains("slain"));
    }

    // --- Result fields ---

    [Test]
    public void Resolve_SetsAttackerAndDefenderNames()
    {
        var attacker = MakeSheet("Alice");
        var defender = MakeSheet("Bob");
        var context = AttackContext.Melee(weapon: null);

        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.AreEqual("Alice", result.attackerName);
        Assert.AreEqual("Bob", result.defenderName);
    }

    [Test]
    public void Resolve_SetsWeaponName_WhenPresent()
    {
        var attacker = MakeSheet();
        var defender = MakeSheet();
        var weapon = MakeSword();
        var context = AttackContext.Melee(weapon);

        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.AreEqual("Cutlass", result.weaponName);
    }

    [Test]
    public void Resolve_WeaponNameNull_WhenNoWeapon()
    {
        var attacker = MakeSheet();
        var defender = MakeSheet();
        var context = AttackContext.Melee(weapon: null);

        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.IsNull(result.weaponName);
    }

    // --- DamageType ---

    [Test]
    public void Resolve_DamageType_ComesFromWeapon()
    {
        var attacker = MakeSheet();
        var defender = MakeSheet();
        var weapon = new EquippableHandheld("Mace", EquippableHandheld.WeaponType.OneHanded,
            5, 1, 1, 10, DamageType.Bludgeoning);
        var context = AttackContext.Melee(weapon);

        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.AreEqual(DamageType.Bludgeoning, result.damageType);
    }

    [Test]
    public void Resolve_DamageType_DefaultsBludgeoning_WhenUnarmed()
    {
        var attacker = MakeSheet();
        var defender = MakeSheet();
        var context = AttackContext.Melee(weapon: null);

        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.AreEqual(DamageType.Bludgeoning, result.damageType);
    }

    [Test]
    public void Resolve_DamageType_ComesFromAbility()
    {
        var attacker = MakeSheet();
        var defender = MakeSheet();
        var ability = new AbilityData
        {
            id = "test_fire",
            damage = 10,
            damageType = DamageType.Fire,
            archetype = AbilityData.Archetype.GroundAttack,
        };
        var context = AttackContext.AreaEffect(distance: 3, ability: ability);

        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.AreEqual(DamageType.Fire, result.damageType);
    }

    // --- Armor reduction ---

    [Test]
    public void Resolve_ArmorReducesDamage()
    {
        var attacker = MakeSheet("Attacker");
        var unarmored = MakeSheet("Unarmored");
        var armored = MakeSheet("Armored");
        var plateArmor = new EquippableItem("Plate", EquippableItem.EquipmentSlot.Armor) { armorBonus = 5 };
        armored.TryEquipItem(plateArmor);

        var weapon = MakeSword(damage: 10);
        var context = AttackContext.Melee(weapon);

        int totalUnarmored = 0;
        int totalArmored = 0;
        for (int i = 0; i < 30; i++)
        {
            var u = MakeSheet();
            var a = MakeSheet();
            a.TryEquipItem(new EquippableItem("Plate", EquippableItem.EquipmentSlot.Armor) { armorBonus = 5 });
            var ru = AttackResolver.Resolve(attacker, u, context);
            var ra = AttackResolver.Resolve(attacker, a, context);
            totalUnarmored += ru.damageDealt;
            totalArmored += ra.damageDealt;
        }

        Assert.Greater(totalUnarmored, totalArmored, "Armored targets should take less total damage");
    }

    [Test]
    public void GetTotalArmor_SumsEquipmentAndBulwark()
    {
        var sheet = MakeSheet();
        var armor = new EquippableItem("Plate", EquippableItem.EquipmentSlot.Armor) { armorBonus = 3 };
        sheet.TryEquipItem(armor);
        new StatusEffect(StatusEffect.EffectType.BULWARK, 3, sheet, 2);

        int total = sheet.GetTotalArmor();

        Assert.AreEqual(5, total);
    }

    // --- Strength bonus for melee ---

    [Test]
    public void Resolve_MeleeAttack_IncludesStrengthBonus()
    {
        var attacker = MakeSheet("Brute");
        attacker.strength = 10;
        var weakAttacker = MakeSheet("Weakling");
        weakAttacker.strength = 0;
        var weapon = MakeSword(damage: 6);

        int totalStrong = 0;
        int totalWeak = 0;
        for (int i = 0; i < 30; i++)
        {
            var d1 = MakeSheet();
            var d2 = MakeSheet();
            var context = AttackContext.Melee(weapon);
            totalStrong += AttackResolver.Resolve(attacker, d1, context).damageDealt;
            totalWeak += AttackResolver.Resolve(weakAttacker, d2, context).damageDealt;
        }

        Assert.Greater(totalStrong, totalWeak, "High-strength melee should deal more damage");
    }

    // --- Melee always hits ---

    [Test]
    public void Resolve_MeleeAlwaysHits()
    {
        var attacker = MakeSheet();
        var defender = MakeSheet();
        var context = AttackContext.Melee(MakeSword());

        for (int i = 0; i < 50; i++)
        {
            var d = MakeSheet();
            var result = AttackResolver.Resolve(attacker, d, context);
            Assert.IsTrue(result.hit, $"Melee attack should always hit (iteration {i})");
        }
    }

    // --- AoE always hits ---

    [Test]
    public void Resolve_AoEAlwaysHits()
    {
        var attacker = MakeSheet();
        var context = AttackContext.AreaEffect(distance: 5);

        for (int i = 0; i < 20; i++)
        {
            var d = MakeSheet();
            var result = AttackResolver.Resolve(attacker, d, context);
            Assert.IsTrue(result.hit, $"AoE attack should always hit (iteration {i})");
        }
    }

    // --- Ranged context ---

    [Test]
    public void Ranged_WithWeapon_UsesWeaponDamage()
    {
        var attacker = MakeSheet("Archer");
        var defender = MakeSheet("Target");
        var musket = new EquippableHandheld("Long Musket",
            EquippableHandheld.WeaponType.TwoHanded, 12, 2, 10, 30,
            DamageType.Bludgeoning);
        var context = AttackContext.Ranged(musket, distance: 2);

        // Ranged can miss, so run multiple times
        bool anyHit = false;
        for (int i = 0; i < 50; i++)
        {
            var d = MakeSheet();
            var result = AttackResolver.Resolve(attacker, d, context);
            if (result.hit)
            {
                anyHit = true;
                Assert.Greater(result.damageDealt, 0);
            }
        }
        Assert.IsTrue(anyHit, "At least some ranged attacks should hit at close range");
    }

    // --- Ranged hit chance result fields ---

    [Test]
    public void Resolve_Ranged_HasHitChance()
    {
        var attacker = MakeSheet("Archer");
        var defender = MakeSheet("Target");
        var musket = new EquippableHandheld("Musket",
            EquippableHandheld.WeaponType.TwoHanded, 8, 2, 10, 30,
            DamageType.Piercing);
        var context = AttackContext.Ranged(musket, distance: 3);

        var result = AttackResolver.Resolve(attacker, defender, context);

        Assert.Greater(result.hitChance, 0f);
        Assert.LessOrEqual(result.hitChance, 1f);
    }

    // --- Damage floor at 0 ---

    [Test]
    public void Resolve_DamageNeverNegative()
    {
        var attacker = MakeSheet();
        attacker.strength = 0;
        var defender = MakeSheet();
        var heavyArmor = new EquippableItem("Adamantine", EquippableItem.EquipmentSlot.Armor) { armorBonus = 100 };
        defender.TryEquipItem(heavyArmor);
        var context = AttackContext.Melee(weapon: null);

        for (int i = 0; i < 20; i++)
        {
            var d = MakeSheet();
            d.TryEquipItem(new EquippableItem("Adamantine", EquippableItem.EquipmentSlot.Armor) { armorBonus = 100 });
            var result = AttackResolver.Resolve(attacker, d, context);
            Assert.GreaterOrEqual(result.damageDealt, 0, $"Damage should never be negative (iteration {i})");
        }
    }
}
