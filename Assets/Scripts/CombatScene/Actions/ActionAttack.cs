using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base class for all attack actions (melee, ranged, ground attacks)
// Handles movement, range validation, and common attack logic
public abstract class ActionAttack : ActionMove
{
    // Configurable parameters (can be set dynamically when mapped to an item)
    [SerializeField] public int maxRange = 1;
    [SerializeField] public int minRange = 1;
    [SerializeField] public int baseDamage = 5;
    [SerializeField] public string actionDisplayName = "Attack";
    
    // Expose base action cost for configuration via items/spells
    public override int BASE_ACTION_COST { get { return baseActionCost; } set { baseActionCost = value; } }

    // Attack-specific properties to be overridden by subclasses
    public abstract bool RequiresLineOfSight { get; }
    public abstract bool TargetsEnemiesOnly { get; }
    public abstract bool CanTargetEmptyTiles { get; }

    // Optional AoE radius for ground-target actions; default 0 (no AoE)
    public virtual int AOE_RADIUS { get { return 0; } }

    public override string DisplayName()
    {
        return actionDisplayName;
    }

    // The tile we intend to attack after movement completes
    protected Tile pendingAttackTarget;

    // It's the responsibility of the controller to validate the target, so no need to do it here.
    public override void BeginAction(Tile targetTile)
    {
        // Store the attack target and move toward the launch tile (separate attack parent)
        pendingAttackTarget = targetTile;

        // Resolve launch tile: prefer attack parent, fallback to movement parent, else current tile
        Tile launchTile = targetTile != null && targetTile.searchAttackParent != null
            ? targetTile.searchAttackParent
            : (targetTile != null && targetTile.searchParent != null
                ? targetTile.searchParent
                : combatController.GetCurrentTile());

        currentPhase = Phase.MOVING;
        PreparePath(launchTile);
        // Call Action.BeginAction semantics without re-preparing a path to the target tile
        inProgress = true;
        characterSheet.DisplayPopupDuringCombat(DisplayName());
        combatController.BeginAction();
    }

    // Override Update to handle attack phase after movement
    protected override void Update()
    {
        if (!inProgress)
        {
            return;
        }
        if (currentPhase == Phase.MOVING)
        {
            Move();
        }
        else if (currentPhase == Phase.ATTACKING)
        {
            AttackPhase();
        }
        else
        {
            currentPhase = Phase.NONE;
        }
    }

    protected virtual void AttackPhase()
    {
        Tile targetTile = pendingAttackTarget;
        
        // Face the target
        Vector3 direction = CalculateDirection(targetTile.transform.position);
        if (direction != Vector3.zero)
            transform.up = new Vector3(direction.x, direction.y, 0f);
        
        // Perform the attack
        currentPhase = Phase.RESOLVING_ATTACK;
        PerformAttack(targetTile);
        // Accrue base action cost (movement already accrued in PreparePath)
        actionPointCost += BASE_ACTION_COST;
        StartCoroutine(EndActionAfterDelayWithGridSnap(GetAttackDuration()));
    }

    // Custom coroutine that snaps to grid direction after attack delay
    protected IEnumerator EndActionAfterDelayWithGridSnap(float fDuration)
    {
        currentPhase = Phase.NONE;
        yield return new WaitForSeconds(fDuration);
        
        // Snap rotation to nearest cardinal direction
        SnapToCardinalDirection();
        
        EndAction();
        yield break;
    }

    // Snap the character's rotation to the nearest cardinal direction (N, S, E, W)
    private void SnapToCardinalDirection()
    {
        Vector3 currentUp = transform.up;
        Vector3 snapDirection;
        
        // Determine which cardinal direction is closest
        float absX = Mathf.Abs(currentUp.x);
        float absY = Mathf.Abs(currentUp.y);
        
        if (absX > absY)
        {
            // More horizontal than vertical - snap to East or West
            snapDirection = currentUp.x > 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            // More vertical than horizontal - snap to North or South
            snapDirection = currentUp.y > 0 ? Vector3.up : Vector3.down;
        }
        
        transform.up = snapDirection;
    }

    // Abstract method for subclasses to implement their specific attack logic
    protected abstract void PerformAttack(Tile targetTile);

    // Virtual method for attack duration (can be overridden)
    protected virtual float GetAttackDuration()
    {
        return 1.0f;
    }

    // Helper method for range calculation (shared across all attack types)
    protected int CalculateManhattanDistance(Tile start, Tile end)
    {
        if (start == null || end == null) return -1;
        return Mathf.Abs(start.x - end.x) + Mathf.Abs(start.y - end.y);
    }

    public override string Description()
    {
        string desc = base.Description();
        desc += $"{actionDisplayName} deals {baseDamage} damage. Range: {minRange}-{maxRange}.";
        return desc;
    }
}
