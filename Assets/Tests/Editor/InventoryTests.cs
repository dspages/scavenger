using NUnit.Framework;

public class InventoryTests
{
    [Test]
    public void Constructor_InitializesAllSlotsEmpty()
    {
        var inventory = new Inventory();

        Assert.AreEqual(Inventory.MaxSlots, inventory.Items.Count);
        Assert.AreEqual(0, inventory.FindFirstEmptySlot());
        Assert.IsNull(inventory.GetItem(0));
    }

    [Test]
    public void TryAddItem_StacksWithExistingCompatibleItem()
    {
        var inventory = new Inventory();
        var firstStack = new InventoryItem("Potion");
        firstStack.ConfigureStacks(10, 6);
        var secondStack = new InventoryItem("Potion");
        secondStack.ConfigureStacks(10, 3);

        Assert.IsTrue(inventory.TryAddItem(firstStack));
        Assert.IsTrue(inventory.TryAddItem(secondStack));

        Assert.AreEqual(9, inventory.GetItem(0).PeekStackSize());
        Assert.AreEqual(0, secondStack.PeekStackSize());
        Assert.IsTrue(inventory.IsSlotEmpty(1));
    }

    [Test]
    public void RemoveItem_RemovesSlotWhenStackDropsToZero()
    {
        var inventory = new Inventory();
        var potion = new InventoryItem("Potion");
        potion.ConfigureStacks(5, 2);
        inventory.TryAddItem(potion);

        Assert.IsTrue(inventory.RemoveItem(0, 2));
        Assert.IsNull(inventory.GetItem(0));
    }

    [Test]
    public void TryAddItem_ReturnsFalseWhenAllSlotsFull()
    {
        var inventory = new Inventory();
        for (int i = 0; i < Inventory.MaxSlots; i++)
        {
            var item = new InventoryItem($"UniqueSlot{i}");
            item.ConfigureStacks(1, 1);
            Assert.IsTrue(inventory.TryAddItem(item), $"slot {i}");
        }

        var overflow = new InventoryItem("Overflow");
        overflow.ConfigureStacks(1, 1);
        Assert.IsFalse(inventory.TryAddItem(overflow));
        Assert.AreEqual(-1, inventory.FindFirstEmptySlot());
    }
}
