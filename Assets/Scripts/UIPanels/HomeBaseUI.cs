using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Weekly command screen for <see cref="SceneNames.HomeBase"/>. Missions, fixed excursion squad slots
/// (dropdown + drag from roster), full roster with assignment icons, base assignments stub, preparation modal.
/// </summary>
public class HomeBaseUI : MonoBehaviour
{
    public VisualTreeAsset HomeBaseTemplate;
    public StyleSheet HomeBaseStyles;
    public StyleSheet ThemeStyles;

    [Header("Preparation loadout (same assets as combat inventory panel)")]
    [Tooltip("Typically Assets/UI/UXML/CombatPanel.uxml — used to extract the InventoryPanel subtree.")]
    public VisualTreeAsset CombatLoadoutTemplate;
    [Tooltip("Typically Assets/UI/UXML/StashPanel.uxml")]
    public VisualTreeAsset StashPanelTemplate;
    [Tooltip("Typically same as CombatPanelUI CombatMenuStyles (CombatPanel.uss).")]
    public StyleSheet CombatLoadoutStyles;

    private const int MaxSquadSlots = 4;
    private const int MaxRosterCapacity = 8;

    private VisualElement uiTreeRoot;
    private VisualElement preparationOverlay;

    private Label weekValueLabel;
    private Label goldValueLabel;
    private Label rosterCountLabel;
    private Button confirmWeekButton;
    private Label missionDetailLabel;
    private readonly VisualElement[] missionCards = new VisualElement[3];
    private readonly Label[] missionNames = new Label[3];
    private readonly Label[] missionTags = new Label[3];
    private readonly Label[] missionRewards = new Label[3];
    private readonly Label[] squadSlotLabels = new Label[4];
    private readonly Label[] squadSlotHints = new Label[4];
    private readonly Button[] squadSlotRemoves = new Button[4];
    private readonly Button[] squadSlotPicks = new Button[4];
    private readonly VisualElement[] squadSlotRoots = new VisualElement[4];
    private VisualElement rosterChips;
    private Label rosterLegendLabel;
    private VisualElement assignmentsList;
    private Label excursionTitleLabel;
    private Button recruitButton;

    private int selectedMissionIndex = -1;
    private int selectedRosterIndex;

    private HomeBaseLoadoutPresenter loadoutPresenter;
    private bool preparationOverlayOpen;

    private int dragPartyIndex = -1;
    private VisualElement dragGhost;
    private VisualElement dragSourceRow;
    private VisualElement dragPointerCaptureElement;
    private int dragPointerId = -1;
    private int dragHoverSlot = -1;

    /// <summary>Pointer-down without crossing threshold → roster click (select). Crossing threshold → drag.</summary>
    private int pendingRosterPartyIndex = -1;
    private VisualElement pendingRosterRow;
    private Vector2 pendingRosterStartPanelPos;
    private int pendingRosterPointerId = -1;
    private const float RosterDragThresholdPx = 8f;

    private VisualElement pickMenuOverlay;
    private int pickMenuSlot = -1;

    private EventCallback<PointerMoveEvent> rosterInteractionMoveHandler;
    private EventCallback<PointerUpEvent> rosterInteractionUpHandler;
    private EventCallback<PointerCaptureOutEvent> rosterDragCaptureOutHandler;
    private EventCallback<PointerMoveEvent> rosterDragCapturedMoveHandler;
    private EventCallback<PointerUpEvent> rosterDragCapturedUpHandler;
    private EventCallback<PointerCancelEvent> rosterDragCapturedCancelHandler;

    /// <summary>UITK often uses 0 or -1 for the mouse across pointer phases; mismatches skip cleanup and leave drag stuck.</summary>
    static bool RosterPointerIdsMatch(int a, int b)
    {
        if (a == b) return true;
        return (a == 0 || a == -1) && (b == 0 || b == -1);
    }

    private static readonly (string name, string tag, string reward, string detail)[] StubMissions =
    {
        ("Bog sortie", "Morale decay biome", "Salvage, chems", "Infested wetlands; slow passive morale drain. Good for reagent farming."),
        ("Convoy escort", "Escort objective", "Credits + rep", "Protect the truck across the map. Failure if the convoy is destroyed."),
        ("Ruins breach", "Heavy loot", "Relics, risk", "Dense enemy clusters; no decay. Bring explosives and med kits."),
    };

