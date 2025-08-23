using UnityEngine;

public class TileTooltipProvider : TooltipProvider
{
    private Tile tile;
    private VisionSystem visionSystem;
    
    protected override void Start()
    {
        base.Start();
        tile = GetComponent<Tile>();
        visionSystem = FindObjectOfType<VisionSystem>();
    }
    
    protected override string GenerateDynamicText()
    {
        if (tile == null) return base.GenerateDynamicText();
        
        string text = $"Tile ({tile.x}, {tile.y})\n";
        text += $"Move Cost: {tile.GetMoveCost()}\n";
        text += $"Walkable: {(tile.isWalkable ? "Yes" : "No")}";
        
        // Add fog of war debug information
        if (visionSystem != null)
        {
            bool isInitialized = visionSystem.IsInitialized();
            bool isVisible = visionSystem.IsTileVisible(tile);
            bool isIlluminated = visionSystem.IsTileIlluminated(tile);
            text += $"\nVisionSystem: {(isInitialized ? "Ready" : "Initializing...")}";
            text += $"\nFog Status: {(isVisible ? "Visible" : "Fogged")}";
            text += $"\nIlluminated: {(isIlluminated ? "Yes" : "No")}";
        }
        else
        {
            text += "\nFog Status: VisionSystem not found";
        }
        
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