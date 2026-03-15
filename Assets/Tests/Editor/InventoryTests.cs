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
        var firstStack = new InventoryItem("Potion") { stackSize = 10, currentStack = 6 };
        var secondStack = new InventoryItem("Potion") { stackSize = 10, currentStack = 3 };

        Assert.IsTrue(inventory.TryAddItem(firstStack));
        Assert.IsTrue(inventory.TryAddItem(secondStack));

        Assert.AreEqual(9, inventory.GetItem(0).currentStack);
        Assert.AreEqual(0, secondStack.currentStack);
        Assert.IsTrue(inventory.IsSlotEmpty(1));
    }

    [Test]
    public void RemoveItem_RemovesSlotWhenStackDropsToZero()
    {
        var inventory = new Inventory();
        var potion = new InventoryItem("Potion") { stackSize = 5, currentStack = 2 };
        inventory.TryAddItem(potion);

        Assert.IsTrue(inventory.RemoveItem(0, 2));
        Assert.IsNull(inventory.GetItem(0));
    }
}
