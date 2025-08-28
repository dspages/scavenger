using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

public partial class CombatController : MonoBehaviour
{
    public CharacterSheet characterSheet = null;
    public bool isTurn = false;
    public bool isActing = false;
    protected Action selectedAction = null;
    // Distinguishes between actions that share a class (e.g., left/right hand)
    private string selectedActionKey = null;
    protected List<Tile> selectableTiles = new List<Tile>();
    protected TileManager manager;

    protected bool hasMoved = false;
    private Tile currentTile;

    // Use this for initialization
    virtual protected void Start()
    {
        manager = FindObjectOfType<TileManager>();
        // Ensure a baseline attack action exists
        GetOrAddActionByType(typeof(ActionWeaponAttack));
        ResetSelectedActionToDefault();
        // Setup illumination handling for equipped items
        if (characterSheet != null)
        {
            characterSheet.OnEquipmentChanged += UpdateEquippedLight;
        }
    }

    virtual public bool IsPC()
    {
        return false;
    }

    virtual public bool IsEnemy()
    {
        return false;
    }

    // Checks to see if the input tile contains an enemy of this.
    // Defaults to false, but can be overridden by subclasses.
    // Note that 'enemy' is from the perspective of the actor;
    // for player-controlled, enemies are AI and vice versa.
    virtual protected bool ContainsEnemy(Tile tile)
    {
        if (tile.occupant == null) return false;
        
        // Check if the occupant is an enemy
        bool isEnemy = tile.occupant.IsEnemy();
        if (!isEnemy) return false;
        
        // Check if the enemy is visible according to vision system
        VisionSystem visionSystem = FindObjectOfType<VisionSystem>();
        if (visionSystem != null)
        {
            return visionSystem.CanSeeUnit(this, tile.occupant);
        }
        
        return true; // Fallback if no vision system
    }

    public void SetCharacterSheet(CharacterSheet c)
    {
        // Unsubscribe previous
        if (characterSheet != null)
        {
            characterSheet.OnEquipmentChanged -= UpdateEquippedLight;
        }
        characterSheet = c;
        characterSheet.OnEquipmentChanged += UpdateEquippedLight;
        // Make sure default action aligns with equipment
        ResetSelectedActionToDefault();
    }

    private IlluminationSource equippedLight = null;

    private void UpdateEquippedLight()
    {
        // Remove existing light if any
        if (characterSheet == null || characterSheet.avatar == null)
        {
            if (equippedLight != null)
            {
                Destroy(equippedLight.gameObject);
                equippedLight = null;
            }
            return;
        }

        // Find any equipped handheld that provides illumination
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
        else
        {
            if (equippedLight != null)
            {
                Destroy(equippedLight.gameObject);
                equippedLight = null;
            }
        }

        // After any equipment change, ensure the selected action is still valid
        EnsureSelectedActionStillValid();
    }

    // Public entry to apply equipment-driven effects (used by VisionSystem after initialization)
    public void ApplyEquipmentEffects()
    {
        UpdateEquippedLight();
    }

    public void Die()
    {
        StartCoroutine(DieAfterDelay(0.8f));
    }

    private IEnumerator DieAfterDelay(float fDuration)
    {
        yield return new WaitForSeconds(fDuration);
        Destroy(characterSheet.avatar);
        yield break;
    }

    public bool Dead()
    {
        return characterSheet.dead;
    }
    
    public void DisplayPopupDuringCombat(string message)
    {
        // This can be overridden by subclasses to show UI messages
        Debug.Log($"{characterSheet.firstName}: {message}");
    }
    
    public void NotifyStatusEffectChanged(StatusEffect.EffectType effectType)
    {
        // Notify vision system when HIDDEN status effects change
        if (effectType == StatusEffect.EffectType.HIDDEN)
        {
            // Delay the vision update to the next frame to avoid recursive updates
            StartCoroutine(NotifyVisionNextFrame());
        }
    }

    private IEnumerator NotifyVisionNextFrame()
    {
        yield return null; // wait a frame to let current update cycles finish
        VisionSystem visionSystem = FindObjectOfType<VisionSystem>();
        if (visionSystem != null)
        {
            visionSystem.CheckForHiddenStatusChanges();
        }
    }

