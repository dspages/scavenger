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
    private VisualElement trashSlot;
    private CharacterSheet currentCharacter;
    private InventoryUIManager inventoryUI;

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
        var inventoryPanel = visualTree.Q<VisualElement>("InventoryPanel");
        var itemGrid = visualTree.Q<VisualElement>("ItemGrid");
        var leftHandSlot = visualTree.Q<VisualElement>("LeftHandSlot");
        var rightHandSlot = visualTree.Q<VisualElement>("RightHandSlot");
        var armorSlot = visualTree.Q<VisualElement>("ArmorSlot");
        trashSlot = visualTree.Q<VisualElement>("Trash");

        // Initialize inventory UI manager
        inventoryUI = new InventoryUIManager(inventoryPanel, itemGrid, leftHandSlot, rightHandSlot, armorSlot);

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
        inventoryUI.SetInventoryPanelVisible(false);

        // Initialize drag/drop controller
        drag = new DragDropController(
            uiRoot,
            () => currentCharacter?.inventory,
            () => new EquipmentProxy(currentCharacter),
            () => inventoryUI.RefreshInventoryUI(),
            () => inventoryUI.RefreshEquipmentUI(AttachTooltipHandlers)
        );

        // Create inventory slots
        inventoryUI.CreateInventorySlots((slot, index) => {
            AttachTooltipHandlers(slot);
            AttachDragHandlers(slot, index);
        });
        // Register trash drop once
        if (trashSlot != null)
        {
            trashSlot.RegisterCallback<PointerUpEvent>(OnTrashPointerUp);
        }

        // Global listeners to track drag motion and drop (bind once)
        if (uiRoot != null && !globalDragBound)
        {
            uiRoot.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
            uiRoot.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
            globalDragBound = true;
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
        if (turnManager != null && !playerActive && inventoryUI.IsInventoryPanelOpen())
        {
            inventoryUI.SetInventoryPanelVisible(false);
        }

        // Update current character and refresh inventory if needed
        UpdateCurrentCharacter();
    }

    private void UpdateCurrentCharacter()
    {
        CharacterSheet newCharacter = GetCurrentPlayerCharacter();
        if (newCharacter != currentCharacter)
        {
            // Unsubscribe from old character
            if (currentCharacter != null)
            {
                currentCharacter.OnInventoryChanged -= inventoryUI.RefreshInventoryUI;
                currentCharacter.OnEquipmentChanged -= () => inventoryUI.RefreshEquipmentUI(AttachTooltipHandlers);
            }

            currentCharacter = newCharacter;
            inventoryUI.SetCurrentCharacter(currentCharacter);

            // Subscribe to new character
            if (currentCharacter != null)
            {
                currentCharacter.OnInventoryChanged += inventoryUI.RefreshInventoryUI;
                currentCharacter.OnEquipmentChanged += () => inventoryUI.RefreshEquipmentUI(AttachTooltipHandlers);
                inventoryUI.RefreshInventoryUI();
                inventoryUI.RefreshEquipmentUI(AttachTooltipHandlers);
                AttachEquipmentDragTargets();
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



    private void AttachEquipmentDragTargets()
    {
        inventoryUI.ForEachEquipSlot((slot, es) => AttachEquipmentDrop(slot, es));
        // Begin drag when pressing on equipped slots
        inventoryUI.ForEachEquipSlot((slot, es) => AttachEquipmentDrag(slot, es));
        // Global hover feedback is applied during drag via UpdateEquipHoverFeedbackGlobal
        // Note: Trash registration happens once in Start() method
    }



    private void ClearEquipSlotFeedback(VisualElement slot)
    {
        slot.RemoveFromClassList("equip-drop-valid");
        slot.RemoveFromClassList("equip-drop-invalid");
        slot.RemoveFromClassList("equip-drop-neutral");
    }

    private void SetAllEquipNeutral()
    {
        inventoryUI.ForEachEquipSlot((slot, es) => {
            ClearEquipSlotFeedback(slot);
            slot.AddToClassList("equip-drop-neutral");
        });
    }

    private void ClearAllHoverFeedback()
    {
        // Clear equipment slot feedback
        inventoryUI.ForEachEquipSlot((slot, es) => ClearEquipSlotFeedback(slot));
        
        // Clear any lingering classes on inventory slots (in case they were applied)
        var inventorySlots = inventoryUI.GetInventorySlots();
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
        inventoryUI.ForEachEquipSlot((slot, es) => ClearEquipSlotFeedback(slot));

        if (!drag.Context.isDragging) { SetAllEquipNeutral(); return; }

        // Identify equipment slot under cursor
        var ve = uiRoot?.panel?.Pick(panelPosition) as VisualElement;
        while (ve != null)
        {
            var leftHandSlot = inventoryUI.GetEquipmentSlot(EquippableItem.EquipmentSlot.LeftHand);
            var rightHandSlot = inventoryUI.GetEquipmentSlot(EquippableItem.EquipmentSlot.RightHand);
            var armorSlot = inventoryUI.GetEquipmentSlot(EquippableItem.EquipmentSlot.Armor);
            
            if (ve == leftHandSlot || ve == rightHandSlot || ve == armorSlot)
            {
                bool isValid = false;
                if (drag.Context.payload is EquippableItem eq)
                {
                    if (ve == armorSlot && eq.slot == EquippableItem.EquipmentSlot.Armor)
                    {
                        // Armor slots only accept armor items
                        isValid = true;
                    }
                    else if (ve == leftHandSlot || ve == rightHandSlot)
                    {
                        // Hand slots need to check dual-wielding compatibility
                        var targetSlot = (ve == leftHandSlot) ? EquippableItem.EquipmentSlot.LeftHand : EquippableItem.EquipmentSlot.RightHand;
                        
                        if (eq is EquippableHandheld)
                        {
                            // Use the actual equipment compatibility check
                            isValid = currentCharacter.IsSlotCompatible(targetSlot, eq);
                        }
                        else
                        {
                            // Non-hand-held items must match their designated slot
                            isValid = (eq.slot == targetSlot);
                        }
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

            if (eq.slot == equipmentSlot || (eq is EquippableHandheld && (equipmentSlot == EquippableItem.EquipmentSlot.LeftHand || equipmentSlot == EquippableItem.EquipmentSlot.RightHand)))
            {
                // From inventory -> equipment
                if (drag.Context.sourceSlot.slotType == SlotType.Inventory)
                {
                    HandleInventoryToEquipmentTransfer(eq, equipmentSlot);
                }
                else // equipment -> equipment
                {
                    HandleEquipmentToEquipmentTransfer(eq, equipmentSlot);
                }
                
                inventoryUI.RefreshInventoryUI();
                inventoryUI.RefreshEquipmentUI(AttachTooltipHandlers);
            }
            else
            {
                // Invalid drop: revert
                if (drag.Context.sourceSlot.slotType == SlotType.Equipment)
                {
                    currentCharacter.TryEquipItem(eq);
                    inventoryUI.RefreshEquipmentUI(AttachTooltipHandlers);
                }
                // else: inventory origin stays where it was
            }
            ClearAllHoverFeedback();
            drag.EndDrag();
        });
    }

    private void HandleInventoryToEquipmentTransfer(EquippableItem eq, EquippableItem.EquipmentSlot equipmentSlot)
    {
        int srcIdx = drag.Context.sourceSlot.inventoryIndex;
        if (srcIdx >= 0) currentCharacter.inventory.SetItemAt(srcIdx, null);
        var prev = currentCharacter.UnequipItem(equipmentSlot);
        
        // Try to equip the item
        bool equipSuccess = currentCharacter.TryEquipItemToSlot(eq, equipmentSlot);
        
        if (equipSuccess)
        {
            // Equipment successful - handle the item that was previously in the target slot
            if (prev != null)
            {
                int target = srcIdx >= 0 ? srcIdx : currentCharacter.inventory.FindFirstEmptySlot();
                if (target >= 0) currentCharacter.inventory.SetItemAt(target, prev);
            }
        }
        else
        {
            // Equipment failed due to dual-wielding incompatibility
            // Put the item back in inventory
            currentCharacter.inventory.SetItemAt(srcIdx, eq);
            
            // Put the target slot's previous item back
            if (prev != null)
            {
                currentCharacter.TryEquipItemToSlot(prev, equipmentSlot);
            }
        }
    }

    private void HandleEquipmentToEquipmentTransfer(EquippableItem eq, EquippableItem.EquipmentSlot equipmentSlot)
    {
        // Check dual-wielding compatibility BEFORE attempting to equip
        var sourceSlot = drag.Context.sourceSlot.equipmentSlot;
        var targetSlot = equipmentSlot;
        
        // Unequip any existing item in the target slot
        var prev = currentCharacter.UnequipItem(targetSlot);
        
        // Now try to equip the dragged item to the target slot
        bool equipSuccess = currentCharacter.TryEquipItemToSlot(eq, targetSlot);
        
        if (equipSuccess)
        {
            // Equipment successful - handle the item that was previously in the target slot
            if (prev != null)
            {
                // Try to put it in the source slot (this creates the swap effect)
                bool swapSuccess = currentCharacter.TryEquipItemToSlot(prev, sourceSlot);
                
                if (!swapSuccess)
                {
                    // If it can't go back to source slot (e.g., dual-wielding conflict), put it in inventory
                    int empty = currentCharacter.inventory.FindFirstEmptySlot();
                    if (empty >= 0) currentCharacter.inventory.SetItemAt(empty, prev);
                }
            }
        }
        else
        {
            // Equipment failed due to dual-wielding incompatibility
            // Put the dragged item back in its original slot
            currentCharacter.TryEquipItemToSlot(eq, sourceSlot);
            
            // Put the target slot's previous item back
            if (prev != null)
            {
                currentCharacter.TryEquipItemToSlot(prev, targetSlot);
            }
        }
    }

    private void OnTrashPointerUp(PointerUpEvent evt)
    {
        if (!drag.Context.isDragging || drag.Context.payload == null) return;
        // Confirm discard
        bool confirmed = ConfirmDiscard(drag.Context.payload);
        if (confirmed && drag.Context.sourceSlot != null && drag.Context.sourceSlot.slotType == SlotType.Inventory)
        {
            currentCharacter.inventory.SetItemAt(drag.Context.sourceSlot.inventoryIndex, null);
            inventoryUI.RefreshInventoryUI();
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
        var inventorySlots = inventoryUI.GetInventorySlots();
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
            var inventorySlots = inventoryUI.GetInventorySlots();
            var slotEl = (destIndex >= 0 && destIndex < inventorySlots.Count) ? inventorySlots[destIndex] : null;
            target = SlotModel.CreateInventory(slotEl, destIndex);
        }
        else
        {
            var ve = evt.target as VisualElement;
            while (ve != null)
            {
                var leftHandSlot = inventoryUI.GetEquipmentSlot(EquippableItem.EquipmentSlot.LeftHand);
                var rightHandSlot = inventoryUI.GetEquipmentSlot(EquippableItem.EquipmentSlot.RightHand);
                var armorSlot = inventoryUI.GetEquipmentSlot(EquippableItem.EquipmentSlot.Armor);
                
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
                        inventoryUI.RefreshInventoryUI();
                    }
                    else if (src != null && src.slotType == SlotType.Equipment)
                    {
                        currentCharacter.UnequipItem(src.equipmentSlot);
                        inventoryUI.RefreshEquipmentUI(AttachTooltipHandlers);
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
            var inventorySlots = inventoryUI.GetInventorySlots();
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
        bool isOpen = inventoryUI.IsInventoryPanelOpen();
        inventoryUI.SetInventoryPanelVisible(!isOpen);
    }
}
