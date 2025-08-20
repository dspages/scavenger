using UnityEngine.UIElements;

public enum SlotType
{
    Inventory,
    Equipment,
    Trash
}

public class SlotModel
{
    public SlotType slotType;
    public int inventoryIndex;
    public EquippableItem.EquipmentSlot equipmentSlot;
    public VisualElement rootElement;

    private SlotModel() {}

    public static SlotModel CreateInventory(VisualElement root, int index)
    {
        return new SlotModel
        {
            slotType = SlotType.Inventory,
            inventoryIndex = index,
            rootElement = root
        };
    }

    public static SlotModel CreateEquipment(VisualElement root, EquippableItem.EquipmentSlot equipSlot)
    {
        return new SlotModel
        {
            slotType = SlotType.Equipment,
            equipmentSlot = equipSlot,
            rootElement = root
        };
    }

    public static SlotModel CreateTrash(VisualElement root)
    {
        return new SlotModel
        {
            slotType = SlotType.Trash,
            rootElement = root
        };
    }
}


