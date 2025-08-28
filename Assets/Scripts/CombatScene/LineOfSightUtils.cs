using UnityEngine;

public static class LineOfSightUtils
{
    /// <summary>
    /// Calculates line of sight between two tiles using Bresenham's line algorithm.
    /// Returns false if any vision-blocking tiles are found along the path (excluding the start tile).
    /// </summary>
    /// <param name="from">Starting tile</param>
    /// <param name="to">Target tile</param>
    /// <param name="tileManager">TileManager instance to get tiles</param>
    /// <returns>True if line of sight is clear, false if blocked</returns>
    public static bool HasLineOfSight(Tile from, Tile to, TileManager tileManager)
    {
        if (from == null || to == null || tileManager == null) return false;
        
        // Simple line of sight using Bresenham's line algorithm
        int x0 = from.x, y0 = from.y;
        int x1 = to.x, y1 = to.y;
        
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        
        int x = x0, y = y0;
        
        while (true)
        {
            if (x == x1 && y == y1) break;
            
            // Check if current tile blocks vision
            Tile currentTile = tileManager.getTile(x, y);
            if (currentTile != null && currentTile.blocksVision && currentTile != from)
            {
                return false;
            }
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
        
        return true;
    }
}

