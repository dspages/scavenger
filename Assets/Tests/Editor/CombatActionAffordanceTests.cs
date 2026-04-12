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

    [Test]
    public void CanAffordExtraItemCosts_MirrorsInventoryPresence()
    {
        var sheet = new CharacterSheet("t", CharacterSheet.CharacterClass.CLASS_SOLDIER, assignDefaults: false);
        var needBread = new AbilityData
        {
            extraItemCosts = new[] { new ItemStackCost { registryId = "bread", amount = 2 } },
        };
        Assert.IsFalse(CombatActionAffordance.CanAffordExtraItemCosts(needBread, sheet));

        var one = ContentRegistry.CreateItem("bread");
        one.ConfigureStacks(one.MaxStack, 3);
        Assert.IsTrue(sheet.inventory.TryAddItem(one));
        Assert.IsTrue(CombatActionAffordance.CanAffordExtraItemCosts(needBread, sheet));
    }

    [Test]
    public void AbilityTheme_DefaultsToPhysical()
    {
        var ability = new AbilityData();
        Assert.AreEqual(AbilityData.Theme.Physical, ability.theme);
    }

    [Test]
    public void AbilityCatalog_Fireball_IsMagicTheme()
    {
        var fireball = ContentRegistry.GetAbilityData("fireball");
        Assert.IsNotNull(fireball);
        Assert.AreEqual(AbilityData.Theme.Magic, fireball.theme);
    }
}
