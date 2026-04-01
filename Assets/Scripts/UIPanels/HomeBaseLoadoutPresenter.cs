using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Home Base preparation loadout: combat-style inventory + character sheet, party stash, drag between stash and character.
/// </summary>
public sealed class HomeBaseLoadoutPresenter
{
    private readonly VisualElement overlayRoot;
    private readonly InventoryUIManager charInventoryUI;
    private readonly InventoryUIManager stashInventoryUI;
    private VisualElement loadoutWrapper;
    private VisualElement loadoutPanel;
    private Button showCharacterLoadoutButton;
    private DragDropController drag;
    private CharacterSheet character;
    private VisualElement charTrash;
    private VisualElement stashTrash;
    private Button inventoryTabButton;
    private Button characterTabButton;
    private VisualElement inventoryTab;
    private VisualElement characterTab;
    private VisualElement statsBlockContainer;
    private VisualElement characterStatsBlockContainer;
    private VisualElement characterSkillsList;
    private VisualElement uiRoot;
    private bool globalDragBound;
    private VisualElement hoverTooltip;
    private Label hoverTooltipLabel;
    private VisualElement currentTooltipOwner;
    private IVisualElementScheduledItem pendingDetailExpand;
    private Vector2 lastPointerPos;

    public HomeBaseLoadoutPresenter(
        VisualElement overlayRoot,
        VisualElement stashColumn,
        VisualElement characterColumn,
        VisualTreeAsset combatPanelAsset,
        VisualTreeAsset stashPanelAsset,
        StyleSheet combatStyles,
        StyleSheet themeStyles)
    {
        this.overlayRoot = overlayRoot;
        if (stashPanelAsset != null)
        {
            var stashTree = stashPanelAsset.CloneTree();
            if (themeStyles != null) stashTree.styleSheets.Add(themeStyles);
            if (combatStyles != null) stashTree.styleSheets.Add(combatStyles);
            stashColumn.Add(stashTree);
            var stashGrid = stashTree.Q<VisualElement>("StashItemGrid");
            stashTrash = stashTree.Q<VisualElement>("StashTrash");
            stashInventoryUI = new InventoryUIManager(stashTree, stashGrid, null, "StashSlot");
            stashInventoryUI.SetBoundInventory(PlayerParty.sharedStash);
        }

        if (combatPanelAsset != null)
        {
            var hudRoot = combatPanelAsset.CloneTree();
            var invPanel = hudRoot.Q<VisualElement>("InventoryPanel");
            if (invPanel != null)
            {
                invPanel.RemoveFromHierarchy();
                loadoutPanel = invPanel;
                if (themeStyles != null) loadoutPanel.styleSheets.Add(themeStyles);
                if (combatStyles != null) loadoutPanel.styleSheets.Add(combatStyles);

                loadoutWrapper = new VisualElement();
                loadoutWrapper.style.flexGrow = 1;
                loadoutWrapper.style.flexShrink = 1;
                loadoutWrapper.style.flexDirection = FlexDirection.Column;
                loadoutWrapper.style.minHeight = 0;
                loadoutWrapper.style.position = Position.Relative;
                characterColumn.Add(loadoutWrapper);

                showCharacterLoadoutButton = new Button(ShowCharacterLoadoutPanel) { text = "Character loadout" };
                showCharacterLoadoutButton.AddToClassList("home-base-btn-secondary");
                showCharacterLoadoutButton.style.display = DisplayStyle.None;
                showCharacterLoadoutButton.style.alignSelf = Align.FlexStart;
                showCharacterLoadoutButton.style.marginBottom = 8;
                loadoutWrapper.Add(showCharacterLoadoutButton);
                loadoutWrapper.Add(loadoutPanel);

                // Detach combat HUD anchoring; lay out in prep column (no floating drag)
                loadoutPanel.style.right = StyleKeyword.Auto;
                loadoutPanel.style.bottom = StyleKeyword.Auto;
                loadoutPanel.style.top = StyleKeyword.Auto;
                loadoutPanel.style.left = StyleKeyword.Auto;
                loadoutPanel.style.width = new Length(100, LengthUnit.Percent);
                loadoutPanel.style.maxWidth = 640;
                // Clone keeps CombatPanel inline height (460px); clear so flex fills prep column and inner ScrollViews get a real viewport.
                loadoutPanel.style.height = StyleKeyword.Auto;
                loadoutPanel.style.minHeight = 0;
                loadoutPanel.style.flexGrow = 1;
                loadoutPanel.style.flexShrink = 1;
                loadoutPanel.style.position = Position.Relative;

                inventoryTabButton = loadoutPanel.Q<Button>("InventoryTabButton");
                characterTabButton = loadoutPanel.Q<Button>("CharacterTabButton");
                inventoryTab = loadoutPanel.Q<VisualElement>("InventoryTab");
                characterTab = loadoutPanel.Q<VisualElement>("CharacterTab");
                var itemGrid = loadoutPanel.Q<VisualElement>("ItemGrid");
                var equipmentPanel = loadoutPanel.Q<VisualElement>("EquipmentPanel");
                charTrash = loadoutPanel.Q<VisualElement>("Trash");
                statsBlockContainer = loadoutPanel.Q<VisualElement>(DerivedStatsView.ContainerName);
                characterStatsBlockContainer = loadoutPanel.Q<VisualElement>("CharacterStatsBlockContainer");
                characterSkillsList = loadoutPanel.Q<VisualElement>("CharacterSkillsList");

                charInventoryUI = new InventoryUIManager(loadoutPanel, itemGrid, equipmentPanel);
                charInventoryUI.SetInventoryPanelVisible(true);
                AddCharacterLoadoutHideButton(loadoutPanel);

                if (inventoryTabButton != null) inventoryTabButton.clicked += () => SetInventoryTab(true);
                if (characterTabButton != null) characterTabButton.clicked += () => SetInventoryTab(false);
                SetInventoryTab(true);

                if (showCharacterLoadoutButton != null)
                    UiToolkitScavengerCursors.RegisterClickPointerHover(showCharacterLoadoutButton);
                if (inventoryTabButton != null) UiToolkitScavengerCursors.RegisterClickPointerHover(inventoryTabButton);
                if (characterTabButton != null) UiToolkitScavengerCursors.RegisterClickPointerHover(characterTabButton);
                if (charTrash != null) UiToolkitScavengerCursors.RegisterDragAffordancePointerHover(charTrash);

                UIClickBlocker.MakeElementAndChildrenBlockClicks(loadoutPanel);
            }
        }

        uiRoot = overlayRoot;
        CreateHoverTooltip(uiRoot);

        if (charInventoryUI != null && stashInventoryUI != null)
        {
            drag = new DragDropController(
                uiRoot,
                () => character?.inventory,
                () => new EquipmentProxy(character),
                () => charInventoryUI.RefreshInventoryUI(),
                () => charInventoryUI.RefreshEquipmentUI(AttachTooltipHandlers),
                () => PlayerParty.sharedStash,
                () => stashInventoryUI.RefreshInventoryUI());

            stashInventoryUI.CreateInventorySlots((slot, index) =>
            {
                AttachTooltipHandlers(slot);
                AttachStashDragHandlers(slot, index);
            });
            stashInventoryUI.RefreshInventoryUI();

            if (stashTrash != null)
            {
                stashTrash.RegisterCallback<PointerUpEvent>(OnStashTrashPointerUp);
                UiToolkitScavengerCursors.RegisterDragAffordancePointerHover(stashTrash);
            }
        }
        else if (charInventoryUI != null)
        {
            drag = new DragDropController(
                uiRoot,
                () => character?.inventory,
                () => new EquipmentProxy(character),
                () => charInventoryUI.RefreshInventoryUI(),
                () => charInventoryUI.RefreshEquipmentUI(AttachTooltipHandlers));
        }

        if (charInventoryUI != null)
        {
            charInventoryUI.CreateInventorySlots((slot, index) =>
            {
                AttachTooltipHandlers(slot);
                AttachCharInventoryDragHandlers(slot, index);
            });

            RegisterEquipmentDragBeginHandlers();

            if (charTrash != null)
                charTrash.RegisterCallback<PointerUpEvent>(OnCharTrashPointerUp);

            if (uiRoot != null && !globalDragBound)
            {
                uiRoot.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
                uiRoot.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
                globalDragBound = true;
            }
        }

        PlayerParty.sharedStash ??= new Inventory();
        PlayerParty.sharedStash.OnInventoryChanged += OnStashChanged;
    }

