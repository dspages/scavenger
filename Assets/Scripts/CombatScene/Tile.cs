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

    // Vision system properties
    public bool isFogged = true;
    public bool isIlluminated = false;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    // Needed for breadth-first search
    public bool searchWasVisited = false;
    public bool searchCanBeChosen = false;
    public Tile searchParent = null;
    public int searchDistance = 0;

    private TileManager manager;
    
    // Visual state configuration
    [Header("Visual Settings")]
    [SerializeField] private float fogDarkeningFactor = 0.4f; // How much to darken fogged tiles
    [SerializeField, Range(0f, 1f)] private float illuminationBoost = 0.25f; // Extra brighten for visible+illuminated
    [SerializeField, Range(0.5f, 1f)] private float baseVisibleDim = 0.9f; // Dim non-illuminated visible tiles slightly
    [SerializeField] private Color selectionTintColor = new Color(0.85f, 0.95f, 1f, 0.15f); // Subtle unified tint
    
    [Header("Hover Icon Settings")]
    [SerializeField] private Color hoverIconColor = new Color(1f, 1f, 1f, 0.9f); // White with slight transparency
    [SerializeField] private float hoverIconScale = 0.3f; // Size relative to tile
    [SerializeField] private bool useDropShadow = true; // Add drop shadow for better visibility
    
    // Hover icon management
    private GameObject hoverIcon;
    
    // Icon options for easy customization
    public enum HoverIconType
    {
        Crosshair,     // ⊕
        Target,        // ⌖
        Circle,        // ◯
        X,             // ×
        Plus,          // ✚
        Diamond,       // ◆
        Triangle,      // ▲
        FilledCircle   // ●
    }
    
    [SerializeField] private HoverIconType iconType = HoverIconType.Plus;

    [Header("Selection Outline Settings")]
    [SerializeField] private Color outlineColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private float outlineWidth = 0.04f;
    [SerializeField] private int outlineSortingOrder = 9; // Below hover icon (10), above tile

    // Outline renderers per edge
    private GameObject outlineContainer;
    private LineRenderer outlineNorth;
    private LineRenderer outlineSouth;
    private LineRenderer outlineEast;
    private LineRenderer outlineWest;

    // Use this for initialization
    void Start()
    {
        manager = GameObject.FindObjectOfType<TileManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
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
        spriteRenderer.color = CalculateFinalTileColor();
        UpdateHoverIcon();
        UpdateSelectionOutlines();
    }
    
    /// <summary>
    /// Calculate the final color for this tile based on all its visual states.
    /// Uses smart color combination logic instead of arbitrary stacking.
    /// </summary>
    private Color CalculateFinalTileColor()
    {
        // Start with the base tile color
        Color finalColor = originalColor;
        
        // Apply fog effect first (darkening)
        if (isFogged)
        {
            finalColor = Color.Lerp(finalColor, Color.black, fogDarkeningFactor);
        }
        
        // Apply illumination/visibility brightness tiers
        if (!isFogged)
        {
            if (isIlluminated)
            {
                // Visible and illuminated: brighten towards white
                finalColor = Color.Lerp(finalColor, Color.white, illuminationBoost);
            }
            else
            {
                // Visible but not illuminated: slightly dim to make illuminated pop
                finalColor = new Color(
                    finalColor.r * baseVisibleDim,
                    finalColor.g * baseVisibleDim,
                    finalColor.b * baseVisibleDim,
                    1f
                );
            }
        }
        
        // Apply interaction highlights (hover is now handled by icon, not color)
        if (searchCanBeChosen)
        {
            // Use a unified subtle selection tint
            finalColor = BlendOverlay(finalColor, selectionTintColor);
        }
        
        // Ensure alpha is always 1 (we don't want transparent tiles)
        finalColor.a = 1f;
        
        return finalColor;
    }
    
    /// <summary>
    /// Overlay blending for subtle target indicators
    /// </summary>
    private Color BlendOverlay(Color baseColor, Color overlayColor)
    {
        float alpha = overlayColor.a;
        return new Color(
            Mathf.Lerp(baseColor.r, overlayColor.r, alpha),
            Mathf.Lerp(baseColor.g, overlayColor.g, alpha),
            Mathf.Lerp(baseColor.b, overlayColor.b, alpha),
            1f
        );
    }
    
    /// <summary>
    /// Updates the hover icon visibility and appearance
    /// </summary>
    private void UpdateHoverIcon()
    {
        if (isHovered)
        {
            if (hoverIcon == null)
            {
                CreateHoverIcon();
            }
            hoverIcon.SetActive(true);
        }
        else
        {
            if (hoverIcon != null)
            {
                hoverIcon.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Creates the hover icon GameObject with text renderer
    /// </summary>
    private void CreateHoverIcon()
    {
        hoverIcon = new GameObject($"HoverIcon_{x}_{y}");
        hoverIcon.transform.SetParent(transform, false);
        
        // Position the icon at the center of the tile, slightly above
        hoverIcon.transform.localPosition = new Vector3(0, 0, -0.1f);
        hoverIcon.transform.localScale = Vector3.one * hoverIconScale;
        
        // Create drop shadow if enabled
        if (useDropShadow)
        {
            GameObject shadowIcon = new GameObject("Shadow");
            shadowIcon.transform.SetParent(hoverIcon.transform, false);
            shadowIcon.transform.localPosition = new Vector3(0.1f, -0.1f, 0.01f);
            
            TextMesh shadowText = shadowIcon.AddComponent<TextMesh>();
            shadowText.text = GetIconText();
            shadowText.fontSize = 30;
            shadowText.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black shadow
            shadowText.anchor = TextAnchor.MiddleCenter;
            shadowText.alignment = TextAlignment.Center;
            
            MeshRenderer shadowRenderer = shadowIcon.GetComponent<MeshRenderer>();
            shadowRenderer.sortingOrder = 9; // Behind main icon
        }
        
        // Add TextMesh component for the main icon
        TextMesh textMesh = hoverIcon.AddComponent<TextMesh>();
        textMesh.text = GetIconText();
        textMesh.fontSize = 30; // Large font size, scaled down by transform
        textMesh.color = hoverIconColor;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        // Set rendering order to appear above tile but below UI
        MeshRenderer meshRenderer = hoverIcon.GetComponent<MeshRenderer>();
        meshRenderer.sortingOrder = 10; // High sorting order to appear above everything else
        
        // Start inactive
        hoverIcon.SetActive(false);
    }
    
    /// <summary>
    /// Gets the appropriate icon text based on the selected icon type
    /// </summary>
    private string GetIconText()
    {
        switch (iconType)
        {
            case HoverIconType.Crosshair: return "⊕";
            case HoverIconType.Target: return "⌖";
            case HoverIconType.Circle: return "◯";
            case HoverIconType.X: return "×";
            case HoverIconType.Plus: return "✚";
            case HoverIconType.Diamond: return "◆";
            case HoverIconType.Triangle: return "▲";
            case HoverIconType.FilledCircle: return "●";
            default: return "⊕";
        }
    }
    
    /// <summary>
    /// Updates or creates selection outline edges on the outer boundary of selectable tiles
    /// - Draw edges only where the neighbor is NOT selectable
    /// - Do not draw edges where neighbor is selectable or blocked by friendly unit
    /// </summary>
    private void UpdateSelectionOutlines()
    {
        EnsureOutlineRenderers();

        bool show = searchCanBeChosen;

        // Early hide if not selectable
        if (!show)
        {
            SetOutlineActive(false);
            return;
        }

        // Determine per-edge visibility based on neighbors
        bool drawNorth = ShouldDrawEdge(manager.GetNorthTile(this));
        bool drawSouth = ShouldDrawEdge(manager.GetSouthTile(this));
        bool drawEast = ShouldDrawEdge(manager.GetEastTile(this));
        bool drawWest = ShouldDrawEdge(manager.GetWestTile(this));

        // Activate container
        if (outlineContainer != null && !outlineContainer.activeSelf) outlineContainer.SetActive(true);

        // Tile local corners (assuming 1x1 tiles centered on transform)
        Vector3 topLeft = new Vector3(-1f, 1f, 0f);
        Vector3 topRight = new Vector3(1f, 1f, 0f);
        Vector3 bottomLeft = new Vector3(-1f, -1f, 0f);
        Vector3 bottomRight = new Vector3(1f, -1f, 0f);

        // Apply to each edge
        ApplyEdge(outlineNorth, drawNorth, topLeft, topRight);
        ApplyEdge(outlineSouth, drawSouth, bottomRight, bottomLeft);
        ApplyEdge(outlineEast, drawEast, topRight, bottomRight);
        ApplyEdge(outlineWest, drawWest, bottomLeft, topLeft);
    }

    private bool ShouldDrawEdge(Tile neighbor)
    {
        // Draw edge if neighbor is null (off-map)
        if (neighbor == null) return true;

        // Skip edge if neighbor is also selectable (internal border)
        if (neighbor.searchCanBeChosen) return false;

        // Skip edge if neighbor is blocked by friendly unit
        if (neighbor.occupant != null && occupant != null && neighbor.occupant.IsPC() == occupant.IsPC())
            return false;

        // Otherwise draw edge
        return true;
    }

    private void EnsureOutlineRenderers()
    {
        if (outlineContainer == null)
        {
            outlineContainer = new GameObject($"Outline_{x}_{y}");
            outlineContainer.transform.SetParent(transform, false);
            outlineContainer.transform.localPosition = new Vector3(0f, 0f, -0.11f);
        }

        if (outlineNorth == null) outlineNorth = CreateEdgeRenderer("North");
        if (outlineSouth == null) outlineSouth = CreateEdgeRenderer("South");
        if (outlineEast == null) outlineEast = CreateEdgeRenderer("East");
        if (outlineWest == null) outlineWest = CreateEdgeRenderer("West");
    }

    private LineRenderer CreateEdgeRenderer(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(outlineContainer.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.startWidth = outlineWidth;
        lr.endWidth = outlineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = outlineColor;
        lr.endColor = outlineColor;
        lr.sortingOrder = outlineSortingOrder;
        lr.numCapVertices = 2;
        lr.enabled = false;
        return lr;
    }

    private void ApplyEdge(LineRenderer lr, bool draw, Vector3 start, Vector3 end)
    {
        if (lr == null) return;
        lr.enabled = draw;
        if (draw)
        {
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }
    }

    private void SetOutlineActive(bool active)
    {
        if (outlineContainer != null) outlineContainer.SetActive(active);
    }
    

    
    /// <summary>
    /// Cleanup hover icon when tile is destroyed
    /// </summary>
    void OnDestroy()
    {
        if (hoverIcon != null)
        {
            DestroyImmediate(hoverIcon);
        }
    }
    
    public void SetFogged(bool fogged)
    {
        isFogged = fogged;
    }
    
    public void SetIlluminated(bool illuminated)
    {
        isIlluminated = illuminated;
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