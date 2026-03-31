using NUnit.Framework;

[TestFixture]
public class CombatActionAffordanceTests
{
    [Test]
    public void CanAffordManaAndTechCosts_WithoutStacks_ReturnsFalseWhenCostPositive()
    {
        var sheet = new CharacterSheet("t", CharacterSheet.CharacterClass.CLASS_SOLDIER, assignDefaults: false);
        var needCrystals = new AbilityData { manaCrystalCost = 1 };
        Assert.IsFalse(CombatActionAffordance.CanAffordManaAndTechCosts(needCrystals, sheet));

        Assert.IsTrue(sheet.inventory.TryAddItem(ContentRegistry.CreateItem("mana_crystal")));
        Assert.IsTrue(CombatActionAffordance.CanAffordManaAndTechCosts(needCrystals, sheet));
    }

    [Test]
    public void ShouldWarnSanityRisk_WhenCostExceedsPool()
    {
        var sheet = new CharacterSheet("t", CharacterSheet.CharacterClass.CLASS_SOLDIER, assignDefaults: false);
        sheet.currentSanity = 5;
        var ab = new AbilityData { sanityCost = 10 };
        Assert.IsTrue(CombatActionAffordance.ShouldWarnSanityRisk(ab, sheet));
        Assert.IsFalse(CombatActionAffordance.ShouldWarnSanityRisk(new AbilityData { sanityCost = 3 }, sheet));
    }
}
