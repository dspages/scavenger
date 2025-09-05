using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : CombatController
{
    private Tile hoverTile = null;
    private TooltipManager tooltipManager;
    private PathRenderer pathRenderer;

    override public bool ContainsEnemy(Tile tile)
    {
        if (tile.occupant == null) return false;
        return tile.occupant.IsEnemy();
    }

    override public bool IsPC()
    {
        return true;
    }

    override protected bool DoesGUI()
    {
        return true;
    }

    override protected void Start()
    {
        base.Start();
        tooltipManager = FindObjectOfType<TooltipManager>();
        // Ensure a PathRenderer exists for drawing paths
        pathRenderer = FindObjectOfType<PathRenderer>();
        if (pathRenderer == null)
        {
            GameObject pr = new GameObject("PathRenderer");
            pathRenderer = pr.AddComponent<PathRenderer>();
        }
    }

    void Update()
    {
        if (!isTurn)
        {
            return;
        }
        if (!isActing)
        {
            if (UIClickBlocker.IsPointerOverBlockingUI())
            {
                // Ensure world hover visuals/tooltips are cleared when over UI
                ClearMouseHover();
                return;
            }
            CheckMouse();
            // Right-click to reset to default attack action
            if (Input.GetMouseButtonUp(1))
            {
                ResetSelectedActionToDefault();
            }
        }
    }

    public void EndTurnButtonClick()
    {
        if (!isTurn) return;
        EndTurn();
    }



    private Tile GetMouseTile()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

        if (hit.collider != null && hit.collider.GetComponent<Tile>() != null)
        {
            Tile mouseTile = hit.collider.GetComponent<Tile>();
            return mouseTile;
        }
        return null;
    }

    // Use PathRenderer to draw the path segments (moved out of PlayerController)
    void LineBetweenPositions(Vector3 start, Vector3 end, PathRenderer.LineType lineType = PathRenderer.LineType.MovementPath)
    {
        if (pathRenderer != null)
        {
            pathRenderer.DrawSegment(start, end, lineType);
        }
    }


// PathDashAnimator moved to PathRenderer.cs

    void ClearMouseHover()
    {
        // Clear all hovered tiles (including AoE highlights)
        TileManager tileManager = FindObjectOfType<TileManager>();
        if (tileManager != null)
        {
            // Clear all tile hover states
            for (int x = 0; x < Globals.COMBAT_WIDTH; x++)
            {
                for (int y = 0; y < Globals.COMBAT_HEIGHT; y++)
                {
                    Tile tile = tileManager.getTile(x, y);
                    if (tile != null && tile.isHovered)
                    {
                        tile.isHovered = false;
                    }
                }
            }
        }
        
        if (pathRenderer != null)
        {
            pathRenderer.ClearAll();
        }
        
        // Stop tooltip for the previous hovered tile
        if (tooltipManager != null)
        {
            tooltipManager.StopHover();
        }
        
        hoverTile = null;
    }

    private void SetMouseHover()
    {
        hoverTile = GetMouseTile();
        if (hoverTile == null) return;
        hoverTile.isHovered = true;
        
        // Only render the path if this tile is actually reachable
        if (hoverTile.searchCanBeChosen)
        {
            if (selectedAction != null)
            {
                bool isGround = selectedAction.TARGET_TYPE == Action.TargetType.GROUND_TILE || (selectedAction is ActionAttack atk && atk.AOE_RADIUS > 0);
                bool isRanged = selectedAction is ActionRangedAttack || selectedAction.TARGET_TYPE == Action.TargetType.RANGED;
                if (isGround || (isRanged && hoverTile.occupant != null && ContainsEnemy(hoverTile)))
                {
                    int aoeRadius = 0;
                    if (selectedAction is ActionAttack aa)
                    {
                        aoeRadius = aa.AOE_RADIUS;
                    }
                    AttackPreviewHelper.DrawCompositeAttackPreview(pathRenderer, hoverTile, GetCurrentTile(), isGround ? aoeRadius : 0);
                }
                else
                {
                    // Normal movement path
                    Tile t = hoverTile;
                    while (t.searchParent)
                    {
                        LineBetweenPositions(t.transform.position, t.searchParent.transform.position);
                        t = t.searchParent;
                    }
                }
            }
        }
        
        // Start tooltip for the new hovered tile
        if (tooltipManager != null)
        {
            tooltipManager.StartHover(hoverTile.gameObject);
        }
    }

    private void ShowGroundTargetPreview(Tile targetTile, ActionGroundAttack groundAttack)
    {
        int radius = groundAttack != null ? groundAttack.radius : 0;
        AttackPreviewHelper.DrawCompositeAttackPreview(pathRenderer, targetTile, GetCurrentTile(), radius);
    }

    // Draw movement path to the launch tile (closest tile from which the action can hit the target),
    // then draw a projectile/spell line from the launch tile to the target. If ground attack, also show AoE.
    private void ShowCompositeAttackPreview(Tile targetTile, int aoeRadius)
    {
        AttackPreviewHelper.DrawCompositeAttackPreview(pathRenderer, targetTile, GetCurrentTile(), aoeRadius);
    }

    private void HighlightAoETiles(Tile center, int radius)
    {
        // Use the same radius calculation as ActionGroundAttack
        Queue<Tile> queue = new Queue<Tile>();
        HashSet<Tile> visited = new HashSet<Tile>();
        Dictionary<Tile, int> depth = new Dictionary<Tile, int>();
        
        queue.Enqueue(center);
        visited.Add(center);
        depth[center] = 0;

        while (queue.Count > 0)
        {
            Tile t = queue.Dequeue();
            int d = depth[t];
            
            // Mark this tile as affected (visual indication)
            t.isHovered = true; // Reuse existing hover visual for now
            
            if (d >= radius) continue;
            
            foreach (Tile n in t.Neighbors())
            {
                if (n == null || visited.Contains(n)) continue;
                visited.Add(n);
                depth[n] = d + 1;
                queue.Enqueue(n);
            }
        }
    }

    private void CheckMouse()
    {
        Tile mouseTile = GetMouseTile();
        if (mouseTile != null)
        {
            if (mouseTile != hoverTile)
            {
                // Clear old hover and path before setting new one
                if (hoverTile != null) 
                {
                    hoverTile.isHovered = false;
                    ClearMouseHover(); // Clear old path
                }
                hoverTile = mouseTile;
                hoverTile.isHovered = true;
                
                // Render the movement path when hovering over a new tile
                SetMouseHover();
            }
            
            // Handle mouse clicks
            if (Input.GetMouseButtonUp(0))
            {
                Tile clickedTile = mouseTile;
                if (clickedTile == null || !clickedTile.searchCanBeChosen) return;
                ClearMouseHover();
                if (clickedTile.occupant != null)
                {
                    // If an enemy is clicked, run selected action (melee/ranged/etc.)
                    selectedAction.BeginAction(clickedTile);
                    return;
                }
                else
                {
                    // If a ground-target action is selected, and it's a ground tile, cast it
                    if (selectedAction != null && selectedAction.TARGET_TYPE == Action.TargetType.GROUND_TILE)
                    {
                        selectedAction.BeginAction(clickedTile);
                    }
                    else
                    {
                        // Otherwise move
                        Action move = GetComponent<ActionMove>();
                        move.BeginAction(clickedTile);
                    }
                    return;
                }
            }
        }
        else
        {
            if (hoverTile != null)
            {
                hoverTile.isHovered = false;
                hoverTile = null;
                ClearMouseHover(); // Clear path when not hovering over anything
            }
        }
    }
}
