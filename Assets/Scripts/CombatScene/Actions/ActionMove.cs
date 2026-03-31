using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionMove : Action
{
    private float moveSpeed = 4;
    protected Stack<Tile> path = new Stack<Tile>();
    protected int reserveTiles = 0;

    override protected void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    virtual protected void Update()
    {
        if (!inProgress)
        {
            return;
        }
        if (currentPhase == Phase.MOVING)
        {
            Move();
        }
        else
        {
            EndAction();
        }
    }

    protected void Move()
    {
        if (path.Count > 0)
        {
            Tile tile = path.Peek();
            Vector3 targetPos = tile.transform.position;

            // Inbetween tiles, move toward the next tile in the chain.
            if (Vector3.Distance(transform.position, targetPos) >= Time.deltaTime * moveSpeed)
            {
                Vector3 direction = CalculateDirection(targetPos);
                transform.up = new Vector3(direction.x, direction.y, 0f);
                transform.Translate(direction * Time.deltaTime * moveSpeed, Space.World);
            }
             // Center of a new tile in the chain reached.
            else
            {
                transform.position = targetPos;
                AccrueMovementCostForEnteredTile(tile);

                if (TryInterruptMovementForHazard(tile))
                    return;

                // Update tile occupancy for every tile we enter (not just the final destination)
                // This ensures vision updates appropriately as the unit moves through tiles
                combatController.SetCurrentTile(tile);

                // Pop the completed tile from the path
                path.Pop();
                
                // Trigger vision update when we actually change tiles
                VisionSystem visionSystem = FindFirstObjectByType<VisionSystem>(FindObjectsInactive.Exclude);
                if (visionSystem != null)
                {
                    visionSystem.UpdateVision();
                }
            }
        }
        else
        {
            // Done moving.
            currentPhase = Phase.ATTACKING;
        }
    }
    
    private bool CheckForHiddenEnemy(Tile tile)
    {
        if (tile.occupant == null) return false;
        
        // Check if the occupant is an enemy and has HIDDEN status effect
        if (tile.occupant.IsEnemy() && 
            StatusEffect.HasEffectType(ref tile.occupant.characterSheet.statusEffects, StatusEffect.EffectType.HIDDEN))
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>Mid-move interrupts (hidden unit, future ground hazards). Returns true if movement stopped.</summary>
    protected virtual bool TryInterruptMovementForHazard(Tile tile)
    {
        if (CheckForHiddenEnemy(tile))
        {
            CombatController hiddenEnemy = tile.occupant;
            hiddenEnemy.characterSheet.RemoveStatusEffect(StatusEffect.EffectType.HIDDEN);
            InterruptMovementWithMessage("Hidden enemy revealed!");
            return true;
        }
        return false;
    }

    override public void BeginAction(Tile targetTile)
    {
        currentPhase = Phase.MOVING;
        PreparePath(targetTile);
        base.BeginAction(targetTile);
    }

    protected void PreparePath(Tile targetTile)
    {
        path.Clear();
        actionPointCost = 0;
        Tile next = targetTile;
        while (next != null)
        {
            path.Push(next);
            next = next.searchParent;
        }
    }

    /// <summary>Movement AP is accrued in <see cref="Move"/> per tile entered so mid-path interrupts only pay for tiles walked.</summary>
    void AccrueMovementCostForEnteredTile(Tile tile)
    {
        if (tile != null && tile.searchParent)
            actionPointCost += tile.GetMoveCost();
    }

    /// <summary>Hidden enemies, traps, etc.: stop moving and end the action; movement AP already reflects tiles walked.</summary>
    protected void InterruptMovementWithMessage(string message)
    {
        path.Clear();
        EndAction();
        combatController.DisplayPopupDuringCombat(message);
    }

    protected Vector3 CalculateDirection(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        return direction.normalized;
    }
}
