using NUnit.Framework;

public class HitCalculatorTests
{
    private CharacterSheet MakeSheet(string name = "Test")
    {
        return new CharacterSheet(name, CharacterSheet.CharacterClass.CLASS_SOLDIER);
    }

    // --- Melee always hits ---

    [Test]
    public void CalculateHitChance_Melee_Returns1()
    {
        var attacker = MakeSheet();
        var defender = MakeSheet();
        var context = AttackContext.Melee(weapon: null);

        float chance = HitCalculator.CalculateHitChance(attacker, defender, context);

        Assert.AreEqual(1f, chance);
    }

    [Test]
    public void CalculateHit_Melee_AlwaysHits()
    {
        var attacker = MakeSheet();
        var defender = MakeSheet();
        var context = AttackContext.Melee(weapon: null);

        for (int i = 0; i < 50; i++)
        {
            var result = HitCalculator.CalculateHit(attacker, defender, context);
            Assert.IsTrue(result.hit);
        }
    }

    // --- AoE always hits ---

    [Test]
    public void CalculateHitChance_AoE_Returns1()
    {
        var attacker = MakeSheet();
        var defender = MakeSheet();
        var context = AttackContext.AreaEffect(distance: 5);

        float chance = HitCalculator.CalculateHitChance(attacker, defender, context);

        Assert.AreEqual(1f, chance);
    }

    // --- Ranged hit formula ---

    [Test]
    public void CalculateHitChance_Ranged_BaseCase()
    {
        var attacker = MakeSheet();
        attacker.perception = 4;
        attacker.agility = 4;
        var defender = MakeSheet();
        defender.agility = 0;
        var context = AttackContext.Ranged(weapon: null, distance: 4);

        // visionRange=8, distance 4 => 4 tiles under vision => 75% + 20% = 95%
        float chance = HitCalculator.CalculateHitChance(attacker, defender, context);

        Assert.AreEqual(0.95f, chance, 0.001f);
    }

    [Test]
    public void CalculateHitChance_Ranged_DistanceBeyondVisionRange_Penalty()
    {
        var attacker = MakeSheet();
        attacker.perception = 4;
        var defender = MakeSheet();
        defender.agility = 0;
        var context = AttackContext.Ranged(weapon: null, distance: 10);

        // visionRange=8, 2 tiles over => 75% - 40% = 35%
        float chance = HitCalculator.CalculateHitChance(attacker, defender, context);

        Assert.AreEqual(0.35f, chance, 0.001f);
    }

    [Test]
    public void CalculateHitChance_Ranged_DistanceUnderVisionRange_Bonus()
    {
        var attacker = MakeSheet();
        attacker.perception = 3;
        var defender = MakeSheet();
        defender.agility = 0;
        var context = AttackContext.Ranged(weapon: null, distance: 3);

        // visionRange=6, 3 tiles under => 75% + 15% = 90%
        float chance = HitCalculator.CalculateHitChance(attacker, defender, context);

        Assert.AreEqual(0.90f, chance, 0.001f);
    }

    [Test]
    public void CalculateHitChance_Ranged_DefenderAgility_Penalty()
    {
        var attacker = MakeSheet();
        attacker.perception = 4;
        var defender = MakeSheet();
        defender.agility = 4;
        var context = AttackContext.Ranged(weapon: null, distance: 4);

        // 95% attacker - (4 * 5%) = 75%
        float chance = HitCalculator.CalculateHitChance(attacker, defender, context);

        Assert.AreEqual(0.75f, chance, 0.001f);
    }

    [Test]
    public void CalculateHitChance_Ranged_DefenderGearDodge_Penalty()
    {
        var attacker = MakeSheet();
        attacker.perception = 4;
        var defender = MakeSheet();
        defender.agility = 0;
        defender.TryEquipItem(new EquippableItem("Boots", EquippableItem.EquipmentSlot.Boots) { dodgeBonus = 2 });
        var context = AttackContext.Ranged(weapon: null, distance: 4);

        // 95% - (2 evasion * 5%) = 85%
        float chance = HitCalculator.CalculateHitChance(attacker, defender, context);

        Assert.AreEqual(0.85f, chance, 0.001f);
    }

    [Test]
    public void CalculateHitChance_Ranged_ClampedMin()
    {
        var attacker = MakeSheet();
        attacker.perception = 1;
        var defender = MakeSheet();
        defender.agility = 20;
        var context = AttackContext.Ranged(weapon: null, distance: 10);

        float chance = HitCalculator.CalculateHitChance(attacker, defender, context);

        Assert.AreEqual(HitCalculator.MIN_HIT_CHANCE, chance, 0.001f);
    }

    [Test]
    public void CalculateHitChance_Ranged_ClampedMax()
    {
        var attacker = MakeSheet();
        attacker.perception = 20;
        var defender = MakeSheet();
        defender.agility = 0;
        var context = AttackContext.Ranged(weapon: null, distance: 1);

        float chance = HitCalculator.CalculateHitChance(attacker, defender, context);

        Assert.AreEqual(HitCalculator.MAX_HIT_CHANCE, chance, 0.001f);
    }

    [Test]
    public void CalculateHitChance_Ranged_BlindedPenalty()
    {
        var attacker = MakeSheet();
        attacker.perception = 4;
        new StatusEffect(StatusEffect.EffectType.BLINDED, 3, attacker);
        var defender = MakeSheet();
        defender.agility = 0;
        var context = AttackContext.Ranged(weapon: null, distance: 4);

        // 95% attacker parts - 40% blinded = 55%
        float chance = HitCalculator.CalculateHitChance(attacker, defender, context);

        Assert.AreEqual(0.55f, chance, 0.001f);
    }

    // --- Crit chance ---

    [Test]
    public void CalculateCritChance_BasedOnAgility()
    {
        var attacker = MakeSheet();
        attacker.agility = 4;
        var defender = MakeSheet();
        var context = AttackContext.Melee(weapon: null);

        // 2% + 2%*4 = 10%
        float chance = HitCalculator.CalculateCritChance(attacker, defender, context);

        Assert.AreEqual(0.10f, chance, 0.001f);
    }

    [Test]
    public void CalculateCritChance_ClampedMax()
    {
        var attacker = MakeSheet();
        attacker.agility = 100;
        var defender = MakeSheet();
        var context = AttackContext.Melee(weapon: null);

        float chance = HitCalculator.CalculateCritChance(attacker, defender, context);

        Assert.AreEqual(HitCalculator.MAX_CRIT_CHANCE, chance, 0.001f);
    }

    [Test]
    public void CalculateCritChance_EmpowerBonus()
    {
        var attacker = MakeSheet();
        attacker.agility = 4;
        new StatusEffect(StatusEffect.EffectType.EMPOWER, 3, attacker);
        var defender = MakeSheet();
        var context = AttackContext.Melee(weapon: null);

        // 2% + 2%*4 + 10% empower = 20%
        float chance = HitCalculator.CalculateCritChance(attacker, defender, context);

        Assert.AreEqual(0.20f, chance, 0.001f);
    }

    [Test]
    public void PublicClampConstants_AlignWithRpgCombatBalancePercents()
    {
        Assert.AreEqual(RpgCombatBalance.MinHitChancePercent / 100f, HitCalculator.MIN_HIT_CHANCE, 0.0001f);
        Assert.AreEqual(RpgCombatBalance.MaxHitChancePercent / 100f, HitCalculator.MAX_HIT_CHANCE, 0.0001f);
        Assert.AreEqual(RpgCombatBalance.MaxCritChancePercent / 100f, HitCalculator.MAX_CRIT_CHANCE, 0.0001f);
    }
}
