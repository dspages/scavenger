using UnityEngine;

public class InventoryItem
{
    public string itemName = "Unnamed Item";
    public string description = "";
    public Sprite icon = null;
    public int weight = 0;
    public ItemRarity rarity = ItemRarity.Common;

    private int maxStack = 1;
    private int stackCount = 1;

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public InventoryItem(string name)
    {
        itemName = name;
    }

    /// <summary>Current stack count (read-only).</summary>
    public int PeekStackSize() => stackCount;

    /// <summary>Maximum stack size for this item instance.</summary>
    public int MaxStack => maxStack;

    /// <summary>Sets max and current stack (e.g. from <see cref="ItemData"/> or save).</summary>
    public void ConfigureStacks(int max, int current)
    {
        maxStack = Mathf.Max(1, max);
        stackCount = Mathf.Clamp(current, 0, maxStack);
    }

    /// <summary>Returns false without mutating if amount is invalid or insufficient.</summary>
    public bool AttemptDecrementStackSize(int amount)
    {
        if (amount <= 0 || stackCount < amount)
            return false;
        stackCount -= amount;
        return true;
    }

    /// <summary>Adds to stack, clamped to <see cref="MaxStack"/>.</summary>
    public void IncrementStackSize(int amount)
    {
        if (amount <= 0) return;
        stackCount = Mathf.Min(maxStack, stackCount + amount);
    }

    public bool CanStackWith(InventoryItem other)
    {
        if (other == null) return false;
        return itemName == other.itemName && PeekStackSize() < MaxStack && other.PeekStackSize() < other.MaxStack;
    }

    public int TryStackWith(InventoryItem other)
    {
        if (!CanStackWith(other)) return 0;

        int spaceAvailable = MaxStack - PeekStackSize();
        int toTransfer = Mathf.Min(spaceAvailable, other.PeekStackSize());

        IncrementStackSize(toTransfer);
        other.AttemptDecrementStackSize(toTransfer);

        return toTransfer;
    }

    public virtual string GetDisplayName()
    {
        if (PeekStackSize() > 1)
            return $"{itemName} ({PeekStackSize()})";
        return itemName;
    }
}
