
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public partial class CombatController : MonoBehaviour
{
    public CharacterSheet characterSheet = null;
    public bool isTurn = false;
    public bool isActing = false;
    protected List<Tile> selectableTiles = new List<Tile>();
    protected TileManager manager;
    protected bool hasMoved = false;
    private Tile currentTile;
    private IlluminationSource equippedLight = null;

    private ActionSelector actionSelector;

    // --- Lifecycle ---

    virtual protected void Start()
    {
        manager = FindFirstObjectByType<TileManager>(FindObjectsInactive.Exclude);
        actionSelector = new ActionSelector(gameObject, () => characterSheet);
        actionSelector.GetOrAddByType(typeof(ActionMeleeAttack));
        actionSelector.ResetToDefault();
        if (characterSheet != null)
            characterSheet.OnEquipmentChanged += OnEquipmentChanged;
    }

    public void SetCharacterSheet(CharacterSheet c)
    {
        if (characterSheet != null)
            characterSheet.OnEquipmentChanged -= OnEquipmentChanged;
        characterSheet = c;
        characterSheet.OnEquipmentChanged += OnEquipmentChanged;
        actionSelector?.ResetToDefault();
    }

    // --- Turn lifecycle ---

    public bool BeginTurn()
    {
        characterSheet.BeginTurn();
        if (Dead()) return false;
        isTurn = true;
        hasMoved = false;
        FindSelectableTiles();
        return true;
    }

    protected void EndTurn()
    {
        isTurn = false;
    }

    public void BeginAction()
    {
        isActing = true;
    }

    public void EndAction()
    {
        isActing = false;
        manager.ResetTileSearch();
        FindSelectableTiles();
    }

    // --- Tile / position ---

    public void SetCurrentTile(Tile t)
    {
        if (currentTile != null)
            currentTile.occupant = null;
        t.occupant = this;
        currentTile = t;
    }

    public Tile GetCurrentTile()
    {
        return currentTile;
    }

    public Vector2 GetFacingDirection()
    {
        return transform.up;
    }

    // --- Identity (overridden by PC/Enemy subclasses) ---

    virtual public bool IsPC() => false;
    virtual public bool IsEnemy() => false;
    virtual public bool ContainsEnemy(Tile tile) => tile.occupant != null;
    virtual protected bool ContainsAlly(Tile tile) => tile.occupant != null;
    virtual protected bool HasEnemy(Tile t) => false;
    virtual protected bool DoesGUI() => false;
    virtual protected bool IsTileVisibleByCurrentActor(Tile tile) => true;

    // --- Death ---

    public void Die()
    {
        StartCoroutine(DieAfterDelay(0.8f));
    }

    private IEnumerator DieAfterDelay(float fDuration)
    {
        yield return new WaitForSeconds(fDuration);
        var ac = GetComponent<AvatarController>();
        if (ac != null) ac.DestroyAvatar();
        else Destroy(gameObject);
    }

    public bool Dead() => characterSheet.dead;

    // --- Status effects / display ---

    public void DisplayPopupDuringCombat(string message)
    {
        Debug.Log($"{characterSheet.firstName}: {message}");
    }

    public void NotifyStatusEffectChanged(StatusEffect.EffectType effectType)
    {
        if (effectType == StatusEffect.EffectType.HIDDEN)
            StartCoroutine(NotifyVisionNextFrame());
    }

    private IEnumerator NotifyVisionNextFrame()
    {
        yield return null;
        VisionSystem visionSystem = FindFirstObjectByType<VisionSystem>(FindObjectsInactive.Exclude);
        if (visionSystem != null)
            visionSystem.CheckForHiddenStatusChanges();
    }

    // --- Equipment effects ---

    public void ApplyEquipmentEffects()
    {
        UpdateEquippedLight();
    }

    private void OnEquipmentChanged()
    {
        UpdateEquippedLight();
        actionSelector?.EnsureStillValid();
        if (isTurn && !isActing)
            FindSelectableTiles();
    }

    private void UpdateEquippedLight()
    {
        if (characterSheet == null || characterSheet.avatar == null)
        {
            if (equippedLight != null)
            {
                Destroy(equippedLight.gameObject);
                equippedLight = null;
            }
            return;
        }

        var equipped = characterSheet.GetEquippedItems();
        EquippableHandheld lightItem = null;
        foreach (var kv in equipped)
        {
            if (kv.Value is EquippableHandheld h && h.providesIllumination)
            {
                lightItem = h;
                break;
            }
        }

        if (lightItem != null)
        {
            if (equippedLight == null)
            {
                GameObject lightObj = new GameObject($"EquippedLight_{characterSheet.firstName}");
                lightObj.transform.SetParent(characterSheet.avatar.transform);
                lightObj.transform.localPosition = Vector3.zero;
                equippedLight = lightObj.AddComponent<IlluminationSource>();
                equippedLight.isMovable = true;
            }
            equippedLight.illuminationRange = lightItem.illuminationRange;
            equippedLight.SetActive(true);
        }
        else if (equippedLight != null)
        {
            Destroy(equippedLight.gameObject);
            equippedLight = null;
        }
    }

    // --- Action selection (delegates to ActionSelector) ---

    protected Action selectedAction
    {
        get => actionSelector?.SelectedAction;
        set => actionSelector?.SetAction(value);
    }

    public Action GetSelectedAction() => actionSelector?.SelectedAction;
    public string GetSelectedActionKey() => actionSelector?.SelectedActionKey;
    public string GetSelectedActionClassName() => actionSelector?.GetSelectedActionClassName();
    public string GetDefaultActionClassName() => actionSelector?.GetDefaultActionClassName() ?? nameof(ActionMeleeAttack);

    public void ResetSelectedActionToDefault()
    {
        actionSelector?.ResetToDefault();
        FindSelectableTiles();
    }

    public void SelectActionByType(Type t)
    {
        actionSelector?.SelectByType(t);
        FindSelectableTiles();
    }

    public void SelectActionByName(string className)
    {
        actionSelector?.SelectByName(className);
        FindSelectableTiles();
    }

    public void SelectAction(string key, string className)
    {
        actionSelector?.Select(key, className);
        FindSelectableTiles();
    }

    public void SelectActionSilent(string key, string className)
    {
        actionSelector?.SelectSilent(key, className);
    }

    public Action GetOrAddActionByType(Type t) => actionSelector?.GetOrAddByType(t);
    public Action GetOrAddActionByName(string className) => actionSelector?.GetOrAddByName(className);

    // --- Tile search (delegates to ReachabilityResolver) ---

    protected void FindSelectableTiles()
    {
        selectableTiles.Clear();
        if (currentTile == null || characterSheet == null || actionSelector == null) return;
        selectableTiles = ReachabilityResolver.FindSelectableTiles(
            currentTile,
            characterSheet.currentActionPoints,
            actionSelector.SelectedAction,
            manager,
            IsPC(),
            IsTileVisibleByCurrentActor);
    }

    protected bool IsTileInRange(Tile fromTile, int minRange, int maxRange, bool requiresLineOfSight, int x, int y)
    {
        return ReachabilityResolver.IsTileInRange(fromTile, minRange, maxRange, requiresLineOfSight, x, y, manager);
    }

    // Legacy wrappers kept for subclass compatibility
    protected void FindSelectableBasicTiles() => FindSelectableTiles();
    protected void FindSelectableChargeTiles() => FindSelectableTiles();
    protected void FindSelectableMeleeAttackTiles() => FindSelectableTiles();
    protected void FindSelectableAllyBuffTiles() => FindSelectableTiles();
    protected void FindSelectableMeleeReachAttackTiles() => FindSelectableTiles();
    protected void FindSelectableRangedAttackTiles() => FindSelectableTiles();
    protected void FindSelectableGroundAttackTiles() => FindSelectableTiles();
}
