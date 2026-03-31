using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages all inventory and equipment UI visualization logic.
/// Handles slot creation, item visual rendering, and UI refresh operations.
/// </summary>
public class InventoryUIManager
{
    private readonly VisualElement inventoryPanel;
    private readonly VisualElement itemGrid;
    private readonly Dictionary<EquippableItem.EquipmentSlot, VisualElement> slotToElement = new Dictionary<EquippableItem.EquipmentSlot, VisualElement>();
    private readonly List<VisualElement> inventorySlots = new List<VisualElement>();

    private static readonly (EquippableItem.EquipmentSlot slot, string name)[] SlotNames = new[]
    {
        (EquippableItem.EquipmentSlot.Helmet, "HelmetSlot"),
        (EquippableItem.EquipmentSlot.Goggles, "GogglesSlot"),
        (EquippableItem.EquipmentSlot.Armor, "ArmorSlot"),
        (EquippableItem.EquipmentSlot.Gloves, "GlovesSlot"),
        (EquippableItem.EquipmentSlot.LeftHand, "LeftHandSlot"),
        (EquippableItem.EquipmentSlot.RightHand, "RightHandSlot"),
        (EquippableItem.EquipmentSlot.LeftRing, "LeftRingSlot"),
        (EquippableItem.EquipmentSlot.RightRing, "RightRingSlot"),
        (EquippableItem.EquipmentSlot.Boots, "BootsSlot"),
    };

    private CharacterSheet currentCharacter;
    /// <summary>When set, backpack grid reads/writes this inventory instead of <see cref="currentCharacter"/>.</summary>
    private Inventory boundInventory;
    private readonly string inventorySlotNamePrefix;

    public InventoryUIManager(VisualElement panel, VisualElement grid, VisualElement equipmentParent, string inventorySlotNamePrefix = "InventorySlot")
    {
        inventoryPanel = panel;
        itemGrid = grid;
        this.inventorySlotNamePrefix = string.IsNullOrEmpty(inventorySlotNamePrefix) ? "InventorySlot" : inventorySlotNamePrefix;
        if (equipmentParent != null)
        {
            foreach (var (slot, name) in SlotNames)
            {
                var el = equipmentParent.Q<VisualElement>(name);
                if (el != null)
                    slotToElement[slot] = el;
            }
        }
    }

    public void SetCurrentCharacter(CharacterSheet character)
    {
        currentCharacter = character;
        boundInventory = null;
    }

    /// <summary>Bind grid to a raw inventory (e.g. party stash). Clears character binding for backpack display.</summary>
    public void SetBoundInventory(Inventory inventory)
    {
        boundInventory = inventory;
        currentCharacter = null;
    }

    private Inventory ActiveBackpack => boundInventory ?? currentCharacter?.inventory;

    public void CreateInventorySlots(System.Action<VisualElement, int> attachHandlers)
    {
        if (itemGrid == null) return;

        inventorySlots.Clear();
        
        // Create 24 inventory slots (6x4 grid)
        for (int i = 0; i < Inventory.MaxSlots; i++)
        {
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");
            slot.AddToClassList("ui-blocker");
            slot.name = $"{inventorySlotNamePrefix}_{i}";
            
            attachHandlers?.Invoke(slot, i);
            
            itemGrid.Add(slot);
            inventorySlots.Add(slot);
        }
    }

    public void RefreshInventoryUI()
    {
        var inv = ActiveBackpack;
        if (inv == null || inventorySlots.Count == 0) return;

        var items = inv.Items;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            var slot = inventorySlots[i];
            var item = (i < items.Count) ? items[i] : null;
            CreateItemVisual(slot, item, includeStackCount: true);
        }
        
