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
            // Special handling for hand slots - allow hand-held items in either hand
            if (slot == EquippableItem.EquipmentSlot.LeftHand || slot == EquippableItem.EquipmentSlot.RightHand)
            {
                // Check if this is a hand-held item (weapon, shield, etc.)
                if (eq is EquippableHandheld)
                {
                    return IsHandSlotCompatible(slot, eq);
                }
                // Non-hand-held items must match their designated slot
                return eq.slot == slot;
            }
            
            // For non-hand slots, check basic slot compatibility
            if (eq.slot != slot) return false;
            return true;
        }
        return false;
    }

    private bool IsHandSlotCompatible(EquippableItem.EquipmentSlot slot, EquippableItem item)
    {
        if (item is not EquippableHandheld weapon) return true;
        
        // Get the other hand slot
        var otherSlot = slot == EquippableItem.EquipmentSlot.LeftHand 
            ? EquippableItem.EquipmentSlot.RightHand 
            : EquippableItem.EquipmentSlot.LeftHand;
        
        var otherWeapon = Get(otherSlot) as EquippableHandheld;
        
        // Edge case: If we're moving the same item between hand slots, always allow it
        // This prevents items from blocking themselves during repositioning
        if (otherWeapon != null && ReferenceEquals(otherWeapon, item))
        {
            return true;
        }
        
        // Check if weapons can be equipped together
        // This checks both directions: can the new weapon dual-wield with the existing one?
        return weapon.CanDualWieldWith(otherWeapon);
    }

    public virtual bool TryEquipToSlot(EquippableItem item, EquippableItem.EquipmentSlot slot, out EquippableItem previous)
    {
        previous = null;
        if (item == null) return false;
        
        // For hand slots, allow hand-held items regardless of their default slot
        if (slot == EquippableItem.EquipmentSlot.LeftHand || slot == EquippableItem.EquipmentSlot.RightHand)
        {
            if (item is EquippableHandheld)
            {
                // Check if this weapon can be equipped alongside what's in the other hand
                if (!IsHandSlotCompatible(slot, item))
                {
                    return false;
                }
                
                // Hand-held items can go in either hand slot
                slotToItem.TryGetValue(slot, out previous);
                
                // Only remove the item from other slots AFTER we've confirmed it can be equipped
                // This prevents item destruction on failed dual-wielding attempts
                RemoveItemFromAllSlots(item);
                
                slotToItem[slot] = item;
                OnEquipmentChanged?.Invoke();
                return true;
            }
        }
        
        // For non-hand slots or non-hand-held items, check slot compatibility
        if (item.slot != slot) return false;
        slotToItem.TryGetValue(slot, out previous);
        
        // Remove this item from any other slot it might be in to prevent duplication
        RemoveItemFromAllSlots(item);
        
        slotToItem[slot] = item;
        OnEquipmentChanged?.Invoke();
        return true;
    }

    private void RemoveItemFromAllSlots(EquippableItem item)
    {
        // Find and remove this item from any slot it's currently in
        var slotsToRemove = new List<EquippableItem.EquipmentSlot>();
        foreach (var kvp in slotToItem)
        {
            if (ReferenceEquals(kvp.Value, item))
            {
                slotsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var slot in slotsToRemove)
        {
            slotToItem.Remove(slot);
        }
    }

    public IReadOnlyDictionary<EquippableItem.EquipmentSlot, EquippableItem> GetAll()
    {
        return slotToItem;
    }
}


