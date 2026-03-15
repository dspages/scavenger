using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class PathfindingSystem
{
    /// <summary>
    /// Performs a Breadth-First Search (or Dijkstra if costs vary) to find reachable tiles.
    /// </summary>
    /// <param name="startTile">The starting tile.</param>
    /// <param name="maxCost">Maximum movement cost/range.</param>
    /// <param name="tileManager">Reference to TileManager to reset search state.</param>
    /// <param name="isTraversable">Delegate to check if a neighbor can be entered. Returns true if traversable.</param>
    /// <param name="onVisit">Optional delegate called when a tile is visited (popped from queue). Useful for side effects like checking attack ranges.</param>
    /// <param name="getCost">Optional delegate to calculate move cost. Defaults to neighbor.GetMoveCost().</param>
    /// <returns>A list of all reachable tiles (excluding the start tile).</returns>
    public static List<Tile> FindReachableTiles(
        Tile startTile, 
        int maxCost, 
        TileManager tileManager, 
        Func<Tile, Tile, bool> isTraversable,
        Action<Tile> onVisit = null,
        Func<Tile, Tile, int> getCost = null)
    {
        List<Tile> reachable = new List<Tile>();
        if (startTile == null || tileManager == null) return reachable;

        // Reset search state
        tileManager.ResetTileSearch();

        // Closed set: once we expand a tile we never update its parent again, so searchParent cannot form a cycle.
        HashSet<Tile> closed = new HashSet<Tile>();
        List<Tile> queue = new List<Tile>();

        startTile.searchDistance = 0;
        startTile.searchWasVisited = true;
        queue.Add(startTile);

        while (queue.Count > 0)
        {
            queue.Sort((a, b) => a.searchDistance.CompareTo(b.searchDistance));
            Tile current = queue[0];
            queue.RemoveAt(0);

            if (closed.Contains(current)) continue;
            closed.Add(current);

            if (current != startTile)
                reachable.Add(current);

            onVisit?.Invoke(current);

            foreach (Tile neighbor in current.Neighbors())
            {
                if (neighbor == null || closed.Contains(neighbor)) continue;
                if (!isTraversable(current, neighbor)) continue;

                int moveCost = (getCost != null) ? getCost(current, neighbor) : neighbor.GetMoveCost();
                int newCost = current.searchDistance + moveCost;

                if (newCost <= maxCost && (!neighbor.searchWasVisited || newCost < neighbor.searchDistance))
                {
                    neighbor.searchDistance = newCost;
                    neighbor.searchParent = current;
                    neighbor.searchWasVisited = true;
                    if (!queue.Contains(neighbor))
                        queue.Add(neighbor);
                }
            }
        }

        return reachable;
    }

    /// <summary>
    /// Simple overload for standard movement (blocking on occupants).
    /// </summary>
    public static List<Tile> GetReachableTilesSimple(Tile startTile, int maxCost, TileManager tileManager)
    {
        return FindReachableTiles(startTile, maxCost, tileManager, (from, to) => 
        {
            // Standard rule: can't enter occupied tiles
            return to.occupant == null;
        });
    }
}

