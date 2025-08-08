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
        return false;
    }

    public void SetCharacterSheet(CharacterSheet c)
    {
        characterSheet = c;
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

    public void BeginTurn()
    {
        if (Dead()) return;
        characterSheet.BeginTurn();
        if (DoesGUI())
        {
            // characterSheet.UpdateUI();
        }
        isTurn = true;
        hasMoved = false;
        FindSelectableBasicTiles();
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
                    if (characterSheet.canAttack && ContainsEnemy(adjacentTile))
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