    private void OnStashChanged()
    {
        stashInventoryUI?.RefreshInventoryUI();
    }

    public void Dispose()
    {
        if (PlayerParty.sharedStash != null)
            PlayerParty.sharedStash.OnInventoryChanged -= OnStashChanged;
    }

    public void SetCharacter(CharacterSheet sheet)
    {
        if (character == sheet) return;

        if (character != null)
        {
            character.OnInventoryChanged -= OnCharacterInvChanged;
            character.OnEquipmentChanged -= OnCharacterEquipChanged;
        }

        character = sheet;
        charInventoryUI?.SetCurrentCharacter(character);

        if (character != null)
        {
            character.OnInventoryChanged += OnCharacterInvChanged;
            character.OnEquipmentChanged += OnCharacterEquipChanged;
        }

        RefreshCharacterPanels();
    }

    private void OnCharacterInvChanged()
    {
        charInventoryUI?.RefreshInventoryUI();
        RefreshStatsBlock();
    }

    private void OnCharacterEquipChanged()
    {
        charInventoryUI?.RefreshEquipmentUI(AttachTooltipHandlers);
        RefreshStatsBlock();
    }

    private void RefreshCharacterPanels()
    {
        if (charInventoryUI == null) return;
        charInventoryUI.RefreshInventoryUI();
        charInventoryUI.RefreshEquipmentUI(AttachTooltipHandlers);
        RefreshStatsBlock();
        RefreshCharacterSkills();
    }

