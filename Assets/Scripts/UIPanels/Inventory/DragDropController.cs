using UnityEngine;
using UnityEngine.UIElements;

public class DragDropController
{
    private readonly VisualElement uiRoot;
    private readonly System.Func<Inventory> getInventory;
    private readonly System.Func<Equipment> getEquipment;
    private readonly System.Action refreshInventoryUI;
    private readonly System.Action refreshEquipmentUI;
    private readonly System.Func<Inventory> getStashInventory;
    private readonly System.Action refreshStashInventoryUI;

    private VisualElement dragGhost;
    public DragContext Context { get; } = new DragContext();

    public DragDropController(
        VisualElement uiRoot,
        System.Func<Inventory> getInventory,
        System.Func<Equipment> getEquipment,
        System.Action refreshInventoryUI,
        System.Action refreshEquipmentUI,
        System.Func<Inventory> getStashInventory = null,
        System.Action refreshStashInventoryUI = null)
    {
        this.uiRoot = uiRoot;
        this.getInventory = getInventory;
        this.getEquipment = getEquipment;
        this.refreshInventoryUI = refreshInventoryUI;
        this.refreshEquipmentUI = refreshEquipmentUI;
        this.getStashInventory = getStashInventory;
        this.refreshStashInventoryUI = refreshStashInventoryUI;
    }

    public void BeginDrag(SlotModel source, InventoryItem item, Vector2 panelPos)
    {
        if (item == null) return;
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
        var stash = getStashInventory != null ? getStashInventory() : null;

        switch (Context.sourceSlot.slotType)
        {
            case SlotType.Inventory:
                TryDropFromCharacterInventory(target, inv, stash, eqp);
                break;
            case SlotType.Stash:
                if (stash != null)
                    TryDropFromStash(target, inv, stash, eqp);
                break;
            case SlotType.Equipment:
                TryDropFromEquipment(target, inv, stash, eqp);
                break;
        }
    }

    private void TryDropFromCharacterInventory(SlotModel target, Inventory inv, Inventory stash, Equipment eqp)
    {
        if (inv == null) return;

        if (target.slotType == SlotType.Inventory)
        {
            if (target.inventoryIndex != Context.sourceSlot.inventoryIndex)
            {
                inv.SwapItems(Context.sourceSlot.inventoryIndex, target.inventoryIndex);
                refreshInventoryUI?.Invoke();
            }
        }
        else if (target.slotType == SlotType.Stash && stash != null)
        {
            SwapAcrossInventories(inv, Context.sourceSlot.inventoryIndex, stash, target.inventoryIndex);
            refreshInventoryUI?.Invoke();
            refreshStashInventoryUI?.Invoke();
        }
        else if (target.slotType == SlotType.Equipment && Context.payload is EquippableItem eq && eqp != null &&
            eqp.IsSlotCompatible(target.equipmentSlot, eq))
        {
            inv.SetItemAt(Context.sourceSlot.inventoryIndex, null);
            if (eqp.TryEquipToSlot(eq, target.equipmentSlot, out var prev))
            {
                int idx = Context.sourceSlot.inventoryIndex >= 0 ? Context.sourceSlot.inventoryIndex : inv.FindFirstEmptySlot();
                if (prev != null && idx >= 0) inv.SetItemAt(idx, prev);
            }
            refreshInventoryUI?.Invoke();
            refreshEquipmentUI?.Invoke();
        }
    }

