using UnityEngine;

public class IlluminationSource : MonoBehaviour
{
    [Header("Illumination Settings")]
    [SerializeField] public int illuminationRange = 3;
    [SerializeField] private bool isActive = true;
    [SerializeField] public bool isMovable = false; // For torches held by characters
    
    private VisionSystem visionSystem;
    private TileManager tileManager;
    private Vector3 lastIlluminationPosition; // Position where illumination was last applied
    private Vector2Int lastTilePosition; // Tile coordinates where illumination was last applied
    
    void Start()
    {
        visionSystem = FindObjectOfType<VisionSystem>();
        tileManager = FindObjectOfType<TileManager>();
        lastIlluminationPosition = transform.position;
        lastTilePosition = new Vector2Int(
            Mathf.RoundToInt(transform.position.x), 
            Mathf.RoundToInt(transform.position.y)
        );
        
        if (isActive)
        {
            IlluminateSurroundingTiles();
        }
    }
    
    void Update()
    {
        // If this is a movable light source, check if we've moved to a different tile
        if (isMovable && isActive)
        {
            Vector2Int currentTilePosition = new Vector2Int(
                Mathf.RoundToInt(transform.position.x), 
                Mathf.RoundToInt(transform.position.y)
            );
            
            // Only update illumination when we actually change tiles, not during smooth movement animation
            if (currentTilePosition != lastTilePosition)
            {
                // Remove old illumination at the previous tile position
                RemoveIlluminationAt(lastIlluminationPosition);
                // Add new illumination at current tile position
                IlluminateSurroundingTiles();
                lastTilePosition = currentTilePosition;
            }
        }
    }
    
    public void SetActive(bool active)
    {
        if (isActive != active)
        {
            isActive = active;
            if (isActive)
            {
                IlluminateSurroundingTiles();
            }
            else
            {
                RemoveIllumination();
            }
        }
    }
    
    private void IlluminateSurroundingTiles()
    {
        if (visionSystem == null || tileManager == null) return;
        
        Vector3 position = transform.position;
        int centerX = Mathf.RoundToInt(position.x);
        int centerY = Mathf.RoundToInt(position.y);
        
        for (int x = centerX - illuminationRange; x <= centerX + illuminationRange; x++)
        {
            for (int y = centerY - illuminationRange; y <= centerY + illuminationRange; y++)
            {
                int distance = Mathf.Abs(x - centerX) + Mathf.Abs(y - centerY);
                if (distance <= illuminationRange)
                {
                    if (x >= 0 && x < Globals.COMBAT_WIDTH && y >= 0 && y < Globals.COMBAT_HEIGHT)
                    {
                        Tile tile = tileManager.getTile(x, y);
                        if (tile != null)
                        {
                            visionSystem.SetTileIllumination(tile, true);
                        }
                    }
                }
            }
        }
        // Store where we applied illumination for future cleanup
        lastIlluminationPosition = position;
        
        // After batch illumination changes, refresh vision so fog and HIDDEN states update
        if (visionSystem != null)
        {
            visionSystem.UpdateVision();
        }
    }
    
    private void RemoveIllumination()
    {
        RemoveIlluminationAt(lastIlluminationPosition);
    }

    private void RemoveIlluminationAt(Vector3 position)
    {
        if (visionSystem == null || tileManager == null) return;

        int centerX = Mathf.RoundToInt(position.x);
        int centerY = Mathf.RoundToInt(position.y);

        for (int x = centerX - illuminationRange; x <= centerX + illuminationRange; x++)
        {
            for (int y = centerY - illuminationRange; y <= centerY + illuminationRange; y++)
            {
                int distance = Mathf.Abs(x - centerX) + Mathf.Abs(y - centerY);
                if (distance <= illuminationRange)
                {
                    if (x >= 0 && x < Globals.COMBAT_WIDTH && y >= 0 && y < Globals.COMBAT_HEIGHT)
                    {
                        Tile tile = tileManager.getTile(x, y);
                        if (tile != null)
                        {
                            visionSystem.SetTileIllumination(tile, false);
                        }
                    }
                }
            }
        }
        // After removing illumination, refresh vision
        if (visionSystem != null)
        {
            visionSystem.UpdateVision();
        }
    }
    
    void OnDestroy()
    {
        // Remove illumination when this object is destroyed
        if (isActive)
        {
            RemoveIllumination();
        }
    }
}