        // Re-apply click blocking to all inventory slots and their new children.
        // Note: we only need the top-most picked element to have "ui-blocker",
        // so mark the slots themselves, not every deep child.
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            UIClickBlocker.MakeElementBlockClicks(inventorySlots[i]);
        }
    }

    public void RefreshEquipmentUI(System.Action<VisualElement> attachTooltipHandlers)
    {
        if (currentCharacter == null) return;
        foreach (var kvp in slotToElement)
            RefreshEquipmentSlot(kvp.Value, kvp.Key, attachTooltipHandlers);
    }

    private void RefreshEquipmentSlot(VisualElement slot, EquippableItem.EquipmentSlot equipmentSlot, 
        System.Action<VisualElement> attachTooltipHandlers)
    {
        if (slot == null) return;

        // Ensure tooltip handlers are attached once
        attachTooltipHandlers?.Invoke(slot);
        var equippedItem = currentCharacter.GetEquippedItem(equipmentSlot);
        
        CreateItemVisual(slot, equippedItem, includeStackCount: false);
        
        // Re-apply click blocking to this slot and its children
        UIClickBlocker.MakeElementAndChildrenBlockClicks(slot);
    }

    private void CreateItemVisual(VisualElement slot, InventoryItem item, bool includeStackCount = false)
    {
        slot.Clear();
        slot.tooltip = null;
        ClearSlotItemFraming(slot);

        if (item == null) return;

        // Add icon if available
        if (item.icon != null)
        {
            var icon = new VisualElement();
            icon.AddToClassList("slot-icon");
            icon.style.backgroundImage = new StyleBackground(item.icon);
            icon.tooltip = item.GetDisplayName();
            slot.Add(icon);
        }
        else
        {
            // Placeholder text for items without icons
            var label = new Label(item.itemName.Substring(0, Mathf.Min(2, item.itemName.Length)));
            label.style.fontSize = 10;
            label.style.color = Color.white;
            label.tooltip = item.GetDisplayName();
            slot.Add(label);
        }

        // Add weapon type indicator for weapons
        if (item is EquippableHandheld weapon)
        {
            var typeLabel = new Label(GetWeaponTypeLabel(weapon.weaponType, weapon.rangeType));
            typeLabel.AddToClassList("weapon-type-label");
            typeLabel.style.fontSize = 8;
            typeLabel.style.color = GetWeaponTypeColor(weapon.weaponType);
            typeLabel.tooltip = item.GetDisplayName();
            slot.Add(typeLabel);
        }

        // Add stack count if > 1 and requested
        if (includeStackCount && item.PeekStackSize() > 1)
        {
            var stackLabel = new Label(item.PeekStackSize().ToString());
            stackLabel.AddToClassList("stack-count");
            stackLabel.tooltip = item.GetDisplayName();
            slot.Add(stackLabel);
        }

        // Set tooltip on slot itself
        slot.tooltip = item.GetDisplayName();

        switch (item.rarity)
        {
            case InventoryItem.ItemRarity.Uncommon:
                slot.AddToClassList("rarity-uncommon");
                break;
            case InventoryItem.ItemRarity.Rare:
                slot.AddToClassList("rarity-rare");
                break;
            case InventoryItem.ItemRarity.Epic:
                slot.AddToClassList("rarity-epic");
                break;
            case InventoryItem.ItemRarity.Legendary:
                slot.AddToClassList("rarity-legendary");
                break;
            default:
                slot.AddToClassList("rarity-common");
                break;
        }
    }

    private static void ClearSlotItemFraming(VisualElement slot)
    {
        if (slot == null) return;
        slot.RemoveFromClassList("rarity-common");
        slot.RemoveFromClassList("rarity-uncommon");
        slot.RemoveFromClassList("rarity-rare");
        slot.RemoveFromClassList("rarity-epic");
        slot.RemoveFromClassList("rarity-legendary");
    }

    public bool IsInventoryPanelOpen()
    {
        return inventoryPanel?.style.display == DisplayStyle.Flex;
    }

    public void SetInventoryPanelVisible(bool visible)
    {
        if (inventoryPanel == null) return;
        
        inventoryPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (visible)
        {
            inventoryPanel.BringToFront();
        }
    }

    public void ForEachEquipSlot(System.Action<VisualElement, EquippableItem.EquipmentSlot> action)
    {
        foreach (var kvp in slotToElement)
            action(kvp.Value, kvp.Key);
    }

    public List<VisualElement> GetInventorySlots()
    {
        return inventorySlots;
    }

    public VisualElement GetEquipmentSlot(EquippableItem.EquipmentSlot equipmentSlot)
    {
        return slotToElement.TryGetValue(equipmentSlot, out var el) ? el : null;
    }

    public bool TryGetSlotForElement(VisualElement element, out EquippableItem.EquipmentSlot slot)
    {
        foreach (var kvp in slotToElement)
        {
            if (kvp.Value == element)
            {
                slot = kvp.Key;
                return true;
            }
        }
        slot = default;
        return false;
    }

    private string GetWeaponTypeLabel(EquippableHandheld.WeaponType weaponType, EquippableHandheld.RangeType rangeType)
    {
        string typeLabel = weaponType switch
        {
            EquippableHandheld.WeaponType.OneHanded => "1H",
            EquippableHandheld.WeaponType.TwoHanded => "2H",
            EquippableHandheld.WeaponType.Shield => "S",
            _ => ""
        };
        
        string rangeLabel = rangeType switch
        {
            EquippableHandheld.RangeType.Melee => "M",
            EquippableHandheld.RangeType.Ranged => "R",
            _ => ""
        };
        
        return $"{typeLabel}/{rangeLabel}";
    }

    private Color GetWeaponTypeColor(EquippableHandheld.WeaponType weaponType)
    {
        return weaponType switch
        {
            EquippableHandheld.WeaponType.OneHanded => Color.cyan,
            EquippableHandheld.WeaponType.TwoHanded => Color.red,
            EquippableHandheld.WeaponType.Shield => Color.green,
            _ => Color.white
        };
    }
}