    public bool BeginTurn()
    {
        characterSheet.BeginTurn();
        if (Dead()) return false;
        if (DoesGUI())
        {
            // characterSheet.UpdateUI();
        }
        isTurn = true;
        hasMoved = false;
        FindSelectableBasicTiles();
        return true;
    }

    // Defaults to false, but can be overridden by subclasses.
    // If true, the unit is interactable via the GUI.
    virtual protected bool DoesGUI()
    {
        return false;
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
        FindSelectableBasicTiles();
        
        // Vision system updates are now handled by specific actions when needed
        // (e.g., ActionMove updates vision when tiles change, not after action completes)
    }

    public void SetCurrentTile(Tile t)
    {
        // Unoccupy old tile, if any
        if (currentTile != null)
        {
            currentTile.occupant = null;
        }
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

    virtual protected bool HasEnemy(Tile t)
    {
        return false;
    }

    protected void FindSelectableBasicTiles()
    {
        FindSelectableTiles();
    }

    private void FindSelectableTiles()
    {
        manager.ResetTileSearch();
        selectableTiles.Clear();

        // Unified targeting system that handles movement+attack combinations
        // Use priority queue for performance optimization
        List<Tile> queue = new List<Tile>();
        queue.Add(currentTile);
        currentTile.searchWasVisited = true;
        currentTile.searchDistance = 0;

        // Cache for range calculations to avoid duplicates
        Dictionary<Tile, HashSet<Tile>> rangeCache = new Dictionary<Tile, HashSet<Tile>>();

        while (queue.Count > 0)
        {
            queue.Sort((item1, item2) => item1.searchDistance.CompareTo(item2.searchDistance));
            Tile tile = queue[0];
            queue.RemoveAt(0);

            // Check for attacks from this position
            FindAttackTargetsFromTile(tile, rangeCache);

            // Expand movement options
            foreach (Tile adjacentTile in tile.Neighbors())
            {
                if (!adjacentTile.searchWasVisited)
                {
                    int newDistance = tile.searchDistance + adjacentTile.GetMoveCost();
                    if (adjacentTile.occupant == null && newDistance <= characterSheet.currentActionPoints)
                    {
                        AttachTile(adjacentTile, tile);
                        queue.Add(adjacentTile);
                    }
                }
            }
        }
    }

    // Find attack targets from a specific tile position
    private void FindAttackTargetsFromTile(Tile fromTile, Dictionary<Tile, HashSet<Tile>> rangeCache)
    {
        if (selectedAction == null) return;

        // Get range parameters based on action type
        int minRange, maxRange, actionCost;
        bool requiresLineOfSight;
        bool targetEnemiesOnly;
        GetActionRangeParameters(out minRange, out maxRange, out actionCost, out requiresLineOfSight, out targetEnemiesOnly);

        // Check if we have enough action points for this attack from this position
        int movementCost = fromTile.searchDistance;
        if (movementCost + actionCost > characterSheet.currentActionPoints)
        {
            return; // Not enough AP to move here and attack
        }

        // Use cached range calculation if available
        HashSet<Tile> targetsInRange;
        if (!rangeCache.TryGetValue(fromTile, out targetsInRange))
        {
            targetsInRange = FindTilesInRange(fromTile, minRange, maxRange, requiresLineOfSight);
            rangeCache[fromTile] = targetsInRange;
        }

        // Filter targets and attach tiles
        foreach (Tile targetTile in targetsInRange)
        {
            bool isValidTarget = false;
            
            if (selectedAction is ActionAttack attackAction && attackAction.CanTargetEmptyTiles)
            {
                // Ground attacks can target any tile
                isValidTarget = true;
            }
            else if (targetEnemiesOnly)
            {
                // Ranged/melee attacks target enemies only
                isValidTarget = ContainsEnemy(targetTile);
            }
            else
            {
                // Other actions might target any occupied tile
                isValidTarget = targetTile.occupant != null;
            }

            if (isValidTarget)
            {
                AttachTile(targetTile, fromTile, movementCost + actionCost);
            }
        }
    }

    private void AttachTile(Tile tile, Tile parent, int moveCostOverride = -1)
    {
        tile.searchParent = parent;
        selectableTiles.Add(tile);
        tile.searchCanBeChosen = true;
        tile.searchWasVisited = true;
        if (moveCostOverride != -1)
        {
            tile.searchDistance = moveCostOverride;
        }
        else
        {
            tile.searchDistance = tile.GetMoveCost() + parent.searchDistance;
        }
    }

    // Get range and cost parameters for the currently selected action
    private void GetActionRangeParameters(out int minRange, out int maxRange, out int actionCost, out bool requiresLineOfSight, out bool targetEnemiesOnly)
    {
        minRange = 1;
        maxRange = 1;
        actionCost = 0;
        requiresLineOfSight = false;
        targetEnemiesOnly = true;

        if (selectedAction == null) return;

        actionCost = selectedAction.ACTION_COST;

        if (selectedAction is ActionAttack attackAction)
        {
            minRange = attackAction.minRange;
            maxRange = attackAction.maxRange;
            requiresLineOfSight = attackAction.RequiresLineOfSight;
            targetEnemiesOnly = attackAction.TargetsEnemiesOnly;
        }
    }

    // Find all tiles within range from a given position
    private HashSet<Tile> FindTilesInRange(Tile fromTile, int minRange, int maxRange, bool requiresLineOfSight)
    {
        HashSet<Tile> tilesInRange = new HashSet<Tile>();
        Queue<Tile> queue = new Queue<Tile>();
        HashSet<Tile> visited = new HashSet<Tile>();
        Dictionary<Tile, int> distance = new Dictionary<Tile, int>();
        
        queue.Enqueue(fromTile);
        visited.Add(fromTile);
        distance[fromTile] = 0;

        while (queue.Count > 0)
        {
            Tile tile = queue.Dequeue();
            int dist = distance[tile];

            // If this tile is within range, check line of sight if required
            if (dist >= minRange && dist <= maxRange)
            {
                if (!requiresLineOfSight || LineOfSightUtils.HasLineOfSight(fromTile, tile, manager))
                {
                    tilesInRange.Add(tile);
                }
            }

            // Continue expanding to neighbors if we haven't reached max range
            if (dist < maxRange)
            {
                foreach (Tile neighbor in tile.Neighbors())
                {
                    if (neighbor != null && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        distance[neighbor] = dist + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return tilesInRange;
    }

    // Get the equipped weapon for the currently selected action
    private EquippableHandheld GetEquippedWeaponForSelectedAction()
    {
        if (characterSheet == null || string.IsNullOrEmpty(selectedActionKey)) return null;

        if (selectedActionKey.EndsWith(":RightHand"))
        {
            return characterSheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
        }
        else if (selectedActionKey.EndsWith(":LeftHand"))
        {
            return characterSheet.GetEquippedItem(EquippableItem.EquipmentSlot.LeftHand) as EquippableHandheld;
        }
        
        // Default to right hand
        return characterSheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
    }
}

// -------------------- Action Selection & Utilities --------------------
public partial class CombatController
{
	public Action GetSelectedAction()
	{
		return selectedAction;
	}

	public string GetSelectedActionKey()
	{
		return selectedActionKey;
	}

	public void ResetSelectedActionToDefault()
	{
		// Default is right-hand item associated action, or basic weapon attack
		string className = GetDefaultActionClassName();
		Action act = GetOrAddActionByName(className);
		if (act == null)
		{
			act = GetOrAddActionByType(typeof(ActionWeaponAttack));
		}
		selectedAction = act;
		// Prefer right hand for default if present
		var right = characterSheet != null ? characterSheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld : null;
		selectedActionKey = right != null ? ($"{className}:RightHand") : className;
		// Configure ground attack ranges from equipped item if applicable
		ConfigureSelectedActionFromEquippedItem();
	}

	public void SelectActionByType(Type t)
	{
		Action act = GetOrAddActionByType(t);
		if (act != null)
		{
			selectedAction = act;
			// Configure ground attack ranges from equipped item if applicable
			ConfigureSelectedActionFromEquippedItem();
		}
        FindSelectableTiles();
	}

	public void SelectActionByName(string className)
	{
		Action act = GetOrAddActionByName(className);
		if (act != null)
		{
			selectedAction = act;
			// Configure ground attack ranges from equipped item if applicable
			ConfigureSelectedActionFromEquippedItem();
		}
        FindSelectableTiles();
	}

	public string GetSelectedActionClassName()
	{
		return selectedAction != null ? selectedAction.GetType().Name : null;
	}

	public string GetDefaultActionClassName()
	{
		if (characterSheet == null) return nameof(ActionWeaponAttack);
		var right = characterSheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
		if (right != null && !string.IsNullOrEmpty(right.associatedActionClass))
		{
			return right.associatedActionClass;
		}
		return nameof(ActionWeaponAttack);
	}

	public Action GetOrAddActionByType(Type t)
	{
		if (t == null) return null;
		var existing = GetComponent(t) as Action;
		if (existing != null) return existing;
		var added = gameObject.AddComponent(t) as Action;
		return added;
	}

	public Action GetOrAddActionByName(string className)
	{
		if (string.IsNullOrEmpty(className)) return null;
		var t = FindTypeByName(className);
		var result = GetOrAddActionByType(t);
		return result;
	}

	private static Type FindTypeByName(string className)
	{
		// Search all loaded assemblies for a type with the given simple name
		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			var t = asm.GetTypes().FirstOrDefault(x => x.Name == className);
			if (t != null) return t;
		}
		return null;
	}

	// Overload that lets UI pass a stable key (e.g., hand-specific)
	public void SelectAction(string key, string className)
	{
		SelectActionByName(className);
		selectedActionKey = key;
		// Configure ground attack ranges from equipped item if applicable
		ConfigureSelectedActionFromEquippedItem();
        FindSelectableTiles();
	}
}

// -------------------- Action Configuration Helpers --------------------
public partial class CombatController
{
	// Configure the selected Action using min/max range from equipped item
	private void ConfigureSelectedActionFromEquippedItem()
	{
		if (characterSheet == null || selectedAction == null) return;

		EquippableHandheld equipped = GetEquippedWeaponForSelectedAction();
		if (equipped == null) return;

		string itemActionClass = string.IsNullOrEmpty(equipped.associatedActionClass) ? nameof(ActionWeaponAttack) : equipped.associatedActionClass;
		
		// Only configure when the selected action class matches the item's associated action
		if (itemActionClass == selectedAction.GetType().Name)
		{
			// All attack actions inherit from ActionAttack and share the same configuration
			if (selectedAction is ActionAttack attackAction)
			{
				attackAction.minRange = equipped.minRange;
				attackAction.maxRange = equipped.maxRange;
				attackAction.actionDisplayName = equipped.itemName;
				attackAction.baseDamage = equipped.damage;
				attackAction.actionPointCost = equipped.actionPointCost;
			}
		}
	}
}

// Selection validation helpers (main class scope for equipment change handling)
public partial class CombatController
{
    private void EnsureSelectedActionStillValid()
    {
        if (characterSheet == null) return;

        // Determine which hand (if any) the current selection depends on
        bool dependsOnRight = !string.IsNullOrEmpty(selectedActionKey) && selectedActionKey.EndsWith(":RightHand");
        bool dependsOnLeft = !string.IsNullOrEmpty(selectedActionKey) && selectedActionKey.EndsWith(":LeftHand");
        bool isPunchSelected = string.IsNullOrEmpty(selectedActionKey) || selectedActionKey == nameof(ActionWeaponAttack);

        var right = characterSheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
        var left = characterSheet.GetEquippedItem(EquippableItem.EquipmentSlot.LeftHand) as EquippableHandheld;

        // Auto-select logic:
        // 1. If current selection depends on a hand that became empty, switch
        // 2. If punch is selected but we have weapons, prefer a weapon
        // 3. Always ensure some valid selection exists
        bool needsReselection = false;
        
        if ((dependsOnRight && right == null) || (dependsOnLeft && left == null))
        {
            needsReselection = true;
        }
        else if (isPunchSelected && (right != null || left != null))
        {
            needsReselection = true;
        }
        else if (string.IsNullOrEmpty(selectedActionKey))
        {
            needsReselection = true;
        }
        
        if (needsReselection)
        {
            // Priority: right hand, then left hand, then punch
            if (right != null)
            {
                string cls = string.IsNullOrEmpty(right.associatedActionClass) ? nameof(ActionWeaponAttack) : right.associatedActionClass;
                SelectAction($"{cls}:RightHand", cls);
            }
            else if (left != null)
            {
                string cls = string.IsNullOrEmpty(left.associatedActionClass) ? nameof(ActionWeaponAttack) : left.associatedActionClass;
                SelectAction($"{cls}:LeftHand", cls);
            }
            else
            {
                // No weapons – default to punch
                SelectAction(nameof(ActionWeaponAttack), nameof(ActionWeaponAttack));
            }
        }
    }
}