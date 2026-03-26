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
    private VisualElement actionBar;
    private VisualElement trashSlot;
    private Label actionPointsLabel;
    private CharacterSheet currentCharacter;
    private InventoryUIManager inventoryUI;
    private VisualElement statsBlockContainer;
    private VisualElement characterStatsBlockContainer;
    private VisualElement characterSkillsList;

    private Button inventoryTabButton;
    private Button characterTabButton;
    private VisualElement inventoryTab;
    private VisualElement characterTab;

    // UI root reference (the cloned VisualTree) for overlays like drag ghost
    private VisualElement uiRoot;
    private bool globalDragBound = false;

    // Drag-and-drop (will be moved to DragDropController progressively)
    private DragDropController drag;
    
    // Lightweight UI Toolkit tooltip
    private VisualElement hoverTooltip;
    private Label hoverTooltipLabel;
    private VisualElement currentTooltipOwner;
    private IVisualElementScheduledItem pendingDetailExpand;
    private Vector2 lastPointerPos;

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
        inventoryTabButton = visualTree.Q<Button>("InventoryTabButton");
        characterTabButton = visualTree.Q<Button>("CharacterTabButton");
        inventoryTab = visualTree.Q<VisualElement>("InventoryTab");
        characterTab = visualTree.Q<VisualElement>("CharacterTab");
        var itemGrid = visualTree.Q<VisualElement>("ItemGrid");
        var equipmentPanel = visualTree.Q<VisualElement>("EquipmentPanel");
        trashSlot = visualTree.Q<VisualElement>("Trash");
        actionBar = visualTree.Q<VisualElement>("ActionBar");
        actionPointsLabel = visualTree.Q<Label>("ActionPointsLabel");

        // Initialize inventory UI manager (equipment slots are found by name under equipmentPanel)
        inventoryUI = new InventoryUIManager(inventoryPanel, itemGrid, equipmentPanel);
        statsBlockContainer = visualTree.Q<VisualElement>(DerivedStatsView.ContainerName);
        characterStatsBlockContainer = visualTree.Q<VisualElement>("CharacterStatsBlockContainer");
        characterSkillsList = visualTree.Q<VisualElement>("CharacterSkillsList");

        // Tabs
        if (inventoryTabButton != null) inventoryTabButton.clicked += () => SetInventoryTab(true);
        if (characterTabButton != null) characterTabButton.clicked += () => SetInventoryTab(false);
        SetInventoryTab(true);

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

        // Initialize combat log UI
        var combatLogPanel = visualTree.Q<VisualElement>("CombatLogPanel");
        if (combatLogPanel != null)
        {
            UIClickBlocker.MakeElementAndChildrenBlockClicks(combatLogPanel);
        }
        var combatLogUI = gameObject.AddComponent<CombatLogUI>();
        combatLogUI.Initialize(visualTree);
        CombatLog.Clear();

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

		// Create a tiny non-blocking enemy movement label
		enemyMoveLabel = new Label("Enemy is moving...");
		enemyMoveLabel.style.position = Position.Absolute;
		enemyMoveLabel.style.left = 0;
		enemyMoveLabel.style.right = 0;
		enemyMoveLabel.style.top = 8;
		enemyMoveLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
		enemyMoveLabel.style.fontSize = 18;
		enemyMoveLabel.style.color = new Color(1f, 0.85f, 0.3f, 1f);
		enemyMoveLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
		enemyMoveLabel.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
		enemyMoveLabel.style.display = DisplayStyle.None;
		enemyMoveLabel.pickingMode = PickingMode.Ignore;
		uiRoot.Add(enemyMoveLabel);

		// Add the UI elements to the root visual element
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null)
        {
            uiDoc = FindFirstObjectByType<UIDocument>(FindObjectsInactive.Exclude);
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

    /// <summary>
    /// Show a full-screen VICTORY or DEFEAT banner for several seconds.
    /// Called by TurnManager when combat ends.
    /// </summary>
    public void ShowGameOver(bool victory)
    {
        if (uiRoot == null) return;
        StartCoroutine(ShowGameOverCoroutine(victory));
    }

    private const float GameOverDisplayDuration = 4f;

    private IEnumerator ShowGameOverCoroutine(bool victory)
    {
        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.top = 0;
        overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        overlay.style.justifyContent = Justify.Center;
        overlay.style.alignItems = Align.Center;
        overlay.pickingMode = PickingMode.Ignore;

        var label = new Label(victory ? "VICTORY" : "DEFEAT");
        label.style.fontSize = 72;
        label.style.color = victory ? new Color(0.9f, 0.85f, 0.2f) : new Color(0.95f, 0.2f, 0.2f);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;

        overlay.Add(label);
        uiRoot.Add(overlay);

        yield return new WaitForSeconds(GameOverDisplayDuration);

        if (overlay.parent != null)
            overlay.RemoveFromHierarchy();
    }

    private TurnManager turnManager;

    private void Awake()
    {
        turnManager = FindFirstObjectByType<TurnManager>(FindObjectsInactive.Exclude);
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

		// Toggle enemy move label
		if (enemyMoveLabel != null)
		{
			bool show = (turnManager != null) && turnManager.IsEnemyTurn();
			if (show)
			{
				EnemyController active = null;
				var enemies = FindObjectsByType<EnemyController>(
					findObjectsInactive: FindObjectsInactive.Exclude,
					sortMode: FindObjectsSortMode.None);
				for (int i = 0; i < enemies.Length; i++) { if (enemies[i] != null && enemies[i].isTurn) { active = enemies[i]; break; } }
				show = (active != null) && active.isActing;
			}
			enemyMoveLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
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
                currentCharacter.OnInventoryChanged -= inventoryUI.RefreshInventoryUI;
                currentCharacter.OnEquipmentChanged -= () => inventoryUI.RefreshEquipmentUI(AttachTooltipHandlers);
                currentCharacter.OnActionPointsChanged -= UpdateActionPointsDisplay;
            }

            currentCharacter = newCharacter;
            inventoryUI.SetCurrentCharacter(currentCharacter);

            // Subscribe to new character
            if (currentCharacter != null)
            {
                currentCharacter.OnInventoryChanged += inventoryUI.RefreshInventoryUI;
                currentCharacter.OnInventoryChanged += () => RefreshActionBar();
                currentCharacter.OnEquipmentChanged += () => { inventoryUI.RefreshEquipmentUI(AttachTooltipHandlers); RefreshActionBar(); RefreshStatsBlock(); };
                currentCharacter.OnActionPointsChanged += UpdateActionPointsDisplay;
                // Also reapply selection validity on equipment changes
                currentCharacter.OnEquipmentChanged += () => {
                    var controller = GetActivePlayerController();
                    if (controller != null)
                    {
                        // Ensure controller validates and UI reflects current selection
                        RefreshActionBar();
                    }
                };
                inventoryUI.RefreshInventoryUI();
                inventoryUI.RefreshEquipmentUI(AttachTooltipHandlers);
                RefreshStatsBlock();
                AttachEquipmentDragTargets();
                RefreshActionBar();
                UpdateActionPointsDisplay();
            }
        }
    }

    private void RefreshStatsBlock()
    {
        DerivedStatsView.Refresh(statsBlockContainer, currentCharacter, compact: true);
        DerivedStatsView.Refresh(characterStatsBlockContainer, currentCharacter, compact: false);
        RefreshCharacterSkills();

        // Ensure newly created stats elements block world clicks
        UIClickBlocker.MakeElementAndChildrenBlockClicks(statsBlockContainer);
        UIClickBlocker.MakeElementAndChildrenBlockClicks(characterStatsBlockContainer);
    }

    private void RefreshCharacterSkills()
    {
        if (characterSkillsList == null) return;
        characterSkillsList.Clear();
        if (currentCharacter == null) return;

        // Data-driven abilities
        foreach (var ability in currentCharacter.GetKnownAbilities())
        {
            if (ability == null) continue;
            var label = new Label(ability.displayName);
            label.AddToClassList("text");
            label.style.whiteSpace = WhiteSpace.Normal;
            label.tooltip = WrapTooltipText(ability.description, 60);
            AttachTooltipHandlers(label);
            characterSkillsList.Add(label);
        }

        // Legacy special actions
        foreach (var t in currentCharacter.GetKnownSpecialActionTypes())
        {
            if (t == null) continue;
            var label = new Label(t.Name);
            label.AddToClassList("text");
            label.style.whiteSpace = WhiteSpace.Normal;
            label.tooltip = WrapTooltipText(t.Name, 60);
            AttachTooltipHandlers(label);
            characterSkillsList.Add(label);
        }

        // Skills list should also fully block world clicks
        UIClickBlocker.MakeElementAndChildrenBlockClicks(characterSkillsList);
    }

    private static string WrapTooltipText(string text, int maxLineLength)
    {
        if (string.IsNullOrEmpty(text) || maxLineLength <= 0) return text;
        var words = text.Split(' ');
        System.Text.StringBuilder sb = new System.Text.StringBuilder(text.Length + 16);
        int current = 0;
        for (int i = 0; i < words.Length; i++)
        {
            var w = words[i];
            if (current == 0)
            {
                sb.Append(w);
                current = w.Length;
            }
            else if (current + 1 + w.Length > maxLineLength)
            {
                sb.Append('\n');
                sb.Append(w);
                current = w.Length;
            }
            else
            {
                sb.Append(' ');
                sb.Append(w);
                current += 1 + w.Length;
            }
        }
        return sb.ToString();
    }

    private void SetInventoryTab(bool showInventory)
    {
        if (inventoryTab != null) inventoryTab.style.display = showInventory ? DisplayStyle.Flex : DisplayStyle.None;
        if (characterTab != null) characterTab.style.display = showInventory ? DisplayStyle.None : DisplayStyle.Flex;

        if (inventoryTabButton != null)
        {
            inventoryTabButton.EnableInClassList("action-btn--selected", showInventory);
        }
        if (characterTabButton != null)
        {
            characterTabButton.EnableInClassList("action-btn--selected", !showInventory);
        }
    }

    private CharacterSheet GetCurrentPlayerCharacter()
    {
        if (turnManager == null || !turnManager.IsPlayerTurn()) return null;

        var players = FindObjectsByType<PlayerController>(
            findObjectsInactive: FindObjectsInactive.Exclude,
            sortMode: FindObjectsSortMode.None);
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
            if (inventoryUI.TryGetSlotForElement(ve, out var targetSlot))
            {
                bool isValid = false;
                if (drag.Context.payload is EquippableItem eq && currentCharacter != null)
                    isValid = currentCharacter.IsSlotCompatible(targetSlot, eq);
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
                if (inventoryUI.TryGetSlotForElement(ve, out var eqSlot))
                {
                    target = SlotModel.CreateEquipment(ve, eqSlot);
                    break;
                }
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
            lastPointerPos = evt.position;
            var content = BuildTieredTooltipContent(element);
            if (string.IsNullOrEmpty(content.compact) && string.IsNullOrEmpty(content.detailed)) return;
            currentTooltipOwner = element;
            ShowTooltipText(content.compact ?? content.detailed, evt.position);

            CancelPendingDetailExpand();
            if (!TooltipDetailSettings.NeverExpandOnHover && !string.IsNullOrEmpty(content.detailed))
            {
                pendingDetailExpand = element.schedule.Execute(() =>
                {
                    if (currentTooltipOwner != element) return;
                    ShowTooltipText(content.detailed, lastPointerPos);
                }).StartingIn((long)(TooltipDetailSettings.DetailDelaySeconds * 1000f));
            }
        });

        element.RegisterCallback<PointerMoveEvent>(evt =>
        {
            lastPointerPos = evt.position;
            if (hoverTooltip.style.display == DisplayStyle.Flex)
            {
                PositionTooltip(evt.position);
            }
        });

        element.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 1) return; // right click
            var content = BuildTieredTooltipContent(element);
            if (string.IsNullOrEmpty(content.detailed)) return;
            currentTooltipOwner = element;
            ShowTooltipText(content.detailed, evt.position);
            CancelPendingDetailExpand();
            evt.StopPropagation();
        });

        element.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            CancelPendingDetailExpand();
            if (currentTooltipOwner == element) currentTooltipOwner = null;
            hoverTooltip.style.display = DisplayStyle.None;
        });
    }

    private void CancelPendingDetailExpand()
    {
        if (pendingDetailExpand == null) return;
        pendingDetailExpand.Pause();
        pendingDetailExpand = null;
    }

    private void ShowTooltipText(string text, Vector2 panelPosition)
    {
        if (hoverTooltipLabel == null || hoverTooltip == null) return;
        hoverTooltipLabel.text = text ?? "";
        hoverTooltip.style.display = DisplayStyle.Flex;
        PositionTooltip(panelPosition);
    }

    private (string compact, string detailed) BuildTieredTooltipContent(VisualElement element)
    {
        // Action buttons store a payload in userData
        if (element.userData is ActionTooltipPayload atp)
        {
            return (atp.compact, atp.detailed);
        }

        // Inventory slots by name
        if (element.name != null && element.name.StartsWith("InventorySlot_") && currentCharacter?.inventory != null)
        {
            if (int.TryParse(element.name.Substring("InventorySlot_".Length), out int idx))
            {
                var item = (idx >= 0 && idx < currentCharacter.inventory.Items.Count) ? currentCharacter.inventory.Items[idx] : null;
                return TooltipTextBuilder.ForItem(item);
            }
        }

        // Equipment slots
        if (currentCharacter != null)
        {
            var ve = element;
            while (ve != null)
            {
                if (inventoryUI != null && inventoryUI.TryGetSlotForElement(ve, out var slot))
                {
                    var item = currentCharacter.GetEquippedItem(slot);
                    return TooltipTextBuilder.ForEquipped(item, slot);
                }
                ve = ve.parent;
            }
        }

        // Generic
        if (!string.IsNullOrEmpty(element.tooltip))
        {
            return (element.tooltip, element.tooltip);
        }
        return (null, null);
    }

    private sealed class ActionTooltipPayload
    {
        public string compact;
        public string detailed;
    }

    private void PositionTooltip(Vector2 panelPosition)
    {
        // Offset tooltip a bit from the cursor
        float offsetX = 12f;
        float offsetY = 12f;
        
        // Clamp tooltip to stay within screen bounds
        float tooltipWidth = 200f; // Reasonable estimate for UI tooltips
        float tooltipHeight = 100f; // Reasonable estimate for multi-line tooltips
        
        float desiredX = panelPosition.x + offsetX;
        float desiredY = panelPosition.y + offsetY;
        
        float clampedX = Mathf.Clamp(desiredX, 0f, Screen.width - tooltipWidth);
        float clampedY = Mathf.Clamp(desiredY, 0f, Screen.height - tooltipHeight);
        
        hoverTooltip.style.left = clampedX;
        hoverTooltip.style.top = clampedY;
    }

    private void OnEndTurnButtonClicked()
    {
        PlayerController[] pcs = FindObjectsByType<PlayerController>(
            findObjectsInactive: FindObjectsInactive.Exclude,
            sortMode: FindObjectsSortMode.None);
        foreach (PlayerController pc in pcs)
        {
            // The controller is responsible for only ending turn if the click is valid.
            pc.EndTurnButtonClick();
        }
    }

    // -------------------- Action Bar --------------------
    private void RefreshActionBar()
    {
        if (actionBar == null) return;
        actionBar.Clear();
        var controller = GetActivePlayerController();
        if (controller == null) return;
        
        // Update action points display
        UpdateActionPointsDisplay();

        // Collect actions: up to 2 from hands, plus specials from character class
        List<(string key, string className, string label)> actions = new List<(string, string, string)>();
        var right = currentCharacter?.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
        var left = currentCharacter?.GetEquippedItem(EquippableItem.EquipmentSlot.LeftHand) as EquippableHandheld;

        // Always add Kick option (allows movement when other items block it)
        actions.Add((nameof(ActionKick), nameof(ActionKick), "Kick"));

        if (right != null)
        {
            string rn = string.IsNullOrEmpty(right.associatedActionClass) ? nameof(ActionMeleeAttack) : right.associatedActionClass;
            actions.Add(($"{rn}:RightHand", rn, right.itemName));
        }
        if (left != null)
        {
            string ln = string.IsNullOrEmpty(left.associatedActionClass) ? nameof(ActionMeleeAttack) : left.associatedActionClass;
            actions.Add(($"{ln}:LeftHand", ln, left.itemName));
        }

        // Legacy special actions (type-based)
        foreach (var t in currentCharacter.GetKnownSpecialActionTypes())
        {
            if (t == null) continue;
            actions.Add((t.Name, t.Name, t.Name));
        }

        // Data-driven abilities
        foreach (var ability in currentCharacter.GetKnownAbilities())
        {
            if (ability == null) continue;
            string cls = ability.ArchetypeClassName();
            actions.Add(($"ability:{ability.id}", cls, ability.displayName));
        }

        string selectedName = controller.GetSelectedActionClassName();
        string selectedKey = controller.GetSelectedActionKey();
        for (int i = 0; i < actions.Count; i++)
        {
            var tuple = actions[i];
            var btn = new Button();
            btn.text = tuple.label;
            btn.AddToClassList("action-btn");
            // Ensure this element blocks world clicks
            btn.AddToClassList("ui-blocker");
            if (!string.IsNullOrEmpty(selectedKey) ? (tuple.key == selectedKey) : (tuple.className == selectedName))
            {
                btn.AddToClassList("action-btn--selected");
            }
            // Use a local copy to avoid closure capture issues
            string key = tuple.key;
            string cls = tuple.className;
            btn.clicked += () => { OnActionButtonClicked(controller, key, cls); };
            // Ensure button is clickable even inside a ui-blocker container
            btn.pickingMode = PickingMode.Position;
            // Tooltip: compact on hover, details after delay/right-click
            var detailed = BuildActionTooltip(key, cls);
            btn.userData = new ActionTooltipPayload
            {
                compact = tuple.label,
                detailed = detailed
            };
            btn.tooltip = tuple.label;
            AttachTooltipHandlers(btn);
            actionBar.Add(btn);
        }
    }

    private PlayerController GetActivePlayerController()
    {
        var players = FindObjectsByType<PlayerController>(
            findObjectsInactive: FindObjectsInactive.Exclude,
            sortMode: FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].isTurn)
            {
                return players[i];
            }
        }
        return null;
    }

    private void OnActionButtonClicked(PlayerController controller, string key, string className)
    {
        // Toggle logic: if click current, reset to default; else select new
        string currentKey = controller.GetSelectedActionKey();
        if (!string.IsNullOrEmpty(currentKey) ? (currentKey == key) : (controller.GetSelectedActionClassName() == className))
        {
            controller.ResetSelectedActionToDefault();
        }
        else
        {
            controller.SelectAction(key, className);
        }
        RefreshActionBar();
    }

    private void UpdateActionPointsDisplay()
    {
        if (actionPointsLabel == null || currentCharacter == null) return;
        
        int currentAP = currentCharacter.currentActionPoints;
        actionPointsLabel.text = $"AP: {currentAP}";
        
        // Color the text based on action points remaining
        if (currentAP == 0)
        {
            actionPointsLabel.style.color = new Color(0.9f, 0.4f, 0.4f); // Red when no AP
        }
        else if (currentAP <= 3)
        {
            actionPointsLabel.style.color = new Color(1f, 0.8f, 0.4f); // Orange when low AP
        }
        else
        {
            actionPointsLabel.style.color = new Color(0.9f, 0.95f, 1f); // Normal white
        }
    }

    private string BuildActionTooltip(string key, string className)
    {
        var controller = GetActivePlayerController();
        
        // Get the action to determine cost
        Action action = null;
        if (controller != null)
        {
            var t = FindTypeByName(className);
            if (t != null)
            {
                action = controller.GetComponent(t) as Action;
            }
        }

        // If this corresponds to a hand-held item action, combine item description with action cost
        bool isRight = key != null && key.EndsWith(":RightHand");
        bool isLeft = key != null && key.EndsWith(":LeftHand");
        if ((isRight || isLeft) && currentCharacter != null)
        {
            var slot = isRight ? EquippableItem.EquipmentSlot.RightHand : EquippableItem.EquipmentSlot.LeftHand;
            var item = currentCharacter.GetEquippedItem(slot) as EquippableHandheld;
            if (item != null)
            {
                string tooltip = "";
                if (action != null && action.BASE_ACTION_COST > 0)
                {
                    tooltip += $"Action Cost: {action.BASE_ACTION_COST} AP\n";
                }
                if (!string.IsNullOrEmpty(item.description))
                {
                    tooltip += item.description;
                }
                return tooltip;
            }
        }

        // For special abilities, use the action's Description() method
        if (action != null)
        {
            return action.Description();
        }
        
        return className;
    }

    private static System.Type FindTypeByName(string className)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = System.Linq.Enumerable.FirstOrDefault(asm.GetTypes(), x => x.Name == className);
            if (t != null) return t;
        }
        return null;
    }

	private void OnToggleInventoryClicked()
    {
        bool isOpen = inventoryUI.IsInventoryPanelOpen();
        inventoryUI.SetInventoryPanelVisible(!isOpen);
    }

	// Minimal field for enemy move UI
	private Label enemyMoveLabel;
}