    private void TryDropFromStash(SlotModel target, Inventory inv, Inventory stash, Equipment eqp)
    {
        int srcIdx = Context.sourceSlot.inventoryIndex;

        if (target.slotType == SlotType.Stash)
        {
            if (target.inventoryIndex != srcIdx)
            {
                stash.SwapItems(srcIdx, target.inventoryIndex);
                refreshStashInventoryUI?.Invoke();
            }
        }
        else if (target.slotType == SlotType.Inventory && inv != null)
        {
            SwapAcrossInventories(stash, srcIdx, inv, target.inventoryIndex);
            refreshInventoryUI?.Invoke();
            refreshStashInventoryUI?.Invoke();
        }
        else if (target.slotType == SlotType.Equipment && Context.payload is EquippableItem eq && eqp != null &&
            eqp.IsSlotCompatible(target.equipmentSlot, eq) && inv != null)
        {
            stash.SetItemAt(srcIdx, null);
            if (eqp.TryEquipToSlot(eq, target.equipmentSlot, out var prev))
            {
                int idx = srcIdx >= 0 ? srcIdx : stash.FindFirstEmptySlot();
                if (prev != null && idx >= 0) stash.SetItemAt(idx, prev);
            }
            refreshStashInventoryUI?.Invoke();
            refreshEquipmentUI?.Invoke();
        }
    }

    private void TryDropFromEquipment(SlotModel target, Inventory inv, Inventory stash, Equipment eqp)
    {
        if (inv == null) return;

        if (target.slotType == SlotType.Inventory)
        {
            var existing = inv.GetItem(target.inventoryIndex);
            var fromSlot = Context.sourceSlot.equipmentSlot;
            if (existing == null)
            {
                eqp.Unequip(fromSlot);
                inv.SetItemAt(target.inventoryIndex, Context.payload);
            }
            else if (existing is EquippableItem existingEq && eqp.IsSlotCompatible(fromSlot, existingEq))
            {
                eqp.Unequip(fromSlot);
                inv.SetItemAt(target.inventoryIndex, Context.payload);
                eqp.TryEquipToSlot(existingEq, fromSlot, out _);
            }
            refreshInventoryUI?.Invoke();
            refreshEquipmentUI?.Invoke();
        }
        else if (target.slotType == SlotType.Stash && stash != null)
        {
            var existing = stash.GetItem(target.inventoryIndex);
            var fromSlot = Context.sourceSlot.equipmentSlot;
            if (existing == null)
            {
                eqp.Unequip(fromSlot);
                stash.SetItemAt(target.inventoryIndex, Context.payload);
            }
            else if (existing is EquippableItem existingEq && eqp.IsSlotCompatible(fromSlot, existingEq))
            {
                eqp.Unequip(fromSlot);
                stash.SetItemAt(target.inventoryIndex, Context.payload);
                eqp.TryEquipToSlot(existingEq, fromSlot, out _);
            }
            refreshStashInventoryUI?.Invoke();
            refreshEquipmentUI?.Invoke();
        }
        else if (target.slotType == SlotType.Equipment)
        {
            var fromSlot = Context.sourceSlot.equipmentSlot;
            if (Context.payload is EquippableItem eq && eqp.IsSlotCompatible(target.equipmentSlot, eq))
            {
                eqp.Unequip(fromSlot);
                var prev = eqp.Unequip(target.equipmentSlot);
                eqp.TryEquipToSlot(eq, target.equipmentSlot, out _);
                if (prev is EquippableItem prevEq && eqp.IsSlotCompatible(fromSlot, prevEq))
                {
                    eqp.TryEquipToSlot(prevEq, fromSlot, out _);
                }
                else if (prev != null)
                {
                    int empty = inv.FindFirstEmptySlot();
                    if (empty >= 0) inv.SetItemAt(empty, prev);
                }
                refreshEquipmentUI?.Invoke();
                refreshInventoryUI?.Invoke();
            }
            else
            {
                if (Context.payload is EquippableItem re)
                {
                    eqp.TryEquipToSlot(re, fromSlot, out _);
                    refreshEquipmentUI?.Invoke();
                }
            }
        }
    }

    private static void SwapAcrossInventories(Inventory a, int idxA, Inventory b, int idxB)
    {
        if (a == null || b == null) return;
        var itemA = a.GetItem(idxA);
        var itemB = b.GetItem(idxB);
        a.SetItemAt(idxA, itemB);
        b.SetItemAt(idxB, itemA);
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