    private void AddCharacterLoadoutHideButton(VisualElement loadoutPanel)
    {
        if (loadoutPanel == null || loadoutPanel.childCount == 0) return;
        var headerRow = loadoutPanel[0] as VisualElement;
        if (headerRow == null) return;
        var hideBtn = new Button(HideCharacterLoadoutPanel) { text = "Hide" };
        hideBtn.AddToClassList("secondary");
        hideBtn.style.height = 30;
        hideBtn.style.minWidth = 64;
        hideBtn.style.marginLeft = 8;
        hideBtn.tooltip = "Hide character loadout (shared stash stays open). Use Character loadout to show again.";
        UiToolkitScavengerCursors.RegisterClickPointerHover(hideBtn);
        headerRow.Add(hideBtn);
    }

    private void HideCharacterLoadoutPanel()
    {
        charInventoryUI?.SetInventoryPanelVisible(false);
        if (showCharacterLoadoutButton != null)
            showCharacterLoadoutButton.style.display = DisplayStyle.Flex;
    }

    private void ShowCharacterLoadoutPanel()
    {
        charInventoryUI?.SetInventoryPanelVisible(true);
        if (showCharacterLoadoutButton != null)
            showCharacterLoadoutButton.style.display = DisplayStyle.None;
    }

    private void SetInventoryTab(bool showInventory)
    {
        if (inventoryTab != null) inventoryTab.style.display = showInventory ? DisplayStyle.Flex : DisplayStyle.None;
        if (characterTab != null) characterTab.style.display = showInventory ? DisplayStyle.None : DisplayStyle.Flex;
        if (inventoryTabButton != null)
            inventoryTabButton.EnableInClassList("action-btn--selected", showInventory);
        if (characterTabButton != null)
            characterTabButton.EnableInClassList("action-btn--selected", !showInventory);
    }

    private void RefreshStatsBlock()
    {
        DerivedStatsView.Refresh(statsBlockContainer, character, compact: true);
        DerivedStatsView.Refresh(characterStatsBlockContainer, character, compact: false);
        if (statsBlockContainer != null)
            UIClickBlocker.MakeElementAndChildrenBlockClicks(statsBlockContainer);
        if (characterStatsBlockContainer != null)
            UIClickBlocker.MakeElementAndChildrenBlockClicks(characterStatsBlockContainer);
    }

