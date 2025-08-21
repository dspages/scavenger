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
    private readonly VisualElement leftHandSlot;
    private readonly VisualElement rightHandSlot;
    private readonly VisualElement armorSlot;
    private readonly List<VisualElement> inventorySlots = new List<VisualElement>();
    
    private CharacterSheet currentCharacter;

    public InventoryUIManager(VisualElement panel, VisualElement grid, 
        VisualElement leftHand, VisualElement rightHand, VisualElement armor)
    {
        inventoryPanel = panel;
        itemGrid = grid;
        leftHandSlot = leftHand;
        rightHandSlot = rightHand;
        armorSlot = armor;
    }

    public void SetCurrentCharacter(CharacterSheet character)
    {
        currentCharacter = character;
    }

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
            slot.name = $"InventorySlot_{i}";
            
            attachHandlers?.Invoke(slot, i);
            
            itemGrid.Add(slot);
            inventorySlots.Add(slot);
        }
    }

    public void RefreshInventoryUI()
    {
        if (currentCharacter?.inventory == null || inventorySlots.Count == 0) return;

        var items = currentCharacter.inventory.Items;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            var slot = inventorySlots[i];
            var item = (i < items.Count) ? items[i] : null;
            CreateItemVisual(slot, item, includeStackCount: true);
        }
        
        // Re-apply click blocking to all inventory slots and their new children
        UIClickBlocker.MakeElementAndChildrenBlockClicks(itemGrid);
    }

    public void RefreshEquipmentUI(System.Action<VisualElement> attachTooltipHandlers)
    {
        if (currentCharacter == null) return;

        RefreshEquipmentSlot(leftHandSlot, EquippableItem.EquipmentSlot.LeftHand, attachTooltipHandlers);
        RefreshEquipmentSlot(rightHandSlot, EquippableItem.EquipmentSlot.RightHand, attachTooltipHandlers);
        RefreshEquipmentSlot(armorSlot, EquippableItem.EquipmentSlot.Armor, attachTooltipHandlers);
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
        if (includeStackCount && item.currentStack > 1)
        {
            var stackLabel = new Label(item.currentStack.ToString());
            stackLabel.AddToClassList("stack-count");
            stackLabel.tooltip = item.GetDisplayName();
            slot.Add(stackLabel);
        }

        // Set tooltip on slot itself
        slot.tooltip = item.GetDisplayName();
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
        if (leftHandSlot != null) action(leftHandSlot, EquippableItem.EquipmentSlot.LeftHand);
        if (rightHandSlot != null) action(rightHandSlot, EquippableItem.EquipmentSlot.RightHand);
        if (armorSlot != null) action(armorSlot, EquippableItem.EquipmentSlot.Armor);
    }

    public List<VisualElement> GetInventorySlots()
    {
        return inventorySlots;
    }

    public VisualElement GetEquipmentSlot(EquippableItem.EquipmentSlot equipmentSlot)
    {
        switch (equipmentSlot)
        {
            case EquippableItem.EquipmentSlot.LeftHand: return leftHandSlot;
            case EquippableItem.EquipmentSlot.RightHand: return rightHandSlot;
            case EquippableItem.EquipmentSlot.Armor: return armorSlot;
            default: return null;
        }
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
