using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CombatController : MonoBehaviour
{
    public CharacterSheet characterSheet = null;
    public bool isTurn = false;
    public bool isActing = false;
    protected Action selectedAction = null;
    protected List<Tile> selectableTiles = new List<Tile>();
    protected TileManager manager;

    protected bool hasMoved = false;
    private Tile currentTile;

    // Use this for initialization
    virtual protected void Start()
    {
        manager = FindObjectOfType<TileManager>();
        selectedAction = GetComponent<ActionWeaponAttack>();
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
        // Default facing direction (can be overridden by subclasses)
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

        // TODO: Replace with PriorityQueue for performance optimization
        List<Tile> queue = new List<Tile>();
        queue.Add(currentTile);
        currentTile.searchWasVisited = true;

        while (queue.Count > 0)
        {
            queue.Sort((item1, item2) => item1.searchDistance.CompareTo(item2.searchDistance));
            Tile tile = queue[0];
            queue.RemoveAt(0);

            foreach (Tile adjacentTile in tile.Neighbors())
            {
                if (!adjacentTile.searchWasVisited)
                {
                    // Potential melee attacks
                    if (ContainsEnemy(adjacentTile))
                    {
                        AttachTile(adjacentTile, tile, 0);
                    }
                    // Potential moves
                    if (adjacentTile.occupant == null && adjacentTile.GetMoveCost() + tile.searchDistance <= characterSheet.currentMovePoints)
                    {
                        AttachTile(adjacentTile, tile);
                        queue.Add(adjacentTile);
                    }
                }
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
            tile.searchDistance = moveCostOverride + parent.searchDistance;
        }
        else
        {
            tile.searchDistance = tile.GetMoveCost() + parent.searchDistance;
        }
    }
}