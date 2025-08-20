using System.Collections.Generic;

public class Equipment
{
    public delegate void EquipmentChangedDelegate();
    public event EquipmentChangedDelegate OnEquipmentChanged;

    private readonly Dictionary<EquippableItem.EquipmentSlot, EquippableItem> slotToItem
        = new Dictionary<EquippableItem.EquipmentSlot, EquippableItem>();

    public bool TryEquip(EquippableItem item)
    {
        if (item == null)
        {
            return false;
        }

        // Replace any existing item in that slot
        slotToItem[item.slot] = item;
        OnEquipmentChanged?.Invoke();
        return true;
    }

    public virtual EquippableItem Unequip(EquippableItem.EquipmentSlot slot)
    {
        if (!slotToItem.TryGetValue(slot, out var existing))
        {
            return null;
        }

        slotToItem.Remove(slot);
        OnEquipmentChanged?.Invoke();
        return existing;
    }

    public virtual EquippableItem Get(EquippableItem.EquipmentSlot slot)
    {
        slotToItem.TryGetValue(slot, out var item);
        return item;
    }

    public bool IsSlotCompatible(EquippableItem.EquipmentSlot slot, InventoryItem item)
    {
        if (item is EquippableItem eq)
        {
            return eq.slot == slot;
        }
        return false;
    }

    public virtual bool TryEquipToSlot(EquippableItem item, EquippableItem.EquipmentSlot slot, out EquippableItem previous)
    {
        previous = null;
        if (item == null || item.slot != slot) return false;
        slotToItem.TryGetValue(slot, out previous);
        slotToItem[slot] = item;
        OnEquipmentChanged?.Invoke();
        return true;
    }

    public IReadOnlyDictionary<EquippableItem.EquipmentSlot, EquippableItem> GetAll()
    {
        return slotToItem;
    }
}


