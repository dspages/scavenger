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
    }

    public Tile getTile(int xCoord, int yCoord)
    {
        return tileGrid[xCoord, yCoord];
    }

    public Tile GetNorthTile(Tile origin)
    {
        if (origin.y + 1 >= Globals.COMBAT_HEIGHT) return null;
        return tileGrid[origin.x, origin.y + 1];
    }

    public Tile GetSouthTile(Tile origin)
    {
        if (origin.y - 1 < 0 ) return null;
        return tileGrid[origin.x, origin.y - 1];
    }

    public Tile GetEastTile(Tile origin)
    {
        if (origin.x + 1 >= Globals.COMBAT_HEIGHT) return null;
        return tileGrid[origin.x + 1, origin.y];
    }

    public Tile GetWestTile(Tile origin)
    {
        if (origin.x - 1 < 0) return null;
        return tileGrid[origin.x - 1, origin.y];
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
