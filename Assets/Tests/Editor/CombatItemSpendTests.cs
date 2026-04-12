using NUnit.Framework;

[TestFixture]
public class CombatItemSpendTests
{
    private CharacterSheet MakeSheet(bool assignDefaults = false)
    {
        return new CharacterSheet("T", CharacterSheet.CharacterClass.CLASS_SOLDIER, assignDefaults);
    }

    [Test]
    public void TryGetHandSlotFromActionKey_RightHand_Succeeds()
    {
        Assert.IsTrue(CombatItemSpend.TryGetHandSlotFromActionKey("MeleeAttack:RightHand", out var slot));
        Assert.AreEqual(EquippableItem.EquipmentSlot.RightHand, slot);
    }

    [Test]
    public void TryGetHandSlotFromActionKey_LeftHand_Succeeds()
    {
        Assert.IsTrue(CombatItemSpend.TryGetHandSlotFromActionKey("RangedAttack:LeftHand", out var slot));
        Assert.AreEqual(EquippableItem.EquipmentSlot.LeftHand, slot);
    }

    [Test]
    public void TryGetHandSlotFromActionKey_ExtraHand1_Succeeds()
    {
        Assert.IsTrue(CombatItemSpend.TryGetHandSlotFromActionKey("MeleeAttack:ExtraHand1", out var slot));
        Assert.AreEqual(EquippableItem.EquipmentSlot.ExtraHand1, slot);
    }

    [Test]
    public void TryGetHandSlotFromActionKey_ExtraHand2_Succeeds()
    {
        Assert.IsTrue(CombatItemSpend.TryGetHandSlotFromActionKey("MeleeAttack:ExtraHand2", out var slot));
        Assert.AreEqual(EquippableItem.EquipmentSlot.ExtraHand2, slot);
    }

    [Test]
    public void TryGetHandSlotFromActionKey_Invalid_Fails()
    {
        Assert.IsFalse(CombatItemSpend.TryGetHandSlotFromActionKey("NoHandSuffix", out _));
        Assert.IsFalse(CombatItemSpend.TryGetHandSlotFromActionKey(null, out _));
        Assert.IsFalse(CombatItemSpend.TryGetHandSlotFromActionKey("", out _));
    }

    [Test]
    public void HasStackInInventory_MatchesByRegistryDisplayName()
    {
        var sheet = MakeSheet();
        var item = ContentRegistry.CreateItem("health_potion");
        item.ConfigureStacks(item.MaxStack, 3);
        Assert.IsTrue(sheet.inventory.TryAddItem(item));

        Assert.IsTrue(CombatItemSpend.HasStackInInventory(sheet, "health_potion", 3));
        Assert.IsFalse(CombatItemSpend.HasStackInInventory(sheet, "health_potion", 4));
    }

    [Test]
    public void HasStackInInventory_InvalidRegistryOrAmount_ReturnsFalse()
    {
        var sheet = MakeSheet();
        Assert.IsFalse(CombatItemSpend.HasStackInInventory(null, "health_potion", 1));
        Assert.IsFalse(CombatItemSpend.HasStackInInventory(sheet, "not_a_real_id", 1));
        Assert.IsFalse(CombatItemSpend.HasStackInInventory(sheet, "health_potion", 0));
        Assert.IsFalse(CombatItemSpend.HasStackInInventory(sheet, null, 1));
    }

    [Test]
    public void CountStackInInventory_SumsAcrossSlots()
    {
        var sheet = MakeSheet();
        var a = ContentRegistry.CreateItem("musket_ball");
        a.ConfigureStacks(a.MaxStack, 5);
        var b = ContentRegistry.CreateItem("musket_ball");
        b.ConfigureStacks(b.MaxStack, 7);
        Assert.IsTrue(sheet.inventory.TryAddItem(a));
        Assert.IsTrue(sheet.inventory.TryAddItem(b));

        Assert.AreEqual(12, CombatItemSpend.CountStackInInventory(sheet, "musket_ball"));
    }

