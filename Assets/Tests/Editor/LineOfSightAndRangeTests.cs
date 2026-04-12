using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Regression tests for ranged LOS (Bresenham) and tile range checks used by attack targeting.
/// </summary>
[TestFixture]
public class LineOfSightAndRangeTests
{
    private TileManager _tileManagerGo;
    private Tile[,] _grid;

    [TearDown]
    public void TearDown()
    {
        if (_tileManagerGo != null)
        {
            Object.DestroyImmediate(_tileManagerGo.gameObject);
            _tileManagerGo = null;
        }
        _grid = null;
    }

    private static void AssignGrid(TileManager tm, Tile[,] grid)
    {
        var fi = typeof(TileManager).GetField("tileGrid", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(fi);
        fi.SetValue(tm, grid);
    }

    private Tile CreateTile(int x, int y, bool blocksVision = false)
    {
        var go = new GameObject($"Tile_{x}_{y}");
        if (_tileManagerGo != null)
            go.transform.SetParent(_tileManagerGo.transform);
        var t = go.AddComponent<Tile>();
        t.x = x;
        t.y = y;
        t.blocksVision = blocksVision;
        return t;
    }

    private void BuildManager(int width, int height)
    {
        _grid = new Tile[width, height];
        var go = new GameObject("TestTileManager");
        _tileManagerGo = go.AddComponent<TileManager>();
        AssignGrid(_tileManagerGo, _grid);
    }

    [Test]
    public void LineOfSight_SameTile_ReturnsTrue()
    {
        BuildManager(Globals.COMBAT_WIDTH, Globals.COMBAT_HEIGHT);
        var t = CreateTile(3, 3);
        _grid[3, 3] = t;

        Assert.IsTrue(LineOfSightUtils.HasLineOfSight(t, t, _tileManagerGo, false));
    }

    [Test]
    public void LineOfSight_ClearStraightLine_ReturnsTrue()
    {
        BuildManager(Globals.COMBAT_WIDTH, Globals.COMBAT_HEIGHT);
        var from = CreateTile(2, 2);
        var to = CreateTile(5, 2);
        for (int x = 2; x <= 5; x++)
            _grid[x, 2] = CreateTile(x, 2);

        Assert.IsTrue(LineOfSightUtils.HasLineOfSight(from, to, _tileManagerGo, false));
    }

    [Test]
    public void LineOfSight_BlockedVisionTile_ReturnsFalse()
    {
        BuildManager(Globals.COMBAT_WIDTH, Globals.COMBAT_HEIGHT);
        var from = CreateTile(1, 1);
        var to = CreateTile(4, 1);
        _grid[1, 1] = from;
        _grid[2, 1] = CreateTile(2, 1, blocksVision: true);
        _grid[3, 1] = CreateTile(3, 1);
        _grid[4, 1] = to;

        Assert.IsFalse(LineOfSightUtils.HasLineOfSight(from, to, _tileManagerGo, false));
    }

    [Test]
    public void LineOfSight_InterposedOccupantBlocksWhenRangedCoverEnabled()
    {
        BuildManager(Globals.COMBAT_WIDTH, Globals.COMBAT_HEIGHT);
        var from = CreateTile(0, 0);
        var to = CreateTile(3, 0);
        for (int x = 0; x <= 3; x++)
            _grid[x, 0] = CreateTile(x, 0);

        var blockerGo = new GameObject("Blocker");
        blockerGo.transform.SetParent(_tileManagerGo.transform);
        var blocker = blockerGo.AddComponent<CombatController>();
        _grid[1, 0].occupant = blocker;

        Assert.IsFalse(LineOfSightUtils.HasLineOfSight(from, to, _tileManagerGo, intermediateOccupantsBlock: true));
    }

    [Test]
    public void LineOfSight_NullOrMissingManager_ReturnsFalse()
    {
        var go = new GameObject("LooseTile");
        var t = go.AddComponent<Tile>();
        t.x = 0;
        t.y = 0;
        try
        {
            Assert.IsFalse(LineOfSightUtils.HasLineOfSight(null, t, null, false));
            Assert.IsFalse(LineOfSightUtils.HasLineOfSight(t, null, null, false));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void IsTileInRange_SameCoordinatesAsFrom_ReturnsFalse()
    {
        BuildManager(Globals.COMBAT_WIDTH, Globals.COMBAT_HEIGHT);
        var from = CreateTile(4, 4);
        _grid[4, 4] = from;

        Assert.IsFalse(ReachabilityResolver.IsTileInRange(from, 1, 3, false, 4, 4, _tileManagerGo));
    }

    [Test]
    public void IsTileInRange_ManhattanWithinMinMax_WithoutLos_ReturnsTrue()
    {
        BuildManager(Globals.COMBAT_WIDTH, Globals.COMBAT_HEIGHT);
        var from = CreateTile(5, 5);
        _grid[5, 5] = from;
        _grid[5, 8] = CreateTile(5, 8);

        Assert.IsTrue(ReachabilityResolver.IsTileInRange(from, 2, 4, requiresLineOfSight: false, 5, 8, _tileManagerGo));
    }

    [Test]
    public void IsTileInRange_OutOfBounds_ReturnsFalse()
    {
        BuildManager(Globals.COMBAT_WIDTH, Globals.COMBAT_HEIGHT);
        var from = CreateTile(0, 0);
        _grid[0, 0] = from;

        Assert.IsFalse(ReachabilityResolver.IsTileInRange(from, 1, 2, false, -1, 0, _tileManagerGo));
        Assert.IsFalse(ReachabilityResolver.IsTileInRange(from, 1, 2, false, Globals.COMBAT_WIDTH, 0, _tileManagerGo));
    }

    [Test]
    public void IsTileInRange_RequiresLos_Blocked_ReturnsFalse()
    {
        BuildManager(Globals.COMBAT_WIDTH, Globals.COMBAT_HEIGHT);
        var from = CreateTile(1, 1);
        var target = CreateTile(4, 1);
        _grid[1, 1] = from;
        _grid[2, 1] = CreateTile(2, 1, blocksVision: true);
        _grid[3, 1] = CreateTile(3, 1);
        _grid[4, 1] = target;

        Assert.IsFalse(ReachabilityResolver.IsTileInRange(from, 1, 5, requiresLineOfSight: true, 4, 1, _tileManagerGo));
    }
}
