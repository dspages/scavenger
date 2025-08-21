using UnityEngine;
using System.Collections.Generic;

public class Tile : MonoBehaviour
{
    [SerializeField] public int x, y;
    [SerializeField] private int moveCost;

    public CombatController occupant = null;
    public bool isWalkable = true;

    public bool isHovered = false;

    public bool blocksVision = false;

    // Needed for breadth-first search
    public bool searchWasVisited = false;
    public bool searchCanBeChosen = false;
    public Tile searchParent = null;
    public int searchDistance = 0;

    private TileManager manager;

    // Use this for initialization
    void Start()
    {
        manager = GameObject.FindObjectOfType<TileManager>();
    }

    public List<Tile> Neighbors()
    {
        List<Tile> l = new List<Tile>();
        if (manager.GetNorthTile(this)) l.Add(manager.GetNorthTile(this));
        if (manager.GetEastTile(this)) l.Add(manager.GetEastTile(this));
        if (manager.GetWestTile(this)) l.Add(manager.GetWestTile(this));
        if (manager.GetSouthTile(this)) l.Add(manager.GetSouthTile(this));
        return l;
    }

    public int GetMoveCost()
    {
        return moveCost;
    }

    // Update is called once per frame
    void Update()
    {
        if (isHovered) GetComponent<Renderer>().material.color = Color.magenta;
        else if (searchCanBeChosen)
        {
            if (occupant != null)
                GetComponent<Renderer>().material.color = Color.red;
            else
                GetComponent<Renderer>().material.color = Color.green;
        }
        else GetComponent<Renderer>().material.color = Color.white;
    }

    public void ResetSearch()
    {
        isHovered = false;
        searchCanBeChosen = false;
        searchWasVisited = false;
        searchParent = null;
        searchDistance = 0;
    }

}