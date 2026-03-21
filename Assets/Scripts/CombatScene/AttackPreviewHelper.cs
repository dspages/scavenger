using System;
using System.Collections.Generic;
using UnityEngine;

public static class AttackPreviewHelper
{
    public static void DrawCompositeAttackPreview(PathRenderer pathRenderer, Tile targetTile, Tile currentTile, int aoeRadius)
    {
        if (pathRenderer == null || targetTile == null) return;

        Tile launchTile = AttackLaunchTile.Resolve(targetTile, currentTile);

        

        // Draw movement path from origin to launch tile (do not include the segment to the target).
        // Cycle detection: pathfinding uses a closed set so searchParent is acyclic; this guards against bugs.
        HashSet<Tile> seen = new HashSet<Tile>();
        Tile t = launchTile;
        while (t != null && t.searchParent && seen.Add(t))
        {
            pathRenderer.DrawSegment(t.transform.position, t.searchParent.transform.position, PathRenderer.LineType.MovementPath);
            t = t.searchParent;
        }

        // Draw projectile/spell line from launch to target
        if (launchTile != null)
        {
            pathRenderer.DrawSegment(launchTile.transform.position, targetTile.transform.position, PathRenderer.LineType.SpellTarget);
        }

        // Highlight AoE if provided
        if (aoeRadius > 0)
        {
            HighlightAoETiles(targetTile, aoeRadius);
        }
    }

    public static void HighlightAoETiles(Tile center, int radius)
    {
        if (center == null || radius < 0) return;
        foreach (Tile t in EnumerateAoETiles(center, radius))
        {
            t.isHovered = true;
        }
    }

    // Shared AoE BFS: enumerates tiles within radius (including center at depth 0)
    public static List<Tile> EnumerateAoETiles(Tile center, int radius)
    {
        List<Tile> result = new List<Tile>();
        if (center == null || radius < 0) return result;

        Queue<Tile> queue = new Queue<Tile>();
        HashSet<Tile> visited = new HashSet<Tile>();
        Dictionary<Tile, int> depth = new Dictionary<Tile, int>();

        queue.Enqueue(center);
        visited.Add(center);
        depth[center] = 0;

        while (queue.Count > 0)
        {
            Tile t = queue.Dequeue();
            result.Add(t);
            int d = depth[t];
            if (d >= radius) continue;
            foreach (Tile n in t.Neighbors())
            {
                if (n == null || visited.Contains(n)) continue;
                visited.Add(n);
                depth[n] = d + 1;
                queue.Enqueue(n);
            }
        }

        return result;
    }

    // Note: VisitAoETiles was removed as unused. Use EnumerateAoETiles or extend as needed.
}


