using UnityEngine;

/// <summary>Resolves the tile an attack is launched from (same rules as ActionAttack.BeginAction).</summary>
public static class AttackLaunchTile
{
    /// <param name="targetTile">Tile under attack (e.g. enemy's tile).</param>
    /// <param name="attackerCurrentTile">Attacker's current tile when resolving the preview or tooltip.</param>
    public static Tile Resolve(Tile targetTile, Tile attackerCurrentTile)
    {
        if (targetTile != null && targetTile.searchAttackParent != null)
            return targetTile.searchAttackParent;
        if (targetTile != null && targetTile.searchParent != null)
            return targetTile.searchParent;
        return attackerCurrentTile;
    }
}
