using UnityEngine;
using UnityEngine.UIElements;

public class DragDropController
{
    private readonly VisualElement uiRoot;
    private readonly System.Func<Inventory> getInventory;
    private readonly System.Func<Equipment> getEquipment;
    private readonly System.Action refreshInventoryUI;
    private readonly System.Action refreshEquipmentUI;

    private VisualElement dragGhost;
    public DragContext Context { get; } = new DragContext();

    public DragDropController(
        VisualElement uiRoot,
        System.Func<Inventory> getInventory,
        System.Func<Equipment> getEquipment,
        System.Action refreshInventoryUI,
        System.Action refreshEquipmentUI)
    {
        this.uiRoot = uiRoot;
        this.getInventory = getInventory;
        this.getEquipment = getEquipment;
        this.refreshInventoryUI = refreshInventoryUI;
        this.refreshEquipmentUI = refreshEquipmentUI;
    }

    public void BeginDrag(SlotModel source, InventoryItem item, Vector2 panelPos)
    {
        if (item == null) return;
        // Ensure any previous ghost is cleaned up to avoid artifacts
        if (dragGhost != null)
        {
            dragGhost.RemoveFromHierarchy();
            dragGhost = null;
        }
        Context.isDragging = true;
        Context.sourceSlot = source;
        Context.payload = item;
        Context.startPanelPosition = panelPos;
        CreateDragGhostForItem(item);
        PositionGhost(panelPos);
    }

    public void UpdateDrag(Vector2 panelPos)
    {
        if (!Context.isDragging || dragGhost == null) return;
        PositionGhost(panelPos);
    }

    public void EndDrag()
    {
        Context.isDragging = false;
        Context.sourceSlot = null;
        Context.payload = null;
        if (dragGhost != null)
        {
            dragGhost.RemoveFromHierarchy();
            dragGhost = null;
        }
    }

    public void TryDropOn(SlotModel target)
    {
        if (!Context.isDragging || Context.payload == null || target == null) return;
        var inv = getInventory();
        var eqp = getEquipment();

        switch (Context.sourceSlot.slotType)
        {
            case SlotType.Inventory:
                if (target.slotType == SlotType.Inventory)
                {
                    if (target.inventoryIndex != Context.sourceSlot.inventoryIndex)
                    {
                        inv.SwapItems(Context.sourceSlot.inventoryIndex, target.inventoryIndex);
                        refreshInventoryUI();
                    }
                }
                else if (target.slotType == SlotType.Equipment)
                {
                    // Equipment transfers are now handled by CombatPanelUI
                    // This method is kept for inventory-only operations
                    break;
                }
                // Trash handled by caller
                break;

            case SlotType.Equipment:
                if (target.slotType == SlotType.Inventory)
                {
                    // Place into inventory; if occupied and compatible, swap back to equipment
                    var existing = inv.GetItem(target.inventoryIndex);
                    var fromSlot = Context.sourceSlot.equipmentSlot;
                    if (existing == null)
                    {
                        eqp.Unequip(fromSlot);
                        inv.SetItemAt(target.inventoryIndex, Context.payload);
                    }
                    else if (existing is EquippableItem existingEq && existingEq.slot == fromSlot)
                    {
                        eqp.Unequip(fromSlot);
                        inv.SetItemAt(target.inventoryIndex, Context.payload);
                        eqp.TryEquipToSlot(existingEq, fromSlot, out _);
                    }
                    refreshInventoryUI();
                    refreshEquipmentUI();
                }
                else if (target.slotType == SlotType.Equipment)
                {
                    var fromSlot = Context.sourceSlot.equipmentSlot;
                    if (Context.payload is EquippableItem eq && eq.slot == target.equipmentSlot)
                    {
                        // Check weapon compatibility before swapping
                        if (!eqp.IsSlotCompatible(target.equipmentSlot, eq))
                        {
                            // Incompatible weapons - return item to source slot
                            if (eq is EquippableItem re)
                            {
                                eqp.TryEquipToSlot(re, fromSlot, out _);
                                refreshEquipmentUI();
                            }
                            return;
                        }
                        
                        // Swap equipment
                        eqp.Unequip(fromSlot);
                        var prev = eqp.Unequip(target.equipmentSlot);
                        eqp.TryEquipToSlot(eq, target.equipmentSlot, out _);
                        if (prev is EquippableItem prevEq && prevEq.slot == fromSlot)
                        {
                            eqp.TryEquipToSlot(prevEq, fromSlot, out _);
                        }
                        else if (prev != null)
                        {
                            int empty = inv.FindFirstEmptySlot();
                            if (empty >= 0) inv.SetItemAt(empty, prev);
                        }
                        refreshEquipmentUI();
                        refreshInventoryUI();
                    }
                    else
                    {
                        // Invalid drop: re-equip
                        if (Context.payload is EquippableItem re)
                        {
                            eqp.TryEquipToSlot(re, fromSlot, out _);
                            refreshEquipmentUI();
                        }
                    }
                }
                break;
        }
    }

    private void CreateDragGhostForItem(InventoryItem item)
    {
        dragGhost = new VisualElement();
        dragGhost.AddToClassList("slot-icon");
        dragGhost.AddToClassList("drag-ghost");
        dragGhost.pickingMode = PickingMode.Ignore;
        if (item.icon != null)
        {
            dragGhost.style.backgroundImage = new StyleBackground(item.icon);
        }
        else
        {
            var label = new Label(item.itemName.Substring(0, Mathf.Min(2, item.itemName.Length)));
            label.style.fontSize = 10;
            label.style.color = Color.white;
            dragGhost.Add(label);
        }
        uiRoot.Add(dragGhost);
    }

    private void PositionGhost(Vector2 panelPos)
    {
        dragGhost.style.position = Position.Absolute;
        dragGhost.style.left = panelPos.x + 10f;
        dragGhost.style.top = panelPos.y + 10f;
    }
}


