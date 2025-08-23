using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// There should only ever be one TileManager per combat scene.
public class TileManager : MonoBehaviour
{
    [SerializeField] GameObject[] tilePrefabs;
    private Tile[,] tileGrid = new Tile[Globals.COMBAT_WIDTH, Globals.COMBAT_HEIGHT];

    // Use this for initialization
    void Start()
    {
        TurnManager manager = GameObject.FindObjectOfType<TurnManager>();
        // Populate Tile Grid.
        for (int x = 0; x < Globals.COMBAT_WIDTH; x++)
        {
            for (int y = 0; y < Globals.COMBAT_HEIGHT; y++)
            {
                int maxPick = tilePrefabs.Length;
                int randPick = Globals.rng.Next(0, tilePrefabs.Length);
                GameObject selectedPrefab = tilePrefabs[randPick];
                GameObject newTile = Instantiate(selectedPrefab, new Vector3(x, y, 0), Quaternion.identity);
                SpriteRenderer rend = newTile.GetComponent<SpriteRenderer>();
                rend.sortingOrder = -1;
                Tile t = newTile.GetComponent<Tile>();
                tileGrid[x, y] = t;
                t.x = x; t.y = y;
            }
        }
        EnemyParty.GenerateNewEnemies();
        EnemyParty.SpawnPartyMembers(manager);
        PlayerParty.SpawnPartyMembers(manager);

        //manager.combatants.Sort(new SortCombatants());
        manager.InitiateCombat();
        
        // Initialize vision system after all units are spawned
        StartCoroutine(InitializeVisionSystemAfterDelay());
    }
    
    private IEnumerator InitializeVisionSystemAfterDelay()
    {
        // Wait a frame to ensure all units are properly spawned
        yield return null;
        
        VisionSystem visionSystem = FindObjectOfType<VisionSystem>();
        if (visionSystem != null)
        {
            visionSystem.UpdateVision();
        }
    }

    public Tile getTile(int xCoord, int yCoord)
    {
        return tileGrid[xCoord, yCoord];
    }

    public enum Direction { North, South, East, West }

    public Tile GetAdjacentTile(Tile origin, Direction direction)
    {
        int newX = origin.x;
        int newY = origin.y;

        switch (direction)
        {
            case Direction.North:
                newY += 1;
                break;
            case Direction.South:
                newY -= 1;
                break;
            case Direction.East:
                newX += 1;
                break;
            case Direction.West:
                newX -= 1;
                break;
        }

        // Check bounds
        if (newX < 0 || newX >= Globals.COMBAT_WIDTH || 
            newY < 0 || newY >= Globals.COMBAT_HEIGHT)
            return null;

        return tileGrid[newX, newY];
    }

    public Tile GetNorthTile(Tile origin)
    {
        return GetAdjacentTile(origin, Direction.North);
    }

    public Tile GetSouthTile(Tile origin)
    {
        return GetAdjacentTile(origin, Direction.South);
    }

    public Tile GetEastTile(Tile origin)
    {
        return GetAdjacentTile(origin, Direction.East);
    }

    public Tile GetWestTile(Tile origin)
    {
        return GetAdjacentTile(origin, Direction.West);
    }

    public void ResetTileSearch()
    {
        for (int x = 0; x < Globals.COMBAT_WIDTH; x++)
        {
            for (int y = 0; y < Globals.COMBAT_HEIGHT; y++)
            {
                tileGrid[x, y].ResetSearch();
            }
        }
    }

}
