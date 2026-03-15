public class ItemData
{
    public string id;
    public string displayName;
    public string description = "";
    public int stackSize = 1;
    public int weight = 0;
    public InventoryItem.ItemRarity rarity = InventoryItem.ItemRarity.Common;

    public virtual InventoryItem CreateInstance()
    {
        return new InventoryItem(displayName)
        {
            description = description,
            stackSize = stackSize,
            weight = weight,
            rarity = rarity,
        };
    }
}
