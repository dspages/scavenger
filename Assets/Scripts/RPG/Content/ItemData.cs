public class ItemData
{
    public string id;
    public string displayName;
    public string description = "";
    private int maxStack = 1;
    public int weight = 0;
    public InventoryItem.ItemRarity rarity = InventoryItem.ItemRarity.Common;

    /// <summary>Maximum stack size for instances created from this definition.</summary>
    public int MaxStack
    {
        get => maxStack;
        set => maxStack = value < 1 ? 1 : value;
    }

    public virtual InventoryItem CreateInstance()
    {
        var item = new InventoryItem(displayName)
        {
            description = description,
            weight = weight,
            rarity = rarity,
        };
        item.ConfigureStacks(MaxStack, MaxStack);
        return item;
    }
}
