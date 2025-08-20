using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine.UIElements;

public class CombatPanelUI : MonoBehaviour
{
    public VisualTreeAsset CombatMenuTemplate;
    public StyleSheet CombatMenuStyles;
    public StyleSheet ThemeStyles;

    private Button endTurnButton;
    private Button toggleInventoryButton;
    private VisualElement inventoryPanel;
    private VisualElement itemGrid;
    private VisualElement leftHandSlot;
    private VisualElement rightHandSlot;
    private VisualElement armorSlot;
    private VisualElement trashSlot;
    private List<VisualElement> inventorySlots = new List<VisualElement>();
    private CharacterSheet currentCharacter;

    // UI root reference (the cloned VisualTree) for overlays like drag ghost
    private VisualElement uiRoot;
    private bool globalDragBound = false;

    // Drag-and-drop (will be moved to DragDropController progressively)
    private DragDropController drag;
    
    // Lightweight UI Toolkit tooltip
    private VisualElement hoverTooltip;
    private Label hoverTooltipLabel;

    private void Start()
    {
        // Load the UXML template and style sheets
        if (CombatMenuTemplate == null)
        {
            Debug.LogError("CombatPanelUI: CombatMenuTemplate is not assigned in the Unity Editor.");
            return;
        }

        VisualElement visualTree = CombatMenuTemplate.CloneTree();
        // Make sure the root doesn't block clicks (full-screen transparent container)
        visualTree.style.position = Position.Absolute;
        visualTree.style.left = 0;
        visualTree.style.right = 0;
        visualTree.style.top = 0;
        visualTree.style.bottom = 0;
        visualTree.pickingMode = PickingMode.Ignore;

        if (ThemeStyles != null)
        {
            visualTree.styleSheets.Add(ThemeStyles);
        }
        if (CombatMenuStyles != null)
        {
            visualTree.styleSheets.Add(CombatMenuStyles);
        }

        // Keep a reference to the UI root for overlays
        uiRoot = visualTree;

        // Get references to the UI elements
        endTurnButton = visualTree.Q<Button>("EndTurnButton");
        toggleInventoryButton = visualTree.Q<Button>("ToggleInventoryButton");
        inventoryPanel = visualTree.Q<VisualElement>("InventoryPanel");
        itemGrid = visualTree.Q<VisualElement>("ItemGrid");
        leftHandSlot = visualTree.Q<VisualElement>("LeftHandSlot");
        rightHandSlot = visualTree.Q<VisualElement>("RightHandSlot");
        armorSlot = visualTree.Q<VisualElement>("ArmorSlot");
        trashSlot = visualTree.Q<VisualElement>("Trash");

        // Ensure HUD bar is visible and blocks clicks (including all buttons and children)
        var hudBar = visualTree.Q<VisualElement>("CombatHudBar");
        if (hudBar != null)
        {
            hudBar.style.display = DisplayStyle.Flex;
            hudBar.style.visibility = Visibility.Visible;
            UIClickBlocker.MakeElementAndChildrenBlockClicks(hudBar);
        }

        // Ensure inventory panel and all its children block clicks
        if (inventoryPanel != null)
        {
            UIClickBlocker.MakeElementAndChildrenBlockClicks(inventoryPanel);
        }

        // Create lightweight tooltip element once and add to root
        CreateHoverTooltip(visualTree);



        // Add event listeners to the buttons
        if (endTurnButton != null)
        {
        endTurnButton.clicked += OnEndTurnButtonClicked;
        }
        if (toggleInventoryButton != null)
        {
            toggleInventoryButton.clicked += OnToggleInventoryClicked;
        }

        // Initial state for the inventory panel
        if (inventoryPanel != null)
        {
            inventoryPanel.style.display = DisplayStyle.None;
        }

        // Initialize drag/drop controller
        drag = new DragDropController(
            uiRoot,
            () => currentCharacter?.inventory,
            () => new EquipmentProxy(currentCharacter),
            RefreshInventoryUI,
            RefreshEquipmentUI
        );

        // Create inventory slots
        CreateInventorySlots();
        // Register trash drop once
        if (trashSlot != null)
        {
            trashSlot.RegisterCallback<PointerUpEvent>(OnTrashPointerUp);
        }

        // Add the UI elements to the root visual element
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null)
        {
            uiDoc = FindObjectOfType<UIDocument>();
        }