    private void RefreshCharacterSkills()
    {
        if (characterSkillsList == null) return;
        characterSkillsList.Clear();
        if (character == null) return;

        foreach (var ability in character.GetKnownAbilities())
        {
            if (ability == null) continue;
            var label = new Label(ability.displayName);
            label.AddToClassList("text");
            label.style.whiteSpace = WhiteSpace.Normal;
            label.tooltip = WrapTooltipText(ability.description, 60);
            AttachTooltipHandlers(label);
            UiToolkitScavengerCursors.RegisterClickPointerHover(label);
            characterSkillsList.Add(label);
        }

        foreach (var t in character.GetKnownSpecialActionTypes())
        {
            if (t == null) continue;
            var label = new Label(t.Name);
            label.AddToClassList("text");
            label.style.whiteSpace = WhiteSpace.Normal;
            label.tooltip = WrapTooltipText(t.Name, 60);
            AttachTooltipHandlers(label);
            UiToolkitScavengerCursors.RegisterClickPointerHover(label);
            characterSkillsList.Add(label);
        }

        UIClickBlocker.MakeElementAndChildrenBlockClicks(characterSkillsList);
    }

    private static string WrapTooltipText(string text, int maxLineLength)
    {
        if (string.IsNullOrEmpty(text) || maxLineLength <= 0) return text;
        var words = text.Split(' ');
        var sb = new StringBuilder(text.Length + 16);
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

    private void AttachStashDragHandlers(VisualElement slot, int index)
    {
        if (slot == null) return;
        if (slot.userData is int) return;
        slot.userData = index;
        slot.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0) return;
            var stash = PlayerParty.sharedStash;
            if (stash == null) return;
            var item = stash.GetItem(index);
            if (item == null) return;
            drag?.BeginDrag(SlotModel.CreateStash(slot, index), item, evt.position);
            UpdateEquipHoverFeedbackGlobal(evt.position);
        });
    }

    private void AttachCharInventoryDragHandlers(VisualElement slot, int index)
    {
        if (slot == null) return;
        if (slot.userData is int) return;
        slot.userData = index;
        slot.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0) return;
            if (character?.inventory == null) return;
            var item = character.inventory.Items[index];
            if (item == null) return;
            drag?.BeginDrag(SlotModel.CreateInventory(slot, index), item, evt.position);
            UpdateEquipHoverFeedbackGlobal(evt.position);
        });
    }

    private void RegisterEquipmentDragBeginHandlers()
    {
        charInventoryUI?.ForEachEquipSlot((slot, es) =>
        {
            if (slot.ClassListContains("home-base-equip-drag"))
                return;
            slot.AddToClassList("home-base-equip-drag");
            slot.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                var equipped = character?.GetEquippedItem(es);
                if (equipped == null) return;
                drag?.BeginDrag(SlotModel.CreateEquipment(slot, es), equipped, evt.position);
                UpdateEquipHoverFeedbackGlobal(evt.position);
            });
            AttachTooltipHandlers(slot);
        });
    }

    private void OnCharTrashPointerUp(PointerUpEvent evt)
    {
        if (drag == null || !drag.Context.isDragging || drag.Context.payload == null) return;
        if (drag.Context.sourceSlot.slotType == SlotType.Inventory && character != null)
        {
            character.inventory.SetItemAt(drag.Context.sourceSlot.inventoryIndex, null);
            charInventoryUI.RefreshInventoryUI();
        }
        else if (drag.Context.sourceSlot.slotType == SlotType.Equipment && character != null)
        {
            character.UnequipItem(drag.Context.sourceSlot.equipmentSlot);
            charInventoryUI.RefreshEquipmentUI(AttachTooltipHandlers);
        }
        ClearAllHoverFeedback();
        drag.EndDrag();
    }

    private void OnStashTrashPointerUp(PointerUpEvent evt)
    {
        if (drag == null || !drag.Context.isDragging || drag.Context.payload == null) return;
        if (drag.Context.sourceSlot.slotType == SlotType.Stash && PlayerParty.sharedStash != null)
        {
            PlayerParty.sharedStash.SetItemAt(drag.Context.sourceSlot.inventoryIndex, null);
            stashInventoryUI?.RefreshInventoryUI();
        }
        ClearAllHoverFeedback();
        drag.EndDrag();
    }

    private void OnGlobalPointerMove(PointerMoveEvent evt)
    {
        if (drag == null || !drag.Context.isDragging) return;
        drag.UpdateDrag(evt.position);
        UpdateEquipHoverFeedbackGlobal(evt.position);
    }

    private void OnGlobalPointerUp(PointerUpEvent evt)
    {
        if (drag == null || !drag.Context.isDragging) return;

        int charIdx = GetCharacterInventorySlotIndex(evt.target);
        SlotModel target = null;
        if (charIdx >= 0)
        {
            var slots = charInventoryUI?.GetInventorySlots();
            var slotEl = (slots != null && charIdx < slots.Count) ? slots[charIdx] : null;
            target = SlotModel.CreateInventory(slotEl, charIdx);
        }
        else
        {
            int stashIdx = GetStashSlotIndex(evt.target);
            if (stashIdx >= 0)
            {
                var slots = stashInventoryUI?.GetInventorySlots();
                var slotEl = (slots != null && stashIdx < slots.Count) ? slots[stashIdx] : null;
                target = SlotModel.CreateStash(slotEl, stashIdx);
            }
            else
            {
                var ve = evt.target as VisualElement;
                while (ve != null)
                {
                    if (charInventoryUI != null && charInventoryUI.TryGetSlotForElement(ve, out var eqSlot))
                    {
                        target = SlotModel.CreateEquipment(ve, eqSlot);
                        break;
                    }
                    if (ve == charTrash) { target = SlotModel.CreateTrash(charTrash); break; }
                    if (ve == stashTrash) { target = SlotModel.CreateTrash(stashTrash); break; }
                    ve = ve.parent;
                }
            }
        }

        if (target != null)
        {
            if (target.slotType == SlotType.Trash)
            {
                if (drag.Context.sourceSlot.slotType == SlotType.Inventory && character != null)
                {
                    character.inventory.SetItemAt(drag.Context.sourceSlot.inventoryIndex, null);
                    charInventoryUI.RefreshInventoryUI();
                }
                else if (drag.Context.sourceSlot.slotType == SlotType.Stash && PlayerParty.sharedStash != null)
                {
                    PlayerParty.sharedStash.SetItemAt(drag.Context.sourceSlot.inventoryIndex, null);
                    stashInventoryUI?.RefreshInventoryUI();
                }
                else if (drag.Context.sourceSlot.slotType == SlotType.Equipment && character != null)
                {
                    character.UnequipItem(drag.Context.sourceSlot.equipmentSlot);
                    charInventoryUI.RefreshEquipmentUI(AttachTooltipHandlers);
                }
            }
            else
            {
                drag.TryDropOn(target);
            }
        }

        ClearAllHoverFeedback();
        drag.EndDrag();
    }

    private int GetCharacterInventorySlotIndex(IEventHandler target)
    {
        var ve = target as VisualElement;
        while (ve != null)
        {
            if (ve.name != null && ve.name.StartsWith("InventorySlot_"))
            {
                if (int.TryParse(ve.name.Substring("InventorySlot_".Length), out int idx))
                    return idx;
            }
            var slots = charInventoryUI?.GetInventorySlots();
            if (slots != null)
            {
                int listIndex = slots.IndexOf(ve);
                if (listIndex >= 0) return listIndex;
            }
            ve = ve.parent;
        }
        return -1;
    }

    private int GetStashSlotIndex(IEventHandler target)
    {
        var ve = target as VisualElement;
        while (ve != null)
        {
            if (ve.name != null && ve.name.StartsWith("StashSlot_"))
            {
                if (int.TryParse(ve.name.Substring("StashSlot_".Length), out int idx))
                    return idx;
            }
            var slots = stashInventoryUI?.GetInventorySlots();
            if (slots != null)
            {
                int listIndex = slots.IndexOf(ve);
                if (listIndex >= 0) return listIndex;
            }
            ve = ve.parent;
        }
        return -1;
    }

    private void ClearEquipSlotFeedback(VisualElement slot)
    {
        slot.RemoveFromClassList("equip-drop-valid");
        slot.RemoveFromClassList("equip-drop-invalid");
        slot.RemoveFromClassList("equip-drop-neutral");
    }

    private void SetAllEquipNeutral()
    {
        charInventoryUI?.ForEachEquipSlot((slot, _) =>
        {
            ClearEquipSlotFeedback(slot);
            slot.AddToClassList("equip-drop-neutral");
        });
    }

    private void ClearAllHoverFeedback()
    {
        charInventoryUI?.ForEachEquipSlot((slot, _) => ClearEquipSlotFeedback(slot));
        var invSlots = charInventoryUI?.GetInventorySlots();
        if (invSlots != null)
        {
            for (int i = 0; i < invSlots.Count; i++)
            {
                var s = invSlots[i];
                if (s == null) continue;
                s.RemoveFromClassList("equip-drop-valid");
                s.RemoveFromClassList("equip-drop-invalid");
                s.RemoveFromClassList("equip-drop-neutral");
            }
        }
    }

    private void UpdateEquipHoverFeedbackGlobal(Vector2 panelPosition)
    {
        charInventoryUI?.ForEachEquipSlot((slot, _) => ClearEquipSlotFeedback(slot));
        if (drag == null || !drag.Context.isDragging) { SetAllEquipNeutral(); return; }

        var ve = uiRoot?.panel?.Pick(panelPosition) as VisualElement;
        while (ve != null)
        {
            if (charInventoryUI != null && charInventoryUI.TryGetSlotForElement(ve, out var targetSlot))
            {
                bool isValid = false;
                if (drag.Context.payload is EquippableItem eq && character != null)
                    isValid = character.IsSlotCompatible(targetSlot, eq);
                ve.AddToClassList(isValid ? "equip-drop-valid" : "equip-drop-invalid");
                break;
            }
            ve = ve.parent;
        }
    }

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
                PositionTooltip(evt.position);
        });

        element.RegisterCallback<PointerLeaveEvent>(_ =>
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
        if (element.name != null && element.name.StartsWith("InventorySlot_") && character?.inventory != null)
        {
            if (int.TryParse(element.name.Substring("InventorySlot_".Length), out int idx))
            {
                var item = (idx >= 0 && idx < character.inventory.Items.Count) ? character.inventory.Items[idx] : null;
                return TooltipTextBuilder.ForItem(item);
            }
        }

        if (element.name != null && element.name.StartsWith("StashSlot_") && PlayerParty.sharedStash != null)
        {
            if (int.TryParse(element.name.Substring("StashSlot_".Length), out int idx))
            {
                var item = PlayerParty.sharedStash.GetItem(idx);
                return TooltipTextBuilder.ForItem(item);
            }
        }

        if (character != null && charInventoryUI != null)
        {
            var ve = element;
            while (ve != null)
            {
                if (charInventoryUI.TryGetSlotForElement(ve, out var slot))
                {
                    var item = character.GetEquippedItem(slot);
                    return TooltipTextBuilder.ForEquipped(item, slot);
                }
                ve = ve.parent;
            }
        }

        if (!string.IsNullOrEmpty(element.tooltip))
            return (element.tooltip, element.tooltip);
        return (null, null);
    }

    private void PositionTooltip(Vector2 panelPosition)
    {
        const float offsetX = 12f;
        const float offsetY = 12f;
        const float tooltipWidth = 200f;
        const float tooltipHeight = 100f;
        float desiredX = panelPosition.x + offsetX;
        float desiredY = panelPosition.y + offsetY;
        hoverTooltip.style.left = Mathf.Clamp(desiredX, 0f, Screen.width - tooltipWidth);
        hoverTooltip.style.top = Mathf.Clamp(desiredY, 0f, Screen.height - tooltipHeight);
    }
}
