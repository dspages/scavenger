using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionMove : Action
{
    [SerializeField] private float moveSpeed = 6;
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
            if (Vector3.Distance(transform.position, targetPos) >= 0.01f)
            {
                Vector3 direction = CalculateDirection(targetPos);
                transform.up = new Vector3(direction.x, direction.y, 0f);
                transform.Translate(direction * Time.deltaTime * moveSpeed, Space.World);
            }
             // Center of a new tile in the chain reached.
            else
            {
                transform.position = targetPos;
                
                // Check for hidden enemies in the target tile
                if (CheckForHiddenEnemy(tile))
                {
                    // Stop movement and refund remaining points
                    HandleHiddenEnemyCollision(tile);
                    return;
                }
                
                // Update tile occupancy for every tile we enter (not just the final destination)
                // This ensures vision updates appropriately as the unit moves through tiles
                combatController.SetCurrentTile(tile);
                
                // Pop the completed tile from the path
                path.Pop();
                
                // Trigger vision update when we actually change tiles
                VisionSystem visionSystem = FindObjectOfType<VisionSystem>();
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
    
    private void HandleHiddenEnemyCollision(Tile tile)
    {
        CombatController hiddenEnemy = tile.occupant;
        
        // Remove HIDDEN status effect using the CharacterSheet method
        // This will automatically notify the VisionSystem through the CombatController
        hiddenEnemy.characterSheet.RemoveStatusEffect(StatusEffect.EffectType.HIDDEN);
        
        // Clear the path and stop movement
        path.Clear();
        EndAction();
        
        // Display message
        combatController.DisplayPopupDuringCombat("Hidden enemy revealed!");
        
        // Vision system will be notified automatically by the CharacterSheet method
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
            // Only has a move cost if it isn't the origin tile.
            if (next.searchParent) actionPointCost += next.GetMoveCost();
            path.Push(next);
            next = next.searchParent;
        }
    }

    protected Vector3 CalculateDirection(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        return direction.normalized;
    }
}