    private void OnDestroy()
    {
        loadoutPresenter?.Dispose();
        loadoutPresenter = null;
        if (uiTreeRoot != null)
        {
            uiTreeRoot.UnregisterCallback(rosterInteractionMoveHandler);
            uiTreeRoot.UnregisterCallback(rosterInteractionUpHandler);
        }
        if (dragSourceRow != null && rosterDragCaptureOutHandler != null)
            dragSourceRow.UnregisterCallback(rosterDragCaptureOutHandler);
        if (dragPointerCaptureElement != null && dragPointerId >= 0)
            dragPointerCaptureElement.ReleasePointer(dragPointerId);
        dragGhost?.RemoveFromHierarchy();
    }

    private void Start()
    {
        PlayerParty.EnsureInitialized();

        if (HomeBaseTemplate == null)
        {
            Debug.LogError("HomeBaseUI: HomeBaseTemplate is not assigned.");
            return;
        }

        // Same as MainMenuUI / CombatPanelUI: parameterless CloneTree() returns the template root.
        VisualElement tree = HomeBaseTemplate.CloneTree();
        tree.name = "HomeBaseUI_Root";
        tree.style.flexGrow = 1;
        tree.style.width = Length.Percent(100);
        tree.style.height = Length.Percent(100);
        uiTreeRoot = tree;
        var rootProbe = tree.Q<VisualElement>("HomeBaseRoot");
        if (HomeBaseTemplate.importedWithErrors)
        {
            Debug.LogError(
                "HomeBaseUI: VisualTreeAsset HomeBase.uxml was imported with errors. Open the Console (clear on play off) " +
                "and fix UXML/USS issues, then reimport Assets/UI/UXML/HomeBase.uxml.");
        }
        if (tree.childCount == 0 && rootProbe == null)
        {
            Debug.LogError(
                "HomeBaseUI: CloneTree() left the root empty. Reimport Assets/UI/UXML/HomeBase.uxml " +
                "and confirm HomeBaseTemplate references that VisualTreeAsset.");
        }

        if (ThemeStyles != null)
            tree.styleSheets.Add(ThemeStyles);
        if (HomeBaseStyles != null)
            tree.styleSheets.Add(HomeBaseStyles);

        preparationOverlay = tree.Q<VisualElement>("PreparationOverlay");
        if (preparationOverlay != null)
        {
            preparationOverlay.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == preparationOverlay)
                    HidePreparation();
            });
        }

        weekValueLabel = tree.Q<Label>("WeekValueLabel");
        goldValueLabel = tree.Q<Label>("GoldValueLabel");
        rosterCountLabel = tree.Q<Label>("RosterCountLabel");
        confirmWeekButton = tree.Q<Button>("ConfirmWeekButton");
        missionDetailLabel = tree.Q<Label>("MissionDetailLabel");
        rosterChips = tree.Q<VisualElement>("RosterChips");
        rosterLegendLabel = tree.Q<Label>("RosterLegendLabel");
        assignmentsList = tree.Q<VisualElement>("AssignmentsList");
        excursionTitleLabel = tree.Q<Label>("ExcursionTitleLabel");

        for (int i = 0; i < 3; i++)
        {
            missionCards[i] = tree.Q<VisualElement>($"MissionCard{i}");
            missionNames[i] = tree.Q<Label>($"MissionName{i}");
            missionTags[i] = tree.Q<Label>($"MissionTag{i}");
            missionRewards[i] = tree.Q<Label>($"MissionReward{i}");
            int idx = i;
            if (missionCards[i] != null)
            {
                missionCards[i].RegisterCallback<ClickEvent>(_ => SelectMission(idx));
                missionCards[i].focusable = true;
            }
        }

        for (int i = 0; i < MaxSquadSlots; i++)
        {
            squadSlotLabels[i] = tree.Q<Label>($"SquadSlotLabel{i}");
            squadSlotHints[i] = tree.Q<Label>($"SquadSlotHint{i}");
            squadSlotRemoves[i] = tree.Q<Button>($"SquadSlotRemove{i}");
            squadSlotPicks[i] = tree.Q<Button>($"SquadSlotPick{i}");
            squadSlotRoots[i] = tree.Q<VisualElement>($"SquadSlot{i}");
            int idx = i;
            if (squadSlotRoots[i] != null)
            {
                squadSlotRoots[i].RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target is Button) return;
                    OnExcursionSquadSlotClicked(idx);
                });
                squadSlotRoots[i].focusable = true;
            }
            if (squadSlotRemoves[i] != null)
            {
                squadSlotRemoves[i].RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    PlayerParty.TryClearExcursionSlot(idx);
                    RefreshAll();
                });
            }
            if (squadSlotPicks[i] != null)
            {
                squadSlotPicks[i].RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    ShowExcursionPickMenu(idx);
                });
            }
        }

        var openPrep = tree.Q<Button>("OpenPreparationButton");
        if (openPrep != null) openPrep.clicked += ShowPreparation;

        var closePrep = tree.Q<Button>("ClosePreparationButton");
        if (closePrep != null) closePrep.clicked += HidePreparation;

        recruitButton = tree.Q<Button>("RecruitButton");
        if (recruitButton != null) recruitButton.clicked += OnRecruitClicked;

        var stashBtn = tree.Q<Button>("StashSummaryButton");
        if (stashBtn != null) stashBtn.clicked += ShowPreparation;

        var quickBtn = tree.Q<Button>("QuickStatusButton");
        if (quickBtn != null) quickBtn.clicked += () => Debug.Log("Morale / heal quick actions (stub)");

        var mainMenuBtn = tree.Q<Button>("MainMenuButton");
        if (mainMenuBtn != null) mainMenuBtn.clicked += () => SceneManager.LoadScene(SceneNames.MainMenu);

        if (confirmWeekButton != null)
            confirmWeekButton.clicked += OnConfirmWeekClicked;

        var learnBtn = tree.Q<Button>("LearnSkillButton");
        if (learnBtn != null) learnBtn.clicked += () => Debug.Log("Learn skill from teach item (stub)");

        UIDocument uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null)
            uiDoc = FindFirstObjectByType<UIDocument>(FindObjectsInactive.Exclude);
        if (uiDoc == null || uiDoc.rootVisualElement == null)
        {
            Debug.LogError("HomeBaseUI: UIDocument or root missing.");
            return;
        }

        uiDoc.rootVisualElement.Add(tree);

        rosterInteractionMoveHandler = OnRosterInteractionMove;
        rosterInteractionUpHandler = OnRosterInteractionUp;
        rosterDragCaptureOutHandler = OnRosterDragPointerCaptureOut;
        rosterDragCapturedMoveHandler = OnRosterDragCapturedMove;
        rosterDragCapturedUpHandler = OnRosterDragCapturedUp;
        rosterDragCapturedCancelHandler = OnRosterDragCapturedCancel;

        var party = PlayerParty.partyMembers;
        if (party != null && party.Count > 0)
            selectedRosterIndex = Mathf.Clamp(selectedRosterIndex, 0, party.Count - 1);
        else
            selectedRosterIndex = 0;

        if (rosterLegendLabel != null)
        {
            rosterLegendLabel.text =
                "Legend: ⊕ excursion · ⌂ base job · · unassigned — dropdown lists only unassigned heroes; drag can move anyone.";
        }

        PopulateStubMissions();
        RefreshAll();
        RegisterHomeBaseInteractiveCursors();
    }

    /// <summary>
    /// Hand: missions, buttons, excursion slot tiles (click / UI; drop feedback uses hover class while dragging from roster).
    /// Gauntlet: roster rows only (drag source). Inventory gauntlets are handled in InventoryUIManager.
    /// </summary>
    private void RegisterHomeBaseInteractiveCursors()
    {
        if (uiTreeRoot == null) return;

        if (confirmWeekButton != null) UiToolkitScavengerCursors.RegisterClickPointerHover(confirmWeekButton);

        for (int i = 0; i < missionCards.Length; i++)
        {
            if (missionCards[i] != null)
                UiToolkitScavengerCursors.RegisterClickPointerHover(missionCards[i]);
        }

        for (int i = 0; i < MaxSquadSlots; i++)
        {
            if (squadSlotRoots[i] != null)
                UiToolkitScavengerCursors.RegisterClickPointerHover(squadSlotRoots[i]);
            if (squadSlotRemoves[i] != null) UiToolkitScavengerCursors.RegisterClickPointerHover(squadSlotRemoves[i]);
            if (squadSlotPicks[i] != null) UiToolkitScavengerCursors.RegisterClickPointerHover(squadSlotPicks[i]);
        }

        var openPrep = uiTreeRoot.Q<Button>("OpenPreparationButton");
        if (openPrep != null) UiToolkitScavengerCursors.RegisterClickPointerHover(openPrep);
        var closePrep = uiTreeRoot.Q<Button>("ClosePreparationButton");
        if (closePrep != null) UiToolkitScavengerCursors.RegisterClickPointerHover(closePrep);
        if (recruitButton != null) UiToolkitScavengerCursors.RegisterClickPointerHover(recruitButton);

        var stashBtn = uiTreeRoot.Q<Button>("StashSummaryButton");
        if (stashBtn != null) UiToolkitScavengerCursors.RegisterClickPointerHover(stashBtn);
        var quickBtn = uiTreeRoot.Q<Button>("QuickStatusButton");
        if (quickBtn != null) UiToolkitScavengerCursors.RegisterClickPointerHover(quickBtn);
        var mainMenuBtn = uiTreeRoot.Q<Button>("MainMenuButton");
        if (mainMenuBtn != null) UiToolkitScavengerCursors.RegisterClickPointerHover(mainMenuBtn);

        var learnBtn = uiTreeRoot.Q<Button>("LearnSkillButton");
        if (learnBtn != null) UiToolkitScavengerCursors.RegisterClickPointerHover(learnBtn);
    }

    private void Update()
    {
        if (!preparationOverlayOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pickMenuOverlay != null)
                DismissPickMenu();
            else
                HidePreparation();
        }
    }

    private void PopulateStubMissions()
    {
        for (int i = 0; i < 3; i++)
        {
            if (missionNames[i] != null) missionNames[i].text = StubMissions[i].name;
            if (missionTags[i] != null) missionTags[i].text = StubMissions[i].tag;
            if (missionRewards[i] != null) missionRewards[i].text = StubMissions[i].reward;
        }

        SelectMission(0);
    }

    private void SelectMission(int index)
    {
        if (index < 0 || index >= missionCards.Length) return;
        selectedMissionIndex = index;
        for (int i = 0; i < missionCards.Length; i++)
        {
            if (missionCards[i] == null) continue;
            if (i == index)
                missionCards[i].AddToClassList("home-base-mission-card--selected");
            else
                missionCards[i].RemoveFromClassList("home-base-mission-card--selected");
        }

        if (missionDetailLabel != null)
            missionDetailLabel.text = StubMissions[index].detail;

        if (confirmWeekButton != null)
            confirmWeekButton.SetEnabled(true);
    }

    private void OnExcursionSquadSlotClicked(int squadSlotIndex)
    {
        int idx = PlayerParty.GetExcursionSlotPartyIndex(squadSlotIndex);
        if (idx < 0) return;
        SelectRoster(idx);
        ShowPreparation();
    }

    private void SelectRoster(int partyIndex)
    {
        var party = PlayerParty.partyMembers;
        if (party == null || partyIndex < 0 || partyIndex >= party.Count)
            return;
        selectedRosterIndex = partyIndex;
        RefreshRosterSelectionVisual();
        loadoutPresenter?.SetCharacter(party[partyIndex]);
    }

    private void RefreshRosterSelectionVisual()
    {
        if (rosterChips != null)
        {
            const string prefix = "HomeRosterRow_";
            for (int i = 0; i < rosterChips.childCount; i++)
            {
                var row = rosterChips[i] as VisualElement;
                if (row == null || row.name == null || !row.name.StartsWith(prefix)) continue;
                if (!int.TryParse(row.name.Substring(prefix.Length), out int partyIdx)) continue;
                row.EnableInClassList("home-base-roster-row--selected", partyIdx == selectedRosterIndex);
            }
        }

        for (int i = 0; i < MaxSquadSlots; i++)
        {
            if (squadSlotRoots[i] == null) continue;
            int inSlot = PlayerParty.GetExcursionSlotPartyIndex(i);
            bool selected = inSlot >= 0 && inSlot == selectedRosterIndex;
            squadSlotRoots[i].EnableInClassList("home-base-squad-slot--selected", selected);
        }
    }

    private void EnsureLoadoutPresenter()
    {
        if (loadoutPresenter != null) return;
        if (preparationOverlay == null) return;
        if (CombatLoadoutTemplate == null || StashPanelTemplate == null)
        {
            Debug.LogError("HomeBaseUI: Assign CombatLoadoutTemplate (CombatPanel.uxml) and StashPanelTemplate on HomeBaseUI.");
            return;
        }

        var stashCol = preparationOverlay.Q<VisualElement>("StashColumn");
        var charCol = preparationOverlay.Q<VisualElement>("CharacterLoadoutColumn");
        if (stashCol == null || charCol == null)
        {
            Debug.LogError("HomeBaseUI: Preparation overlay missing StashColumn or CharacterLoadoutColumn.");
            return;
        }

        loadoutPresenter = new HomeBaseLoadoutPresenter(
            preparationOverlay,
            stashCol,
            charCol,
            CombatLoadoutTemplate,
            StashPanelTemplate,
            CombatLoadoutStyles,
            ThemeStyles);

        var party = PlayerParty.partyMembers;
        if (party != null && party.Count > 0)
        {
            selectedRosterIndex = Mathf.Clamp(selectedRosterIndex, 0, party.Count - 1);
            loadoutPresenter.SetCharacter(party[selectedRosterIndex]);
        }
    }

    private void RefreshAll()
    {
        if (weekValueLabel != null)
            weekValueLabel.text = CampaignMeta.CurrentWeek.ToString();
        if (goldValueLabel != null)
            goldValueLabel.text = CampaignMeta.Gold.ToString();

        var party = PlayerParty.partyMembers;
        int n = party != null ? party.Count : 0;
        if (rosterCountLabel != null)
            rosterCountLabel.text = $"{n} / {MaxRosterCapacity}";

        if (recruitButton != null)
            recruitButton.SetEnabled(n < MaxRosterCapacity);

        if (party != null && party.Count > 0)
            selectedRosterIndex = Mathf.Clamp(selectedRosterIndex, 0, party.Count - 1);

        if (excursionTitleLabel != null)
        {
            excursionTitleLabel.text =
                "Excursion squad (order = spawn) — empty: ▼ bench pick or drag roster here; filled: − benches; click name for loadout.";
        }

        RefreshSquadSlots(party);
        RefreshRosterRows(party);
        RefreshAssignments(party);
        RefreshRosterSelectionVisual();
        loadoutPresenter?.SetCharacter(party != null && party.Count > 0 ? party[selectedRosterIndex] : null);
    }

    private void RefreshSquadSlots(List<CharacterSheet> party)
    {
        for (int i = 0; i < MaxSquadSlots; i++)
        {
            if (squadSlotLabels[i] == null) continue;
            int pIdx = PlayerParty.GetExcursionSlotPartyIndex(i);
            bool filled = party != null && pIdx >= 0 && pIdx < party.Count;

            if (filled)
            {
                var sheet = party[pIdx];
                squadSlotLabels[i].text = sheet.DisplayName();
                if (squadSlotHints[i] != null)
                {
                    squadSlotHints[i].style.display = DisplayStyle.None;
                }
            }
            else
            {
                squadSlotLabels[i].text = "Empty";
                if (squadSlotHints[i] != null)
                {
                    squadSlotHints[i].style.display = DisplayStyle.Flex;
                    squadSlotHints[i].text = "drop";
                }
            }

            if (squadSlotRemoves[i] != null)
                squadSlotRemoves[i].style.display = filled ? DisplayStyle.Flex : DisplayStyle.None;
            if (squadSlotPicks[i] != null)
                squadSlotPicks[i].style.display = DisplayStyle.Flex;
        }
    }

    private void RefreshRosterRows(List<CharacterSheet> party)
    {
        if (rosterChips == null) return;
        rosterChips.Clear();
        if (party == null) return;

        for (int i = 0; i < party.Count; i++)
        {
            var row = new VisualElement { name = $"HomeRosterRow_{i}" };
            row.AddToClassList("home-base-roster-row");
            row.pickingMode = PickingMode.Position;
            row.focusable = true;

            GetWeeklyAssignmentGlyphAndDetail(i, out string glyph, out string detail);

            var icon = new Label(glyph);
            icon.AddToClassList("home-base-roster-job-icon");
            icon.pickingMode = PickingMode.Ignore;

            var nameLabel = new Label(party[i].DisplayName());
            nameLabel.AddToClassList("home-base-chip");
            nameLabel.focusable = true;
            nameLabel.pickingMode = PickingMode.Ignore;

            var jobLabel = new Label(detail);
            jobLabel.AddToClassList("home-base-detail");
            jobLabel.style.flexGrow = 1;
            jobLabel.style.fontSize = 11;
            jobLabel.pickingMode = PickingMode.Ignore;

            int partyIdx = i;
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                if (evt.target is Button) return;
                BeginRosterPointerPending(partyIdx, row, evt);
            });

            row.Add(icon);
            row.Add(nameLabel);
            row.Add(jobLabel);
            rosterChips.Add(row);
            UiToolkitScavengerCursors.RegisterDragAffordancePointerHover(row);
        }
    }

    private static void GetWeeklyAssignmentGlyphAndDetail(int partyIndex, out string glyph, out string detail)
    {
        for (int s = 0; s < MaxSquadSlots; s++)
        {
            if (PlayerParty.GetExcursionSlotPartyIndex(s) != partyIndex) continue;
            glyph = "⊕";
            detail = $"excursion · slot {s + 1}";
            return;
        }

        if (PlayerParty.weeklyBaseJobByPartyIndex != null &&
            PlayerParty.weeklyBaseJobByPartyIndex.TryGetValue(partyIndex, out var job) &&
            job != WeeklyBaseJobKind.None)
        {
            glyph = "⌂";
            detail = job.ToString().ToLowerInvariant();
            return;
        }

        glyph = "·";
        detail = "unassigned";
    }

    private void BeginRosterPointerPending(int partyIndex, VisualElement row, PointerDownEvent evt)
    {
        if (uiTreeRoot == null) return;
        if (dragPartyIndex >= 0 || pendingRosterPartyIndex >= 0) return;
        DismissPickMenu();
        pendingRosterPartyIndex = partyIndex;
        pendingRosterRow = row;
        pendingRosterStartPanelPos = evt.position;
        pendingRosterPointerId = evt.pointerId;
        uiTreeRoot.RegisterCallback(rosterInteractionMoveHandler, TrickleDown.TrickleDown);
        uiTreeRoot.RegisterCallback(rosterInteractionUpHandler, TrickleDown.TrickleDown);
    }

    private void CommitRosterDragFromPending(Vector2 panelPosition)
    {
        if (pendingRosterPartyIndex < 0 || pendingRosterRow == null || uiTreeRoot == null) return;

        int partyIndex = pendingRosterPartyIndex;
        dragSourceRow = pendingRosterRow;
        int pid = pendingRosterPointerId;
        pendingRosterPartyIndex = -1;
        pendingRosterRow = null;
        pendingRosterPointerId = -1;

        dragPartyIndex = partyIndex;
        dragSourceRow.AddToClassList("home-base-roster-row--dragging");

        var party = PlayerParty.partyMembers;
        string name = party != null && partyIndex >= 0 && partyIndex < party.Count
            ? party[partyIndex].DisplayName()
            : "?";
        dragGhost = new Label($" {name} ");
        dragGhost.AddToClassList("home-base-chip");
        dragGhost.style.position = Position.Absolute;
        dragGhost.pickingMode = PickingMode.Ignore;
        dragGhost.style.opacity = 0.92f;
        uiTreeRoot.Add(dragGhost);
        PositionDragGhost(panelPosition);

        dragPointerId = pid;
        dragPointerCaptureElement = dragSourceRow;
        dragSourceRow.RegisterCallback(rosterDragCapturedMoveHandler);
        dragSourceRow.RegisterCallback(rosterDragCapturedUpHandler);
        dragSourceRow.RegisterCallback(rosterDragCapturedCancelHandler);
        dragSourceRow.RegisterCallback(rosterDragCaptureOutHandler);
        dragSourceRow.CapturePointer(dragPointerId);
    }

    private void PositionDragGhost(Vector2 panelPosition)
    {
        if (dragGhost == null) return;
        const float ox = 12f;
        const float oy = 12f;
        dragGhost.style.left = panelPosition.x + ox;
        dragGhost.style.top = panelPosition.y + oy;
    }

    private void OnRosterDragCapturedMove(PointerMoveEvent evt)
    {
        if (dragPartyIndex < 0) return;
        if (!RosterPointerIdsMatch(evt.pointerId, dragPointerId)) return;
        PositionDragGhost(evt.position);
        SetDragHoverSlot(PickSquadSlotIndexAtPanelPos(evt.position));
    }

    private void OnRosterDragCapturedUp(PointerUpEvent evt)
    {
        if (dragPartyIndex < 0) return;
        if (!RosterPointerIdsMatch(evt.pointerId, dragPointerId)) return;
        int slot = PickSquadSlotIndexAtPanelPos(evt.position);
        if (slot >= 0)
        {
            bool ok = PlayerParty.TryAssignExcursionSlot(slot, dragPartyIndex);
            if (!ok)
                Debug.Log("Home Base: cannot assign to that excursion slot (slot rules).");
        }
        EndRosterDrag();
    }

    private void OnRosterDragCapturedCancel(PointerCancelEvent evt)
    {
        if (dragPartyIndex < 0) return;
        if (!RosterPointerIdsMatch(evt.pointerId, dragPointerId)) return;
        EndRosterDrag();
    }

    private void OnRosterDragPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (dragPartyIndex < 0) return;
        if (!RosterPointerIdsMatch(evt.pointerId, dragPointerId)) return;
        EndRosterDrag();
    }

    private void OnRosterInteractionMove(PointerMoveEvent evt)
    {
        if (dragPartyIndex >= 0)
        {
            if (!RosterPointerIdsMatch(evt.pointerId, dragPointerId)) return;
            PositionDragGhost(evt.position);
            SetDragHoverSlot(PickSquadSlotIndexAtPanelPos(evt.position));
            return;
        }

        if (pendingRosterPartyIndex < 0) return;
        if (!RosterPointerIdsMatch(evt.pointerId, pendingRosterPointerId)) return;
        float dist = Vector2.Distance(evt.position, pendingRosterStartPanelPos);
        if (dist >= RosterDragThresholdPx)
            CommitRosterDragFromPending(evt.position);
    }

    private void OnRosterInteractionUp(PointerUpEvent evt)
    {
        if (dragPartyIndex >= 0)
        {
            if (!RosterPointerIdsMatch(evt.pointerId, dragPointerId)) return;
            int slot = PickSquadSlotIndexAtPanelPos(evt.position);
            if (slot >= 0)
            {
                bool ok = PlayerParty.TryAssignExcursionSlot(slot, dragPartyIndex);
                if (!ok)
                    Debug.Log("Home Base: cannot assign to that excursion slot (slot rules).");
            }

            EndRosterDrag();
            return;
        }

        if (pendingRosterPartyIndex >= 0)
        {
            if (RosterPointerIdsMatch(evt.pointerId, pendingRosterPointerId))
            {
                SelectRoster(pendingRosterPartyIndex);
                pendingRosterPartyIndex = -1;
                pendingRosterRow = null;
                pendingRosterPointerId = -1;
                UnregisterRosterInteractionHandlers();
            }
        }
    }

    private void UnregisterRosterInteractionHandlers()
    {
        if (uiTreeRoot == null) return;
        uiTreeRoot.UnregisterCallback(rosterInteractionMoveHandler);
        uiTreeRoot.UnregisterCallback(rosterInteractionUpHandler);
    }

    private int PickSquadSlotIndexAtPanelPos(Vector2 panelPosition)
    {
        if (uiTreeRoot?.panel == null) return -1;
        var picked = uiTreeRoot.panel.Pick(panelPosition);
        return FindSquadSlotIndexFromAncestor(picked);
    }

    private int FindSquadSlotIndexFromAncestor(VisualElement ve)
    {
        while (ve != null)
        {
            for (int i = 0; i < MaxSquadSlots; i++)
            {
                if (squadSlotRoots[i] != null && ve == squadSlotRoots[i])
                    return i;
            }
            ve = ve.parent;
        }
        return -1;
    }

    private void SetDragHoverSlot(int slotIndex)
    {
        if (dragHoverSlot == slotIndex) return;
        if (dragHoverSlot >= 0 && dragHoverSlot < MaxSquadSlots && squadSlotRoots[dragHoverSlot] != null)
            squadSlotRoots[dragHoverSlot].RemoveFromClassList("home-base-squad-slot--drop-hover");
        dragHoverSlot = slotIndex;
        if (dragHoverSlot >= 0 && dragHoverSlot < MaxSquadSlots && squadSlotRoots[dragHoverSlot] != null)
            squadSlotRoots[dragHoverSlot].AddToClassList("home-base-squad-slot--drop-hover");
    }

    private void EndRosterDrag()
    {
        if (dragPartyIndex < 0 && dragGhost == null)
            return;

        var row = dragSourceRow;
        if (row != null && rosterDragCaptureOutHandler != null)
            row.UnregisterCallback(rosterDragCaptureOutHandler);
        if (row != null && rosterDragCapturedMoveHandler != null)
            row.UnregisterCallback(rosterDragCapturedMoveHandler);
        if (row != null && rosterDragCapturedUpHandler != null)
            row.UnregisterCallback(rosterDragCapturedUpHandler);
        if (row != null && rosterDragCapturedCancelHandler != null)
            row.UnregisterCallback(rosterDragCapturedCancelHandler);

        if (dragPointerCaptureElement != null && dragPointerId >= 0)
        {
            dragPointerCaptureElement.ReleasePointer(dragPointerId);
            dragPointerCaptureElement = null;
            dragPointerId = -1;
        }

        UnregisterRosterInteractionHandlers();

        SetDragHoverSlot(-1);
        if (row != null)
            row.RemoveFromClassList("home-base-roster-row--dragging");
        dragSourceRow = null;

        if (dragGhost != null)
        {
            dragGhost.RemoveFromHierarchy();
            dragGhost = null;
        }

        dragPartyIndex = -1;
        RefreshAll();
    }

    private void ShowExcursionPickMenu(int slotIndex)
    {
        DismissPickMenu();
        pickMenuSlot = slotIndex;

        var party = PlayerParty.partyMembers;
        if (party == null) return;

        var candidates = new List<int>();
        for (int i = 0; i < party.Count; i++)
        {
            if (PlayerParty.IsEligibleForExcursionDropdown(i) &&
                PlayerParty.CanAssignExcursionSlot(party[i], slotIndex))
                candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            Debug.Log("Home Base: no unassigned heroes available for this slot.");
            pickMenuSlot = -1;
            return;
        }

        pickMenuOverlay = new VisualElement();
        pickMenuOverlay.style.position = Position.Absolute;
        pickMenuOverlay.style.left = 0;
        pickMenuOverlay.style.right = 0;
        pickMenuOverlay.style.top = 0;
        pickMenuOverlay.style.bottom = 0;
        pickMenuOverlay.pickingMode = PickingMode.Position;
        pickMenuOverlay.style.backgroundColor = new Color(0, 0, 0, 0.35f);

        var panel = new VisualElement();
        panel.AddToClassList("home-base-squad-pick-popup");
        panel.style.position = Position.Absolute;
        panel.style.backgroundColor = new StyleColor(new Color(0.12f, 0.14f, 0.18f, 0.98f));
        panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius =
            panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 6;
        panel.style.borderLeftWidth = panel.style.borderRightWidth =
            panel.style.borderTopWidth = panel.style.borderBottomWidth = 1;
        panel.style.borderLeftColor = panel.style.borderRightColor =
            panel.style.borderTopColor = panel.style.borderBottomColor =
                new Color(0.4f, 0.45f, 0.55f, 0.6f);
        panel.style.paddingLeft = panel.style.paddingRight = 8;
        panel.style.paddingTop = panel.style.paddingBottom = 8;
        panel.style.minWidth = 200;
        panel.style.flexDirection = FlexDirection.Column;

        var title = new Label("Assign to excursion");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 6;
        title.style.color = new Color(0.95f, 0.96f, 0.98f);
        panel.Add(title);

        foreach (var pi in candidates)
        {
            int idx = pi;
            var btn = new Button(() =>
            {
                PlayerParty.TryAssignExcursionSlot(slotIndex, idx);
                DismissPickMenu();
                RefreshAll();
            })
            {
                text = party[pi].DisplayName()
            };
            btn.style.marginBottom = 4;
            btn.AddToClassList("home-base-btn-secondary");
            UiToolkitScavengerCursors.RegisterClickPointerHover(btn);
            panel.Add(btn);
        }

        pickMenuOverlay.Add(panel);
        uiTreeRoot.Add(pickMenuOverlay);

        pickMenuOverlay.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == pickMenuOverlay)
                DismissPickMenu();
        });

        if (squadSlotRoots[slotIndex] != null)
        {
            var slotWorld = squadSlotRoots[slotIndex].worldBound;
            var rootWorld = uiTreeRoot.worldBound;
            float lx = slotWorld.xMin - rootWorld.xMin;
            float ty = slotWorld.yMax - rootWorld.yMin + 4f;
            panel.style.left = lx;
            panel.style.top = ty;
        }
    }

    private void DismissPickMenu()
    {
        if (pickMenuOverlay != null)
        {
            pickMenuOverlay.RemoveFromHierarchy();
            pickMenuOverlay = null;
        }
        pickMenuSlot = -1;
    }

    private void RefreshAssignments(List<CharacterSheet> party)
    {
        if (assignmentsList == null) return;
        assignmentsList.Clear();
        if (party == null) return;

        for (int i = 0; i < party.Count; i++)
        {
            GetWeeklyAssignmentGlyphAndDetail(i, out _, out string detail);
            if (PlayerParty.IsOnExcursionSquad(i))
                continue;
            var line = new Label($"{party[i].DisplayName()}: {detail} (assign room — stub)");
            line.AddToClassList("home-base-assignment-line");
            assignmentsList.Add(line);
        }

        if (assignmentsList.childCount == 0)
        {
            var line = new Label("No off-squad base jobs assigned (stub — hook workbench / barracks here).");
            line.AddToClassList("home-base-assignment-line");
            assignmentsList.Add(line);
        }
    }

    private void ShowPreparation()
    {
        EnsureLoadoutPresenter();
        var party = PlayerParty.partyMembers;
        if (party != null && party.Count > 0)
        {
            selectedRosterIndex = Mathf.Clamp(selectedRosterIndex, 0, party.Count - 1);
            loadoutPresenter?.SetCharacter(party[selectedRosterIndex]);
        }
        RefreshRosterSelectionVisual();
        if (preparationOverlay != null)
            preparationOverlay.style.display = DisplayStyle.Flex;
        preparationOverlayOpen = true;
    }

    private void HidePreparation()
    {
        if (preparationOverlay != null)
            preparationOverlay.style.display = DisplayStyle.None;
        preparationOverlayOpen = false;
    }

    private void OnRecruitClicked()
    {
        Debug.Log("Recruit hero (stub — hook barracks / roster capacity later).");
    }

    private void OnConfirmWeekClicked()
    {
        if (selectedMissionIndex < 0)
            return;
        PlayerParty.SanitizeExcursionSquad();
        SceneManager.LoadScene(SceneNames.Combat);
    }
}
