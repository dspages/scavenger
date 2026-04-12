using NUnit.Framework;

[TestFixture]
public class TooltipTextBuilderTests
{
    [Test]
    public void ForItem_Null_ReturnsNullPair()
    {
        var (compact, detailed) = TooltipTextBuilder.ForItem(null);
        Assert.IsNull(compact);
        Assert.IsNull(detailed);
    }

    [Test]
    public void ForEquipped_EmptySlot_ShowsEmptyCompact()
    {
        var (compact, detailed) = TooltipTextBuilder.ForEquipped(null, EquippableItem.EquipmentSlot.RightHand);
        Assert.IsTrue(compact.Contains("Right hand"));
        Assert.IsTrue(compact.Contains("empty"));
        Assert.IsNull(detailed);
    }

    [Test]
    public void ForEquipped_EmptyExtraHand1_UsesPrehensileTailLabel()
    {
        var (compact, _) = TooltipTextBuilder.ForEquipped(null, EquippableItem.EquipmentSlot.ExtraHand1);
        Assert.IsTrue(compact.Contains("Prehensile Tail"));
        Assert.IsTrue(compact.Contains("empty"));
    }

    [Test]
    public void ForItem_StackedPotion_IncludesStackInCompactAndDetailed()
    {
        var potion = ContentRegistry.CreateItem("health_potion");
        potion.ConfigureStacks(potion.MaxStack, 3);

        var (compact, detailed) = TooltipTextBuilder.ForItem(potion);

        Assert.IsTrue(compact.Contains("3"));
        Assert.IsNotNull(detailed);
        Assert.IsTrue(detailed.Contains("Stack:"));
    }

    [Test]
    public void ForItem_Weapon_IncludesDamageAndRangeLines()
    {
        var musket = ContentRegistry.CreateItem("long_musket") as EquippableHandheld;
        Assert.IsNotNull(musket);

        var (compact, detailed) = TooltipTextBuilder.ForItem(musket);

        Assert.IsNotNull(compact);
        Assert.IsNotNull(detailed);
        Assert.IsTrue(detailed.Contains("Damage:"));
        Assert.IsTrue(detailed.Contains("Range:"));
    }
}
