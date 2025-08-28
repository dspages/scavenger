using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionSystem : MonoBehaviour
{
    private TileManager tileManager;
    private Dictionary<Tile, bool> tileVisibility = new Dictionary<Tile, bool>();
    private Dictionary<Tile, bool> tileIllumination = new Dictionary<Tile, bool>();
    
    // Vision cones for each unit
    private Dictionary<CombatController, List<Tile>> unitVisionCones = new Dictionary<CombatController, List<Tile>>();
    private bool isUpdatingVision = false;
    
    void Start()
    {
        StartCoroutine(InitializeVisionSystemWhenReady());
    }
    
    private IEnumerator InitializeVisionSystemWhenReady()
    {
        // Wait for TileManager to be available
        while (tileManager == null)
        {
            tileManager = FindObjectOfType<TileManager>();
            if (tileManager == null)
            {
                yield return new WaitForEndOfFrame();
            }
        }
        
        // Wait a few more frames to ensure all tiles are created
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        // Verify that tiles are actually available
        Tile testTile = tileManager.getTile(0, 0);
        while (testTile == null)
        {
            yield return new WaitForEndOfFrame();
            testTile = tileManager.getTile(0, 0);
        }
        
        InitializeVisionSystem();
    }
    
    private void InitializeVisionSystem()
    {
        if (tileManager == null)
        {
            Debug.LogError("VisionSystem: TileManager is null, cannot initialize vision system!");
            return;
        }
        
        // Initialize all tiles as fogged
        for (int x = 0; x < Globals.COMBAT_WIDTH; x++)
        {
            for (int y = 0; y < Globals.COMBAT_HEIGHT; y++)
            {
                Tile tile = tileManager.getTile(x, y);
                if (tile != null)
                {
                    tileVisibility[tile] = false;
                    tileIllumination[tile] = false;
                    // Set initial tile state directly instead of creating overlays
                    tile.SetFogged(true);
                    tile.SetIlluminated(false);
                }
            }
        }
        
        // Initialization complete

        // After vision system is ready, apply equipment-driven effects for already-spawned combatants
        CombatController[] allCombatants = FindObjectsOfType<CombatController>();
        foreach (CombatController cc in allCombatants)
        {
            cc.ApplyEquipmentEffects();
        }
    }
    
    // Safe method to add new tiles to the system (called during runtime if needed)
    public void AddTileToSystem(Tile tile)
    {
        if (tile != null && !tileVisibility.ContainsKey(tile))
        {
            tileVisibility[tile] = false;
            tileIllumination[tile] = false;
            // Set initial tile state directly instead of creating overlays
            tile.SetFogged(true);
            tile.SetIlluminated(false);
            Debug.Log($"VisionSystem: Added tile ({tile.x}, {tile.y}) to system");
        }
    }
    

    
    public void UpdateVision()
    {
        // Prevent re-entrant updates which can cause infinite recursion
        if (isUpdatingVision) return;

        if (tileManager == null || tileVisibility.Count == 0)
        {
            Debug.LogWarning("VisionSystem: Cannot update vision - system not yet initialized");
            return;
        }

        isUpdatingVision = true;
        try
        {
        
        // Clear all vision - create a copy of keys to avoid modification during enumeration
        List<Tile> tilesToUpdate = new List<Tile>(tileVisibility.Keys);
        foreach (Tile tile in tilesToUpdate)
        {
            tileVisibility[tile] = false;
        }
        
        // Clear vision cones
        unitVisionCones.Clear();
        
        // Get all combatants
        CombatController[] allCombatants = FindObjectsOfType<CombatController>();
        
        foreach (CombatController combatant in allCombatants)
        {
            if (combatant.Dead()) continue;
            
            // Unfog tiles containing friendly units
            if (combatant.IsPC())
            {
                UnfogTile(combatant.GetCurrentTile());
            }
            
            // Calculate vision cone for all units (for AI decision-making)
            List<Tile> visionCone = CalculateVisionCone(combatant);
            unitVisionCones[combatant] = visionCone;
            
            // Only apply vision cone effects to unfog tiles for PLAYER units
            if (combatant.IsPC())
            {
                foreach (Tile visibleTile in visionCone)
                {
                    if (tileVisibility.ContainsKey(visibleTile))
                    {
                        tileVisibility[visibleTile] = true;
                    }
                }
            }
            // Enemy vision cones are calculated but don't unfog tiles for the player
        }
        
        // Update tile visual states
        UpdateTileVisualStates();
        
        // Update illumination effects
        UpdateIlluminationEffects();
        
        // Debug: Count how many tiles are visible
        int visibleTileCount = 0;
        foreach (var kvp in tileVisibility)
        {
            if (kvp.Value) visibleTileCount++;
        }
        
        }
        finally
        {
            isUpdatingVision = false;
        }
    }
    
    private List<Tile> CalculateVisionCone(CombatController unit)
    {
        List<Tile> visibleTiles = new List<Tile>();
        Tile currentTile = unit.GetCurrentTile();
        
        if (currentTile == null) return visibleTiles;
        
        int baseVisionRange = unit.characterSheet.GetVisionRange();
        int illuminatedVisionRange = baseVisionRange * 2; // always check out to 2x
        
        // Get unit's facing direction (assuming they face the direction they last moved)
        Vector2 facingDirection = unit.GetFacingDirection();
        
        // Calculate vision cone (forward-facing arc)
        for (int x = -illuminatedVisionRange; x <= illuminatedVisionRange; x++)
        {
            for (int y = -illuminatedVisionRange; y <= illuminatedVisionRange; y++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(y);
                if (distance > illuminatedVisionRange) continue;
                
                // Check if tile is in the forward-facing cone (120 degree arc)
                Vector2 tileDirection = new Vector2(x, y).normalized;
                float angle = Vector2.Angle(facingDirection, tileDirection);
                if (angle > 60f) continue; // 120 degree cone
                
                int targetX = currentTile.x + x;
                int targetY = currentTile.y + y;
                
                if (targetX < 0 || targetX >= Globals.COMBAT_WIDTH || 
                    targetY < 0 || targetY >= Globals.COMBAT_HEIGHT)
                    continue;
                
                Tile targetTile = tileManager.getTile(targetX, targetY);
                if (targetTile == null) continue;
                if (!HasLineOfSight(currentTile, targetTile)) continue;

                // Visibility rules:
                // - If within baseVisionRange => always visible
                // - If > baseVisionRange and <= 2*baseVisionRange => visible only if tile is illuminated
                if (distance <= baseVisionRange)
                {
                    visibleTiles.Add(targetTile);
                }
                else
                {
                    bool illum = tileIllumination.ContainsKey(targetTile) && tileIllumination[targetTile];
                    if (illum)
                    {
                        visibleTiles.Add(targetTile);
                    }
                }
            }
        }
        
        return visibleTiles;
    }
    
    private bool HasLineOfSight(Tile from, Tile to)
    {
        return LineOfSightUtils.HasLineOfSight(from, to, tileManager);
    }
    
    private void UnfogTile(Tile tile)
    {
        if (tile != null && tileVisibility.ContainsKey(tile))
        {
            tileVisibility[tile] = true;
        }
    }
    
    public void SetTileIllumination(Tile tile, bool illuminated)
    {
        if (tile != null && tileIllumination.ContainsKey(tile))
        {
            tileIllumination[tile] = illuminated;
            // Set tile illumination state directly
            tile.SetIlluminated(illuminated);
        }
    }
    
    private void UpdateTileVisualStates()
    {
        // Update fog and visibility states for all tiles
        foreach (var kvp in tileVisibility)
        {
            Tile tile = kvp.Key;
            bool isVisible = kvp.Value;
            
            // Set tile fog state directly
            tile.SetFogged(!isVisible);
        }
        
        // Update illumination states for all tiles
        foreach (var kvp in tileIllumination)
        {
            Tile tile = kvp.Key;
            bool isIlluminated = kvp.Value;
            
            // Set tile illumination state directly
            tile.SetIlluminated(isIlluminated);
        }
    }
    
    private void UpdateIlluminationEffects()
    {
        // Cancel HIDDEN status effect for units in illuminated tiles
        CombatController[] allCombatants = FindObjectsOfType<CombatController>();
        
        foreach (CombatController combatant in allCombatants)
        {
            if (combatant.Dead()) continue;
            
            Tile currentTile = combatant.GetCurrentTile();
            if (currentTile != null && tileIllumination.ContainsKey(currentTile) && tileIllumination[currentTile])
            {
                // Remove HIDDEN status effect using the CharacterSheet method
                // Suppress notification to avoid recursive vision updates; we'll handle refresh after the loop
                combatant.characterSheet.RemoveStatusEffect(StatusEffect.EffectType.HIDDEN, notify: false);
            }
        }
        
        // Tile illumination states are updated by UpdateTileVisualStates()
    }
    
    // Call this method whenever you want to check for HIDDEN status changes
    public void CheckForHiddenStatusChanges()
    {
        // This method can be called from external systems to update vision
        // when HIDDEN status effects are added or removed
        UpdateVision();
    }
    
    public bool IsInitialized()
    {
        return tileManager != null && tileVisibility.Count > 0;
    }
    
    public bool IsTileVisible(Tile tile)
    {
        if (tile == null || tileVisibility.Count == 0) return false;
        return tileVisibility.ContainsKey(tile) && tileVisibility[tile];
    }
    
    public bool IsTileIlluminated(Tile tile)
    {
        if (tile == null || tileIllumination.Count == 0) return false;
        return tileIllumination.ContainsKey(tile) && tileIllumination[tile];
    }
    
    public List<Tile> GetUnitVisionCone(CombatController unit)
    {
        return unitVisionCones.ContainsKey(unit) ? unitVisionCones[unit] : new List<Tile>();
    }
    
    public bool CanSeeUnit(CombatController observer, CombatController target)
    {
        if (target.Dead()) return false;
        
        // Check if target has HIDDEN status effect
        if (StatusEffect.HasEffectType(ref target.characterSheet.statusEffects, StatusEffect.EffectType.HIDDEN))
        {
            // HIDDEN effect is canceled in illuminated tiles
            Tile targetTile = target.GetCurrentTile();
            if (targetTile != null && !IsTileIlluminated(targetTile))
            {
                return false;
            }
        }
        
        // Check if target is in observer's vision cone
        Tile targetTile2 = target.GetCurrentTile();
        if (targetTile2 != null && unitVisionCones.ContainsKey(observer))
        {
            return unitVisionCones[observer].Contains(targetTile2);
        }
        
        return false;
    }
    

}

