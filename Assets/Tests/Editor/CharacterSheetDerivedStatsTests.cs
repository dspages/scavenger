using NUnit.Framework;

public class CharacterSheetDerivedStatsTests
{
    private CharacterSheet MakeSheet(string name = "Test")
    {
        return new CharacterSheet(name, CharacterSheet.CharacterClass.CLASS_SOLDIER);
    }

    [Test]
    public void GetMeleeDamageBonus_IsStrengthHalved()
    {
        var s = MakeSheet();
        s.strength = 9;
        Assert.AreEqual(4, s.GetMeleeDamageBonus());
        s.strength = 10;
        Assert.AreEqual(5, s.GetMeleeDamageBonus());
    }

    [Test]
    public void GetBackstabDamageBonus_EqualsAgility()
    {
        var s = MakeSheet();
        s.agility = 7;
        Assert.AreEqual(7, s.GetBackstabDamageBonus());
    }

    [Test]
    public void GetTotalGearDodgeBonus_SumsEquipment()
    {
        var s = MakeSheet();
        var boot = new EquippableItem("Boots", EquippableItem.EquipmentSlot.Boots) { dodgeBonus = 2 };
        var ring = new EquippableItem("Ring", EquippableItem.EquipmentSlot.LeftRing) { dodgeBonus = 1 };
        s.TryEquipItem(boot);
        s.TryEquipItem(ring);
        Assert.AreEqual(3, s.GetTotalGearDodgeBonus());
    }

    [Test]
    public void GetRangedEvasionPoints_AgilityPlusGearDodge()
    {
        var s = MakeSheet();
        s.agility = 7;
        s.TryEquipItem(new EquippableItem("R", EquippableItem.EquipmentSlot.LeftRing) { dodgeBonus = 2 });
        Assert.AreEqual(9, s.GetRangedEvasionPoints());
    }

    [Test]
    public void GetCritChancePercent_BaseAgilityAndEmpower()
    {
        var s = MakeSheet();
        s.agility = 4;
        Assert.AreEqual(10, s.GetCritChancePercent());
        new StatusEffect(StatusEffect.EffectType.EMPOWER, 3, s);
        Assert.AreEqual(20, s.GetCritChancePercent());
    }
}
