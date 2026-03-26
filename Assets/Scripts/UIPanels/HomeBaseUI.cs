using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Weekly command screen for <see cref="SceneNames.HomeBase"/>. Layout follows the design wireframe (missions, squad, roster, base assignments, preparation modal).
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
    private readonly VisualElement[] squadSlotRoots = new VisualElement[4];
    private VisualElement rosterChips;
    private VisualElement assignmentsList;
    private Label excursionTitleLabel;
    private Button recruitButton;

    private int selectedMissionIndex = -1;
    private int selectedRosterIndex;

    private HomeBaseLoadoutPresenter loadoutPresenter;
    private bool preparationOverlayOpen;

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
    }

    private void Start()
    {
        PlayerParty.EnsureInitialized();

        if (HomeBaseTemplate == null)
        {
            Debug.LogError("HomeBaseUI: HomeBaseTemplate is not assigned.");
            return;
        }

        VisualElement tree = HomeBaseTemplate.CloneTree();
        tree.style.flexGrow = 1;
        tree.style.width = Length.Percent(100);
        tree.style.height = Length.Percent(100);

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
            squadSlotRoots[i] = tree.Q<VisualElement>($"SquadSlot{i}");
            int idx = i;
            if (squadSlotRoots[i] != null)
            {
                squadSlotRoots[i].RegisterCallback<ClickEvent>(_ => OnExcursionSquadSlotClicked(idx));
                squadSlotRoots[i].focusable = true;
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

        var party = PlayerParty.partyMembers;
        if (party != null && party.Count > 0)
            selectedRosterIndex = Mathf.Clamp(selectedRosterIndex, 0, party.Count - 1);
        else
            selectedRosterIndex = 0;

        PopulateStubMissions();
        RefreshAll();
    }

    private void Update()
    {
        if (!preparationOverlayOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape))
            HidePreparation();
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
        var squad = PlayerParty.excursionSquadIndices;
        var party = PlayerParty.partyMembers;
        if (squad == null || party == null || squadSlotIndex < 0 || squadSlotIndex >= squad.Count)
            return;
        SelectRoster(squad[squadSlotIndex]);
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
            for (int i = 0; i < rosterChips.childCount; i++)
            {
                var row = rosterChips[i];
                if (row == null || row.childCount == 0) continue;
                var nameLabel = row[0] as Label;
                if (nameLabel == null) continue;
                if (i == selectedRosterIndex)
                    nameLabel.AddToClassList("home-base-chip--selected");
                else
                    nameLabel.RemoveFromClassList("home-base-chip--selected");
            }
        }

        var squad = PlayerParty.excursionSquadIndices;
        for (int i = 0; i < MaxSquadSlots; i++)
        {
            if (squadSlotRoots[i] == null) continue;
            bool selected = squad != null && i < squad.Count && squad[i] == selectedRosterIndex;
            if (selected)
                squadSlotRoots[i].AddToClassList("home-base-squad-slot--selected");
            else
                squadSlotRoots[i].RemoveFromClassList("home-base-squad-slot--selected");
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
            excursionTitleLabel.text =
                "Excursion squad (max 4) — toggle Deploy on roster members; click a filled slot to edit loadout.";

        RefreshSquadSlots(party);
        RefreshRosterChips(party);
        RefreshAssignments(party);
        RefreshRosterSelectionVisual();
        loadoutPresenter?.SetCharacter(party != null && party.Count > 0 ? party[selectedRosterIndex] : null);
    }

    private void RefreshSquadSlots(List<CharacterSheet> party)
    {
        var squad = PlayerParty.excursionSquadIndices;
        for (int i = 0; i < MaxSquadSlots; i++)
        {
            if (squadSlotLabels[i] == null) continue;
            if (party != null && squad != null && i < squad.Count)
            {
                int idx = squad[i];
                if (idx >= 0 && idx < party.Count)
                    squadSlotLabels[i].text = party[idx].DisplayName();
                else
                    squadSlotLabels[i].text = "—";
            }
            else
                squadSlotLabels[i].text = "—";
        }
    }

    private void RefreshRosterChips(List<CharacterSheet> party)
    {
        if (rosterChips == null) return;
        rosterChips.Clear();
        if (party == null) return;

        for (int i = 0; i < party.Count; i++)
        {
            var row = new VisualElement();
            row.AddToClassList("home-base-roster-row");

            var nameLabel = new Label(party[i].DisplayName());
            nameLabel.AddToClassList("home-base-chip");
            nameLabel.focusable = true;
            nameLabel.pickingMode = PickingMode.Position;
            if (PlayerParty.IsOnExcursionSquad(i))
                nameLabel.AddToClassList("home-base-chip--in-squad");
            int partyIdx = i;
            nameLabel.RegisterCallback<ClickEvent>(_ => SelectRoster(partyIdx));

            var deployToggle = new Toggle("Deploy") { value = PlayerParty.IsOnExcursionSquad(i) };
            deployToggle.AddToClassList("home-base-roster-deploy-toggle");
            deployToggle.RegisterValueChangedCallback(evt =>
            {
                if (!PlayerParty.TrySetExcursionSquad(partyIdx, evt.newValue))
                    deployToggle.SetValueWithoutNotify(!evt.newValue);
                RefreshAll();
            });

            row.Add(nameLabel);
            row.Add(deployToggle);
            rosterChips.Add(row);
        }
    }

    private void RefreshAssignments(List<CharacterSheet> party)
    {
        if (assignmentsList == null) return;
        assignmentsList.Clear();
        if (party == null) return;

        for (int i = MaxSquadSlots; i < party.Count; i++)
        {
            var line = new Label($"{party[i].DisplayName()}: Workbench / barracks / altar (assign room — stub)");
            line.AddToClassList("home-base-assignment-line");
            assignmentsList.Add(line);
        }

        if (party.Count <= MaxSquadSlots)
        {
            var line = new Label("All roster members are on the excursion squad. No base jobs this week.");
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
