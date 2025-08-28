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
    [SerializeField] public int actionPointCost = 6;
    
    public override int ACTION_COST { get { return actionPointCost; } }

    // Attack-specific properties to be overridden by subclasses
    public abstract bool RequiresLineOfSight { get; }
    public abstract bool TargetsEnemiesOnly { get; }
    public abstract bool CanTargetEmptyTiles { get; }

    public override string DisplayName()
    {
        return actionDisplayName;
    }

    public override void BeginAction(Tile targetTile)
    {
        if (targetTile == null)
        {
            EndAction();
            return;
        }

        // Validate target based on attack type
        if (!IsValidTarget(targetTile))
        {
            EndAction();
            return;
        }

        // Validate range using Manhattan distance via BFS steps from current tile
        Tile origin = combatController.GetCurrentTile();
        if (origin == null)
        {
            EndAction();
            return;
        }

        int distance = CalculateManhattanDistance(origin, targetTile);
        if (distance < 0 || distance < minRange || distance > maxRange)
        {
            // Out of range; do nothing
            EndAction();
            return;
        }

        // Check line of sight if required
        if (RequiresLineOfSight && !LineOfSightUtils.HasLineOfSight(origin, targetTile, FindObjectOfType<TileManager>()))
        {
            EndAction();
            return;
        }

        // Use ActionMove's movement system to get to the target
        currentPhase = Phase.MOVING;
        PreparePath(targetTile);
        base.BeginAction(targetTile);
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
        if (path.Count == 1)
        {
            Tile targetTile = path.Pop();
            
            // Face the target
            Vector3 direction = CalculateDirection(targetTile.transform.position);
            direction.y = 0f;
            if (direction != Vector3.zero)
                transform.forward = direction;
            
            // Perform the attack
            PerformAttack(targetTile);
            StartCoroutine(EndActionAfterDelay(GetAttackDuration()));
        }
        else
        {
            StartCoroutine(EndActionAfterDelay(GetAttackDuration()));
        }
    }

    // Abstract method for subclasses to implement their specific attack logic
    protected abstract void PerformAttack(Tile targetTile);

    // Virtual method for attack duration (can be overridden)
    protected virtual float GetAttackDuration()
    {
        return 1.0f;
    }

    // Validate if the target tile is valid for this attack type
    protected virtual bool IsValidTarget(Tile targetTile)
    {
        if (TargetsEnemiesOnly)
        {
            return targetTile.occupant != null && combatController.ContainsEnemy(targetTile);
        }
        
        if (CanTargetEmptyTiles)
        {
            return true; // Ground attacks can target any tile
        }
        
        return targetTile.occupant != null; // Must have an occupant but not necessarily an enemy
    }

    // Helper method for range calculation (shared across all attack types)
    protected int CalculateManhattanDistance(Tile start, Tile end)
    {
        if (start == null || end == null) return -1;
        
        // BFS to compute steps since tile coordinates may not be strictly grid-aligned
        Queue<Tile> queue = new Queue<Tile>();
        HashSet<Tile> visited = new HashSet<Tile>();
        Dictionary<Tile, int> dist = new Dictionary<Tile, int>();
        queue.Enqueue(start);
        visited.Add(start);
        dist[start] = 0;
        
        while (queue.Count > 0)
        {
            Tile t = queue.Dequeue();
            if (t == end) return dist[t];
            foreach (Tile n in t.Neighbors())
            {
                if (n == null || visited.Contains(n)) continue;
                visited.Add(n);
                dist[n] = dist[t] + 1;
                queue.Enqueue(n);
            }
        }
        return -1;
    }

    public override string Description()
    {
        string desc = base.Description();
        desc += $"{actionDisplayName} deals {baseDamage} damage. Range: {minRange}-{maxRange}.";
        return desc;
    }
}