        if (uiDoc == null)
        {
            Debug.LogError("CombatPanelUI: No UIDocument found. Ensure a UIDocument exists in the scene.");
            return;
        }

        VisualElement root = uiDoc.rootVisualElement;
        if (root != null)
        {
            root.Add(visualTree);
        }
        else
        {
            Debug.LogError("CombatPanelUI: Root visual element is null on found UIDocument.");
        }
    }

    private TurnManager turnManager;

    private void Awake()
    {
        turnManager = FindObjectOfType<TurnManager>();
    }

    private void Update()
    {
        bool playerActive = (turnManager == null) || turnManager.IsPlayerTurn();

        if (toggleInventoryButton != null)
        {
            toggleInventoryButton.style.display = playerActive ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // If enemy is active and panel is open, close it
        if (turnManager != null && !playerActive && inventoryPanel != null && inventoryPanel.style.display == DisplayStyle.Flex)
        {
            inventoryPanel.style.display = DisplayStyle.None;
        }

        // Update current character and refresh inventory if needed
        UpdateCurrentCharacter();
    }

    private void CreateInventorySlots()
    {
        if (itemGrid == null) return;

        // Create 24 inventory slots (6x4 grid)
        for (int i = 0; i < Inventory.MaxSlots; i++)
        {
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");
            slot.AddToClassList("ui-blocker"); // Ensure each slot blocks clicks
            slot.name = $"InventorySlot_{i}";
            AttachTooltipHandlers(slot);
            AttachDragHandlers(slot, i);
            
            itemGrid.Add(slot);
            inventorySlots.Add(slot);
        }

        // Global listeners to track drag motion and drop (bind once)
        if (uiRoot != null && !globalDragBound)
        {
            uiRoot.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
            uiRoot.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
            globalDragBound = true;
        }
    }

    private void UpdateCurrentCharacter()
    {
        CharacterSheet newCharacter = GetCurrentPlayerCharacter();
        if (newCharacter != currentCharacter)
        {
            // Unsubscribe from old character
            if (currentCharacter != null)
            {
                currentCharacter.OnInventoryChanged -= RefreshInventoryUI;
                currentCharacter.OnEquipmentChanged -= RefreshEquipmentUI;
            }

            currentCharacter = newCharacter;

            // Subscribe to new character
            if (currentCharacter != null)
            {
                currentCharacter.OnInventoryChanged += RefreshInventoryUI;
                currentCharacter.OnEquipmentChanged += RefreshEquipmentUI;
                RefreshInventoryUI();
                RefreshEquipmentUI();
            }
        }
    }

    private CharacterSheet GetCurrentPlayerCharacter()
    {
        if (turnManager == null || !turnManager.IsPlayerTurn()) return null;

        var players = FindObjectsOfType<PlayerController>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].isTurn)
            {
                return players[i].characterSheet;
            }
        }
        return null;
    }

    private void RefreshInventoryUI()
    {
        if (currentCharacter?.inventory == null || inventorySlots.Count == 0) return;

        var items = currentCharacter.inventory.Items;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            var slot = inventorySlots[i];
            slot.Clear();
            // Clear any previous tooltip
            slot.tooltip = null;

            if (i < items.Count && items[i] != null)
            {
                var item = items[i];
                
                // Add icon if available
                if (item.icon != null)
                {
                    var icon = new VisualElement();
                    icon.AddToClassList("slot-icon");
                    icon.style.backgroundImage = new StyleBackground(item.icon);
                    // Mirror tooltip on child so hover over child still shows
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

                // Add stack count if > 1
                if (item.currentStack > 1)
                {
                    var stackLabel = new Label(item.currentStack.ToString());
                    stackLabel.AddToClassList("stack-count");
                    stackLabel.tooltip = item.GetDisplayName();
                    slot.Add(stackLabel);
                }

                // Set tooltip on slot itself
                slot.tooltip = item.GetDisplayName();
            }
        }
        
        // Re-apply click blocking to all inventory slots and their new children
        UIClickBlocker.MakeElementAndChildrenBlockClicks(itemGrid);
    }

    private void RefreshEquipmentUI()
    {
        if (currentCharacter == null) return;

        RefreshEquipmentSlot(leftHandSlot, EquippableItem.EquipmentSlot.LeftHand);
        RefreshEquipmentSlot(rightHandSlot, EquippableItem.EquipmentSlot.RightHand);
        RefreshEquipmentSlot(armorSlot, EquippableItem.EquipmentSlot.Armor);
        AttachEquipmentDragTargets();
    }

    private void RefreshEquipmentSlot(VisualElement slot, EquippableItem.EquipmentSlot equipmentSlot)
    {
        if (slot == null) return;

        slot.Clear();
        // Ensure tooltip handlers are attached once
        AttachTooltipHandlers(slot);
        var equippedItem = currentCharacter.GetEquippedItem(equipmentSlot);
        
        // Clear any previous tooltip
        slot.tooltip = null;

        if (equippedItem != null)
        {
            if (equippedItem.icon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("slot-icon");
                icon.style.backgroundImage = new StyleBackground(equippedItem.icon);
                icon.tooltip = equippedItem.GetDisplayName();
                slot.Add(icon);
            }
            else
            {
                var label = new Label(equippedItem.itemName.Substring(0, Mathf.Min(2, equippedItem.itemName.Length)));
                label.style.fontSize = 10;
                label.style.color = Color.white;
                label.tooltip = equippedItem.GetDisplayName();
                slot.Add(label);
            }

            // Tooltip on the slot itself
            slot.tooltip = equippedItem.GetDisplayName();
        }
        
        // Re-apply click blocking to this slot and its children
        UIClickBlocker.MakeElementAndChildrenBlockClicks(slot);
    }

    private void AttachEquipmentDragTargets()
    {
        ForEachEquipSlot((slot, es) => AttachEquipmentDrop(slot, es));
        // Begin drag when pressing on equipped slots
        ForEachEquipSlot((slot, es) => AttachEquipmentDrag(slot, es));
        // Global hover feedback is applied during drag via UpdateEquipHoverFeedbackGlobal
        if (trashSlot != null)
        {
            trashSlot.RegisterCallback<PointerUpEvent>(OnTrashPointerUp);
        }
    }

    private void ForEachEquipSlot(System.Action<VisualElement, EquippableItem.EquipmentSlot> action)
    {
        if (leftHandSlot != null) action(leftHandSlot, EquippableItem.EquipmentSlot.LeftHand);
        if (rightHandSlot != null) action(rightHandSlot, EquippableItem.EquipmentSlot.RightHand);
        if (armorSlot != null) action(armorSlot, EquippableItem.EquipmentSlot.Armor);
    }

    private void ClearEquipSlotFeedback(VisualElement slot)
    {
        slot.RemoveFromClassList("equip-drop-valid");
        slot.RemoveFromClassList("equip-drop-invalid");
        slot.RemoveFromClassList("equip-drop-neutral");
    }

    private void SetAllEquipNeutral()
    {
        if (leftHandSlot != null) { ClearEquipSlotFeedback(leftHandSlot); leftHandSlot.AddToClassList("equip-drop-neutral"); }
        if (rightHandSlot != null) { ClearEquipSlotFeedback(rightHandSlot); rightHandSlot.AddToClassList("equip-drop-neutral"); }
        if (armorSlot != null) { ClearEquipSlotFeedback(armorSlot); armorSlot.AddToClassList("equip-drop-neutral"); }
    }

    private void ClearAllHoverFeedback()
    {
        // Clear equipment slot feedback
        if (leftHandSlot != null) ClearEquipSlotFeedback(leftHandSlot);
        if (rightHandSlot != null) ClearEquipSlotFeedback(rightHandSlot);
        if (armorSlot != null) ClearEquipSlotFeedback(armorSlot);
        // Clear any lingering classes on inventory slots (in case they were applied)
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            var s = inventorySlots[i];
            if (s == null) continue;
            s.RemoveFromClassList("equip-drop-valid");
            s.RemoveFromClassList("equip-drop-invalid");
            s.RemoveFromClassList("equip-drop-neutral");
        }
    }

    private void UpdateEquipHoverFeedbackGlobal(Vector2 panelPosition)
    {
        // Clear all first
        if (leftHandSlot != null) ClearEquipSlotFeedback(leftHandSlot);
        if (rightHandSlot != null) ClearEquipSlotFeedback(rightHandSlot);
        if (armorSlot != null) ClearEquipSlotFeedback(armorSlot);

        if (!drag.Context.isDragging) { SetAllEquipNeutral(); return; }

        // Identify equipment slot under cursor
        var ve = uiRoot?.panel?.Pick(panelPosition) as VisualElement;
        while (ve != null)
        {
            if (ve == leftHandSlot || ve == rightHandSlot || ve == armorSlot)
            {
                bool isValid = false;
                if (drag.Context.payload is EquippableItem eq)
                {
                    if ((ve == leftHandSlot && eq.slot == EquippableItem.EquipmentSlot.LeftHand) ||
                        (ve == rightHandSlot && eq.slot == EquippableItem.EquipmentSlot.RightHand) ||
                        (ve == armorSlot && eq.slot == EquippableItem.EquipmentSlot.Armor))
                    {
                        isValid = true;
                    }
                }
                ve.AddToClassList(isValid ? "equip-drop-valid" : "equip-drop-invalid");
                break;
            }
            ve = ve.parent;
        }
    }

    private void AttachEquipmentDrag(VisualElement slot, EquippableItem.EquipmentSlot equipmentSlot)
    {
        if (slot == null) return;
        slot.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0) return;
            var equipped = currentCharacter.GetEquippedItem(equipmentSlot);
            if (equipped == null) return;
            // Begin drag via controller
            drag.BeginDrag(SlotModel.CreateEquipment(slot, equipmentSlot), equipped, evt.position);
            UpdateEquipHoverFeedbackGlobal(evt.position);
        });
    }

    private void AttachEquipmentDrop(VisualElement slot, EquippableItem.EquipmentSlot equipmentSlot)
    {
        if (slot == null) return;
        slot.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (!drag.Context.isDragging || drag.Context.payload == null) return;
            if (!(drag.Context.payload is EquippableItem eq)) { drag.EndDrag(); return; }

            if (eq.slot == equipmentSlot)
            {
                // From inventory -> equipment
                if (drag.Context.sourceSlot.slotType == SlotType.Inventory)
                {
                    int srcIdx = drag.Context.sourceSlot.inventoryIndex;
                    if (srcIdx >= 0) currentCharacter.inventory.SetItemAt(srcIdx, null);
                    var prev = currentCharacter.UnequipItem(equipmentSlot);
                    currentCharacter.TryEquipItem(eq);
                    if (prev != null)
                    {
                        int target = srcIdx >= 0 ? srcIdx : currentCharacter.inventory.FindFirstEmptySlot();
                        if (target >= 0) currentCharacter.inventory.SetItemAt(target, prev);
                    }
                }
                else // equipment -> equipment
                {
                    var prev = currentCharacter.UnequipItem(equipmentSlot);
                    currentCharacter.TryEquipItem(eq);
                    if (prev is EquippableItem prevEq && prevEq.slot == drag.Context.sourceSlot.equipmentSlot)
                    {
                        currentCharacter.TryEquipItem(prevEq);
                    }
                    else if (prev != null)
                    {
                        int empty = currentCharacter.inventory.FindFirstEmptySlot();
                        if (empty >= 0) currentCharacter.inventory.SetItemAt(empty, prev);
                    }
                }
                RefreshInventoryUI();
                RefreshEquipmentUI();
            }
            else
            {
                // Invalid drop: revert
                if (drag.Context.sourceSlot.slotType == SlotType.Equipment)
                {
                    currentCharacter.TryEquipItem(eq);
                    RefreshEquipmentUI();
                }
                // else: inventory origin stays where it was
            }
            ClearAllHoverFeedback();
            drag.EndDrag();
        });
    }

    private void OnTrashPointerUp(PointerUpEvent evt)
    {
        if (!drag.Context.isDragging || drag.Context.payload == null) return;
        // Confirm discard
        bool confirmed = ConfirmDiscard(drag.Context.payload);
        if (confirmed && drag.Context.sourceSlot != null && drag.Context.sourceSlot.slotType == SlotType.Inventory)
        {
            currentCharacter.inventory.SetItemAt(drag.Context.sourceSlot.inventoryIndex, null);
            RefreshInventoryUI();
        }
        ClearAllHoverFeedback();
        drag.EndDrag();
    }

    private bool ConfirmDiscard(InventoryItem item)
    {
        // Simple confirmation for now; can be replaced with real modal later
        Debug.Log($"Discarding item: {item.GetDisplayName()}");
        return true; // TODO: replace with modal popup
    }

    // -------------------- Drag & Drop --------------------
    private void AttachDragHandlers(VisualElement slot, int index)
    {
        if (slot == null) return;
        if (slot.userData is int) return; // already attached
        slot.userData = index;

        slot.RegisterCallback<PointerDownEvent>(evt =>
        {
            // Only start drag on left button
            if (evt.button != 0) return;
            if (currentCharacter?.inventory == null) return;
            var item = currentCharacter.inventory.Items[index];
            if (item == null) return;
            drag.BeginDrag(SlotModel.CreateInventory(slot, index), item, evt.position);
            UpdateEquipHoverFeedbackGlobal(evt.position);
        });
    }

    private void TryBeginDrag(int sourceIndex, Vector2 panelPos)
    {
        if (currentCharacter?.inventory == null) return;
        if (sourceIndex < 0 || sourceIndex >= currentCharacter.inventory.Items.Count) return;
        var item = currentCharacter.inventory.Items[sourceIndex];
        if (item == null) return;
        var slotEl = (sourceIndex >= 0 && sourceIndex < inventorySlots.Count) ? inventorySlots[sourceIndex] : null;
        drag.BeginDrag(SlotModel.CreateInventory(slotEl, sourceIndex), item, panelPos);
        UpdateEquipHoverFeedbackGlobal(panelPos);
    }

    private void OnGlobalPointerMove(PointerMoveEvent evt)
    {
        if (!drag.Context.isDragging) return;
        drag.UpdateDrag(evt.position);
        UpdateEquipHoverFeedbackGlobal(evt.position);
    }

    private void OnGlobalPointerUp(PointerUpEvent evt)
    {
        if (!drag.Context.isDragging) return;

        // Find destination slot by walking up from event target
        int destIndex = GetSlotIndexFromEventTarget(evt.target);
        SlotModel target = null;
        if (destIndex >= 0)
        {
            var slotEl = (destIndex >= 0 && destIndex < inventorySlots.Count) ? inventorySlots[destIndex] : null;
            target = SlotModel.CreateInventory(slotEl, destIndex);
        }
        else
        {
            var ve = evt.target as VisualElement;
            while (ve != null)
            {
                if (ve == leftHandSlot) { target = SlotModel.CreateEquipment(leftHandSlot, EquippableItem.EquipmentSlot.LeftHand); break; }
                if (ve == rightHandSlot) { target = SlotModel.CreateEquipment(rightHandSlot, EquippableItem.EquipmentSlot.RightHand); break; }
                if (ve == armorSlot) { target = SlotModel.CreateEquipment(armorSlot, EquippableItem.EquipmentSlot.Armor); break; }
                if (ve == trashSlot) { target = SlotModel.CreateTrash(trashSlot); break; }
                ve = ve.parent;
            }
        }
        if (target != null)
        {
            if (target.slotType == SlotType.Trash)
            {
                bool confirmed = ConfirmDiscard(drag.Context.payload);
                if (confirmed)
                {
                    var src = drag.Context.sourceSlot;
                    if (src != null && src.slotType == SlotType.Inventory)
                    {
                        currentCharacter.inventory.SetItemAt(src.inventoryIndex, null);
                        RefreshInventoryUI();
                    }
                    else if (src != null && src.slotType == SlotType.Equipment)
                    {
                        currentCharacter.UnequipItem(src.equipmentSlot);
                        RefreshEquipmentUI();
                    }
                }
            }
            else
            {
                drag.TryDropOn(target);
            }
        }
        // Clear any hover feedback after drop attempt
        ClearAllHoverFeedback();
        drag.EndDrag();
    }

    private int GetSlotIndexFromEventTarget(IEventHandler target)
    {
        var ve = target as VisualElement;
        while (ve != null)
        {
            // Check name pattern first
            if (ve.name != null && ve.name.StartsWith("InventorySlot_"))
            {
                if (int.TryParse(ve.name.Substring("InventorySlot_".Length), out int idx))
                {
                    return idx;
                }
            }
            // Or check if this exact element is in our list
            int listIndex = inventorySlots.IndexOf(ve);
            if (listIndex >= 0) return listIndex;

            ve = ve.parent;
        }
        return -1;
    }

    // Drag helpers now live in DragDropController

    private void CreateHoverTooltip(VisualElement root)
    {
        hoverTooltip = new VisualElement();
        hoverTooltip.AddToClassList("ui-tooltip");
        hoverTooltip.style.display = DisplayStyle.None;

        hoverTooltipLabel = new Label();
        hoverTooltipLabel.AddToClassList("ui-tooltip__label");
        hoverTooltip.Add(hoverTooltipLabel);

        root.Add(hoverTooltip);
    }

    private void AttachTooltipHandlers(VisualElement element)
    {
        if (element == null) return;
        if (element.ClassListContains("tooltip-bound")) return;
        element.AddToClassList("tooltip-bound");

        element.RegisterCallback<PointerEnterEvent>(evt =>
        {
            if (string.IsNullOrEmpty(element.tooltip)) return;
            hoverTooltipLabel.text = element.tooltip;
            hoverTooltip.style.display = DisplayStyle.Flex;
            PositionTooltip(evt.position);
        });

        element.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (hoverTooltip.style.display == DisplayStyle.Flex)
            {
                PositionTooltip(evt.position);
            }
        });

        element.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            hoverTooltip.style.display = DisplayStyle.None;
        });
    }

    private void PositionTooltip(Vector2 panelPosition)
    {
        // Offset tooltip a bit from the cursor
        float offsetX = 12f;
        float offsetY = 12f;
        hoverTooltip.style.left = panelPosition.x + offsetX;
        hoverTooltip.style.top = panelPosition.y + offsetY;
    }

    private void OnEndTurnButtonClicked()
    {
        PlayerController[] pcs = FindObjectsOfType<PlayerController>();
        foreach (PlayerController pc in pcs)
        {
            // The controller is responsible for only ending turn if the click is valid.
            pc.EndTurnButtonClick();
        }
    }

    private void OnToggleInventoryClicked()
    {
        if (inventoryPanel == null)
        {
            Debug.LogWarning("InventoryPanel not found in UI tree.");
            return;
        }
        bool isOpen = inventoryPanel.style.display == DisplayStyle.Flex;
        inventoryPanel.style.display = isOpen ? DisplayStyle.None : DisplayStyle.Flex;
        if (!isOpen)
        {
            inventoryPanel.BringToFront();
        }

    }
}
