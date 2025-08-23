using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : CombatController
{
    private Tile hoverTile = null;
    private TooltipManager tooltipManager;
    private PathRenderer pathRenderer;

    override protected bool ContainsEnemy(Tile tile)
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
    void LineBetweenPositions(Vector3 start, Vector3 end)
    {
        if (pathRenderer != null)
        {
            pathRenderer.DrawSegment(start, end);
        }
    }


// PathDashAnimator moved to PathRenderer.cs

    void ClearMouseHover()
    {
        if (hoverTile != null) hoverTile.isHovered = false;
        foreach (GameObject line in GameObject.FindGameObjectsWithTag("LineTag"))
        {
            Destroy(line);
        }
        
        // Stop tooltip for the previous hovered tile
        if (tooltipManager != null)
        {
            tooltipManager.StopHover();
        }
    }

    private void SetMouseHover()
    {
        hoverTile = GetMouseTile();
        if (hoverTile == null) return;
        hoverTile.isHovered = true;
        
        // Only render the path if this tile is actually reachable
        if (hoverTile.searchCanBeChosen)
        {
            Tile t = hoverTile;
            while (t.searchParent)
            {
                LineBetweenPositions(t.transform.position, t.searchParent.transform.position);
                t = t.searchParent;
            }
        }
        
        // Start tooltip for the new hovered tile
        if (tooltipManager != null)
        {
            tooltipManager.StartHover(hoverTile.gameObject);
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
                    selectedAction.BeginAction(clickedTile);
                    return;
                }
                else
                {
                    Action move = GetComponent<ActionMove>();
                    move.BeginAction(clickedTile);
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
