using UnityEngine;

public class TileTooltipProvider : TooltipProvider
{
    private Tile tile;
    
    protected override void Start()
    {
        base.Start();
        tile = GetComponent<Tile>();
    }
    
    protected override string GenerateDynamicText()
    {
        if (tile == null) return base.GenerateDynamicText();
        
        string text = $"Tile ({tile.x}, {tile.y})\n";
        text += $"Move Cost: {tile.GetMoveCost()}\n";
        text += $"Walkable: {(tile.isWalkable ? "Yes" : "No")}";
        
        if (tile.occupant != null)
        {
            text += $"\nOccupant: {tile.occupant.characterSheet.firstName}";
            if (tile.occupant.IsPC())
            {
                text += " (Player)";
            }
            else
            {
                text += " (Enemy)";
            }
        }
        
        if (tile.searchCanBeChosen)
        {
            text += "\nSelectable";
            if (tile.searchDistance > 0)
            {
                text += $" (Distance: {tile.searchDistance})";
            }
        }
        
        return text;
    }
}