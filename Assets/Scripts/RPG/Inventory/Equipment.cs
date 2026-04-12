using System.Collections.Generic;

public class Equipment
{
    static readonly EquippableItem.EquipmentSlot[] AllHandSlots =
    {
        EquippableItem.EquipmentSlot.LeftHand,
        EquippableItem.EquipmentSlot.RightHand,
        EquippableItem.EquipmentSlot.ExtraHand1,
        EquippableItem.EquipmentSlot.ExtraHand2,
    };

    public static bool IsPrimaryHandSlot(EquippableItem.EquipmentSlot slot)
    {
        return slot == EquippableItem.EquipmentSlot.LeftHand || slot == EquippableItem.EquipmentSlot.RightHand;
    }

    public static bool IsExtraHandSlot(EquippableItem.EquipmentSlot slot)
    {
        return slot == EquippableItem.EquipmentSlot.ExtraHand1 || slot == EquippableItem.EquipmentSlot.ExtraHand2;
    }

    public static bool IsAnyHandSlot(EquippableItem.EquipmentSlot slot)
    {
        return IsPrimaryHandSlot(slot) || IsExtraHandSlot(slot);
    }

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

    public virtual bool IsSlotCompatible(EquippableItem.EquipmentSlot slot, InventoryItem item)
    {
        if (item is EquippableItem eq)
        {
            // Special handling for hand slots - allow hand-held items in either hand
            if (IsAnyHandSlot(slot))
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

        // At most one shield-class item across all hand slots (KISS; revisit if stacking rules change).
        if (weapon.handedness == EquippableHandheld.Handedness.Shield)
        {
            foreach (var hs in AllHandSlots)
            {
                if (hs == slot) continue;
                var other = Get(hs) as EquippableHandheld;
                if (other != null && other.handedness == EquippableHandheld.Handedness.Shield
                    && !ReferenceEquals(other, item))
                    return false;
            }
        }

        if (IsExtraHandSlot(slot))
        {
            // Extra hand slots are reserved for light handhelds.
            if (weapon.tag != EquippableHandheld.HandheldTag.Light) return false;
            if (weapon.handedness == EquippableHandheld.Handedness.TwoHanded) return false;
            return true;
        }

        // Primary hand: check compatibility against the other primary hand only.
        var otherSlot = slot == EquippableItem.EquipmentSlot.LeftHand
            ? EquippableItem.EquipmentSlot.RightHand
            : EquippableItem.EquipmentSlot.LeftHand;

        var otherWeapon = Get(otherSlot) as EquippableHandheld;
        if (otherWeapon != null && ReferenceEquals(otherWeapon, item))
            return true;
        return weapon.CanDualWieldWith(otherWeapon);
    }

    public virtual bool TryEquipToSlot(EquippableItem item, EquippableItem.EquipmentSlot slot, out EquippableItem previous)
    {
        previous = null;
        if (item == null) return false;
        
        // For hand slots, allow hand-held items regardless of their default slot
        if (IsAnyHandSlot(slot))
        {
            if (item is EquippableHandheld)
            {
                if (!IsHandSlotCompatible(slot, item))
                {
                    return false;
                }
                
                slotToItem.TryGetValue(slot, out previous);
                RemoveItemFromAllSlots(item);

                // Reassign the item's slot to the target hand
                item.slot = slot;
                slotToItem[slot] = item;
                OnEquipmentChanged?.Invoke();
                return true;
            }
        }
        
        // For non-hand slots or non-hand-held items, check slot compatibility
        if (item.slot != slot) return false;
        slotToItem.TryGetValue(slot, out previous);
        
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


