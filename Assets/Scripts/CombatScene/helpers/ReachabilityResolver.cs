using System;
using System.Collections.Generic;
using UnityEngine;

public static class ReachabilityResolver
{
    public static List<Tile> FindSelectableTiles(
        Tile origin,
        int actionPoints,
        Action selectedAction,
        TileManager manager,
        bool isPC,
        Func<Tile, bool> isVisibleByActor)
    {
        var selectableTiles = new List<Tile>();

        if (selectedAction != null && selectedAction.TARGET_TYPE == Action.TargetType.SELF_ONLY)
        {
            manager.ResetTileSearch();
            origin.searchDistance = 0;
            origin.searchWasVisited = true;
            bool canAffordAp = selectedAction.BASE_ACTION_COST <= actionPoints;
            origin.searchCanBeChosen = canAffordAp;
            origin.searchHardCostsAffordable = true;
            selectableTiles.Add(origin);
            return selectableTiles;
        }

        origin.searchDistance = 0;
        origin.searchWasVisited = true;
        selectableTiles.Add(origin);

        var losCache = new Dictionary<(Tile, Tile), bool>();
        TurnManager turnManager = UnityEngine.Object.FindFirstObjectByType<TurnManager>(FindObjectsInactive.Exclude);

        var reachable = PathfindingSystem.FindReachableTiles(
            origin, actionPoints, manager,
            (from, to) => to.occupant == null,
            (tile) => FindAttackTargetsFromTile(
                tile, selectedAction, actionPoints, manager, isPC,
                isVisibleByActor, selectableTiles, losCache, turnManager));

        bool canChooseMove = selectedAction == null ||
            selectedAction.TARGET_TYPE != Action.TargetType.GROUND_TILE;

        foreach (var t in reachable)
        {
            if (!selectableTiles.Contains(t))
            {
                t.searchCanBeChosen = canChooseMove;
                selectableTiles.Add(t);
            }
        }

        return selectableTiles;
    }

    private static void FindAttackTargetsFromTile(
        Tile fromTile,
        Action selectedAction,
        int actionPoints,
        TileManager manager,
        bool isPC,
        Func<Tile, bool> isVisibleByActor,
        List<Tile> selectableTiles,
        Dictionary<(Tile, Tile), bool> losCache,
        TurnManager tm)
    {
        if (selectedAction == null) return;

        int movementCost = fromTile.searchDistance;
        if (movementCost + selectedAction.BASE_ACTION_COST > actionPoints)
            return;

        if (selectedAction is ActionAttack atk)
        {
            List<CombatController> potentialTargets = new List<CombatController>();
            HashSet<Tile> tilesInRange = new HashSet<Tile>();

            switch (selectedAction.TARGET_TYPE)
            {
                case Action.TargetType.GROUND_TILE:
                    tilesInRange = FindTilesInRange(fromTile, atk.minRange, atk.maxRange, atk.RequiresLineOfSight, manager);
                    break;
                case Action.TargetType.MELEE:
                case Action.TargetType.MELEE_REACH:
                case Action.TargetType.RANGED:
                case Action.TargetType.CHARGE:
                    potentialTargets = isPC ? tm.AllLivingEnemies() : tm.AllLivingPCs();
                    tilesInRange = FilterTilesInRange(
                        fromTile, atk.minRange, atk.maxRange, atk.RequiresLineOfSight, potentialTargets, manager, isVisibleByActor,
                        interposedOccupantsBlockRanged: atk.TARGET_TYPE == Action.TargetType.RANGED);
                    break;
                case Action.TargetType.SELF_OR_ALLY:
                    potentialTargets = isPC ? tm.AllLivingPCs() : tm.AllLivingEnemies();
                    tilesInRange = FilterTilesInRange(
                        fromTile, atk.minRange, atk.maxRange, atk.RequiresLineOfSight, potentialTargets, manager, isVisibleByActor,
                        interposedOccupantsBlockRanged: false);
                    break;
            }

            foreach (Tile targetTile in tilesInRange)
            {
                if (targetTile.searchCanBeChosen) continue;
                targetTile.searchAttackParent = fromTile;
                selectableTiles.Add(targetTile);
                targetTile.searchCanBeChosen = true;
                targetTile.searchDistance = movementCost + atk.BASE_ACTION_COST;
            }
        }
    }

    public static bool IsTileInRange(
        Tile fromTile, int minRange, int maxRange, bool requiresLineOfSight, int x, int y, TileManager manager,
        bool interposedOccupantsBlockRanged = false)
    {
        if (x < 0 || x >= Globals.COMBAT_WIDTH || y < 0 || y >= Globals.COMBAT_HEIGHT) return false;

        int dist = Mathf.Abs(fromTile.x - x) + Mathf.Abs(fromTile.y - y);
        if (dist < minRange || dist > maxRange) return false;

        Tile tile = manager.getTile(x, y);
        if (tile == null || tile == fromTile) return false;
        if (!requiresLineOfSight) return true;
        return LineOfSightUtils.HasLineOfSight(fromTile, tile, manager, interposedOccupantsBlockRanged);
    }

    public static HashSet<Tile> FilterTilesInRange(
        Tile fromTile, int minRange, int maxRange, bool requiresLineOfSight,
        List<CombatController> potentialTargets, TileManager manager,
        Func<Tile, bool> isVisibleByActor,
        bool interposedOccupantsBlockRanged = false)
    {
        var filtered = new HashSet<Tile>();
        foreach (var cc in potentialTargets)
        {
            Tile tile = cc.GetCurrentTile();
            if (IsTileInRange(fromTile, minRange, maxRange, requiresLineOfSight, tile.x, tile.y, manager, interposedOccupantsBlockRanged)
                && isVisibleByActor(tile))
            {
                filtered.Add(tile);
            }
        }
        return filtered;
    }

    public static HashSet<Tile> FindTilesInRange(Tile fromTile, int minRange, int maxRange, bool requiresLineOfSight, TileManager manager)
    {
        var tilesInRange = new HashSet<Tile>();
        int cappedMax = Mathf.Min(maxRange, Globals.COMBAT_WIDTH + Globals.COMBAT_HEIGHT);
        for (int dx = -cappedMax; dx <= cappedMax; dx++)
        {
            int remaining = cappedMax - Mathf.Abs(dx);
            for (int dy = -remaining; dy <= remaining; dy++)
            {
                if (IsTileInRange(fromTile, minRange, maxRange, requiresLineOfSight, fromTile.x + dx, fromTile.y + dy, manager))
                {
                    tilesInRange.Add(manager.getTile(fromTile.x + dx, fromTile.y + dy));
                }
            }
        }
        return tilesInRange;
    }
}