    [Test]
    public void TryConsumeStackByRegistryId_RemovesFromFirstMatchingSlot()
    {
        var sheet = MakeSheet();
        var stack = ContentRegistry.CreateItem("mana_crystal");
        stack.ConfigureStacks(stack.MaxStack, 4);
        Assert.IsTrue(sheet.inventory.TryAddItem(stack));

        Assert.IsTrue(CombatItemSpend.TryConsumeStackByRegistryId(sheet, CombatActionAffordance.ManaCrystalRegistryId, 2));
        Assert.AreEqual(2, sheet.inventory.GetItem(0).PeekStackSize());
    }

    [Test]
    public void TryConsumeStackByRegistryId_ClearsSlotWhenEmpty()
    {
        var sheet = MakeSheet();
        var stack = ContentRegistry.CreateItem("tech_component");
        stack.ConfigureStacks(stack.MaxStack, 1);
        Assert.IsTrue(sheet.inventory.TryAddItem(stack));

        Assert.IsTrue(CombatItemSpend.TryConsumeStackByRegistryId(sheet, CombatActionAffordance.TechComponentRegistryId, 1));
        Assert.IsNull(sheet.inventory.GetItem(0));
    }

    [Test]
    public void TrySpendExtraItemCosts_AllPresent_Succeeds()
    {
        var sheet = MakeSheet();
        var reg = ContentRegistry.CreateItem("health_potion");
        reg.ConfigureStacks(reg.MaxStack, 5);
        Assert.IsTrue(sheet.inventory.TryAddItem(reg));

        var ability = new AbilityData
        {
            extraItemCosts = new[] { new ItemStackCost { registryId = "health_potion", amount = 2 } },
        };

        Assert.IsTrue(CombatItemSpend.TrySpendExtraItemCosts(ability, sheet));
        Assert.AreEqual(3, sheet.inventory.GetItem(0).PeekStackSize());
    }

    [Test]
    public void TrySpendExtraItemCosts_NotEnough_ReturnsFalse()
    {
        var sheet = MakeSheet();
        var reg = ContentRegistry.CreateItem("health_potion");
        reg.ConfigureStacks(reg.MaxStack, 1);
        Assert.IsTrue(sheet.inventory.TryAddItem(reg));

        var ability = new AbilityData
        {
            extraItemCosts = new[] { new ItemStackCost { registryId = "health_potion", amount = 3 } },
        };

        Assert.IsFalse(CombatItemSpend.TrySpendExtraItemCosts(ability, sheet));
        Assert.AreEqual(1, sheet.inventory.GetItem(0).PeekStackSize());
    }

    [Test]
    public void TrySpendAbilityHardCosts_ManaCrystalsAndTech_Succeeds()
    {
        var sheet = MakeSheet();
        var mc = ContentRegistry.CreateItem("mana_crystal");
        mc.ConfigureStacks(mc.MaxStack, 2);
        var tc = ContentRegistry.CreateItem("tech_component");
        tc.ConfigureStacks(tc.MaxStack, 1);
        Assert.IsTrue(sheet.inventory.TryAddItem(mc));
        Assert.IsTrue(sheet.inventory.TryAddItem(tc));

        var ability = new AbilityData { manaCrystalCost = 2, techComponentsCost = 1 };

        Assert.IsTrue(CombatItemSpend.TrySpendAbilityHardCosts(ability, sheet));
        Assert.IsNull(sheet.inventory.GetItem(0));
        Assert.IsNull(sheet.inventory.GetItem(1));
    }

    [Test]
    public void ApplySanityCost_ReducesSanity()
    {
        var sheet = MakeSheet();
        sheet.currentSanity = 20;
        var ability = new AbilityData { sanityCost = 7 };

        CombatItemSpend.ApplySanityCost(ability, sheet);

        Assert.AreEqual(13, sheet.currentSanity);
    }

    [Test]
    public void ApplySanityCost_NoCostOrNull_NoOp()
    {
        var sheet = MakeSheet();
        int before = sheet.currentSanity;
        CombatItemSpend.ApplySanityCost(new AbilityData { sanityCost = 0 }, sheet);
        CombatItemSpend.ApplySanityCost(null, sheet);
        Assert.AreEqual(before, sheet.currentSanity);
    }
}
