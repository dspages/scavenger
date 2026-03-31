using UnityEngine;

public static class LineOfSightUtils
{
    /// <summary>
    /// Bresenham line from <paramref name="from"/> to <paramref name="to"/>.
    /// Fails if any tile along the path has <see cref="Tile.blocksVision"/> (except the start tile).
    /// If <paramref name="intermediateOccupantsBlock"/> is true, also fails when a strictly intermediate tile
    /// has an <see cref="Tile.occupant"/> (ranged cover); endpoints are not checked for occupants.
    /// </summary>
    public static bool HasLineOfSight(Tile from, Tile to, TileManager tileManager, bool intermediateOccupantsBlock = false)
    {
        if (from == null || to == null || tileManager == null) return false;

        int x0 = from.x, y0 = from.y;
        int x1 = to.x, y1 = to.y;
        if (x0 == x1 && y0 == y1) return true;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        int maxSteps = Mathf.Min(Globals.COMBAT_WIDTH + Globals.COMBAT_HEIGHT, dx + dy + 2);
        int x = x0, y = y0;
        for (int step = 0; step < maxSteps; step++)
        {
            if (x == x1 && y == y1) return true;
            Tile currentTile = tileManager.getTile(x, y);
            if (currentTile != null && currentTile.blocksVision && currentTile != from)
                return false;
            if (intermediateOccupantsBlock && currentTile != null && currentTile != from && currentTile.occupant != null)
                return false;
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
        return (x == x1 && y == y1);
    }
}
