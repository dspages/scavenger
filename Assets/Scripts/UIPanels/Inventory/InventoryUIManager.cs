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
    private readonly VisualElement equipmentGrid;
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
        equipmentGrid = equipmentParent?.Q<VisualElement>(className: "equipment-grid");
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

        foreach (var kvp in slotToElement)
        {
            RegisterEquipmentSlotCursor(kvp.Key, kvp.Value);
        }
    }

    public void SetCurrentCharacter(CharacterSheet character)
    {
        currentCharacter = character;
        boundInventory = null;
        ConfigureSpeciesSlotLayout();
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
            int slotIndex = i;
            UiToolkitScavengerCursors.RegisterDraggableItemSlotPointerHover(slot, () =>
            {
                var inv = ActiveBackpack;
                if (inv == null) return false;
                var item = inv.GetItem(slotIndex);
                return item != null;
            });

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
        {
            if (kvp.Value.style.display == DisplayStyle.None) continue;
            RefreshEquipmentSlot(kvp.Value, kvp.Key, attachTooltipHandlers);
        }
    }

    private void RefreshEquipmentSlot(VisualElement slot, EquippableItem.EquipmentSlot equipmentSlot, 
        System.Action<VisualElement> attachTooltipHandlers)
    {
        if (slot == null) return;

        // Ensure tooltip handlers are attached once
        attachTooltipHandlers?.Invoke(slot);
        var equippedItem = currentCharacter.GetEquippedItem(equipmentSlot);
        
        CreateItemVisual(slot, equippedItem, includeStackCount: false);
        if (equippedItem == null)
            slot.tooltip = $"{EquippableItem.GetEquipmentSlotDisplayName(equipmentSlot)} (empty)";

        // Block world clicks using the slot only; children use PickingMode.Ignore so the slot owns cursor + tooltips.
        UIClickBlocker.MakeElementBlockClicks(slot);
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
            var typeLabel = new Label(GetWeaponTypeLabel(weapon.handedness, weapon.rangeType));
            typeLabel.AddToClassList("weapon-type-label");
            typeLabel.style.fontSize = 8;
            typeLabel.style.color = GetWeaponTypeColor(weapon.handedness);
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

        SetSlotContentPickingIgnore(slot);
    }

    /// <summary>
    /// Children must not steal pointer hit-tests or UITK will show the default cursor over text/icons instead of the slot cursor.
    /// </summary>
    static void SetSlotContentPickingIgnore(VisualElement slot)
    {
        if (slot == null) return;
        for (int i = 0; i < slot.childCount; i++)
            SetSubtreePickingIgnore(slot[i]);
    }

    static void SetSubtreePickingIgnore(VisualElement ve)
    {
        if (ve == null) return;
        ve.pickingMode = PickingMode.Ignore;
        for (int i = 0; i < ve.childCount; i++)
            SetSubtreePickingIgnore(ve[i]);
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
        {
            if (kvp.Value.style.display == DisplayStyle.None) continue;
            action(kvp.Value, kvp.Key);
        }
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

    private void RegisterEquipmentSlotCursor(EquippableItem.EquipmentSlot eqSlot, VisualElement el)
    {
        if (el == null) return;
        UiToolkitScavengerCursors.RegisterDraggableItemSlotPointerHover(el,
            () => currentCharacter != null && currentCharacter.GetEquippedItem(eqSlot) != null);
    }

    private void ConfigureSpeciesSlotLayout()
    {
        foreach (var (slot, _) in SlotNames)
        {
            if (!slotToElement.TryGetValue(slot, out var el) || el == null) continue;
            bool allowed = currentCharacter == null || currentCharacter.CanUseEquipmentSlot(slot);
            el.style.display = allowed ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Clean up any prior dynamic extra-hand slots.
        RemoveDynamicSlot(EquippableItem.EquipmentSlot.ExtraHand1);
        RemoveDynamicSlot(EquippableItem.EquipmentSlot.ExtraHand2);

        if (currentCharacter == null || equipmentGrid == null) return;
        var extraSlots = currentCharacter.GetExtraHandSlots();
        for (int i = 0; i < extraSlots.Count; i++)
        {
            EnsureDynamicExtraHandSlot(extraSlots[i], i, extraSlots.Count);
        }
    }

    private void RemoveDynamicSlot(EquippableItem.EquipmentSlot slot)
    {
        if (!slotToElement.TryGetValue(slot, out var old) || old == null) return;
        old.RemoveFromHierarchy();
        slotToElement.Remove(slot);
    }

    private void EnsureDynamicExtraHandSlot(EquippableItem.EquipmentSlot slot, int index, int total)
    {
        if (slotToElement.ContainsKey(slot)) return;
        var template = slotToElement.TryGetValue(EquippableItem.EquipmentSlot.RightHand, out var right)
            ? right
            : null;
        float tw = template != null ? template.resolvedStyle.width : 48f;
        float th = template != null ? template.resolvedStyle.height : 48f;
        // resolvedStyle is often NaN until after layout; NaN width/height makes the slot invisible.
        const float fallbackHandSlot = 48f;
        if (float.IsNaN(tw) || tw <= 0f) tw = fallbackHandSlot;
        if (float.IsNaN(th) || th <= 0f) th = fallbackHandSlot;
        var el = new VisualElement();
        el.name = slot == EquippableItem.EquipmentSlot.ExtraHand1 ? "PrehensileTailSlot" : "ExtraHand2Slot";
        el.AddToClassList("equipment-slot");
        el.AddToClassList("ui-blocker");
        el.style.position = Position.Absolute;
        el.style.width = tw;
        el.style.height = th;

        // Place extras below the regular hand row.
        float top = 252f;
        if (total <= 1)
        {
            el.style.left = 66f;
        }
        else
        {
            el.style.left = index == 0 ? 36f : 96f;
        }
        el.style.top = top;
        equipmentGrid.Add(el);
        slotToElement[slot] = el;
        RegisterEquipmentSlotCursor(slot, el);
    }

    private string GetWeaponTypeLabel(EquippableHandheld.Handedness handedness, EquippableHandheld.RangeType rangeType)
    {
        string typeLabel = handedness switch
        {
            EquippableHandheld.Handedness.OneHanded => "1H",
            EquippableHandheld.Handedness.TwoHanded => "2H",
            EquippableHandheld.Handedness.Shield => "S",
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

    private Color GetWeaponTypeColor(EquippableHandheld.Handedness handedness)
    {
        return handedness switch
        {
            EquippableHandheld.Handedness.OneHanded => Color.cyan,
            EquippableHandheld.Handedness.TwoHanded => Color.red,
            EquippableHandheld.Handedness.Shield => Color.green,
            _ => Color.white
        };
    }
}
