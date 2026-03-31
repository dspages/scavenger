using UnityEngine;

public class TileTooltipProvider : TooltipProvider
{
    private Tile tile;
    private VisionSystem visionSystem;
    
    protected override void Start()
    {
        base.Start();
        tile = GetComponent<Tile>();
        visionSystem = FindFirstObjectByType<VisionSystem>(FindObjectsInactive.Exclude);
    }
    
    protected override string GenerateDynamicText()
    {
        if (tile == null) return base.GenerateDynamicText();
        
        string text = $"Tile ({tile.x}, {tile.y})\n";
        text += $"Walkable: {(tile.isWalkable ? "Yes" : "No")}";
        
        // Add fog of war information (without internal initialization status)
        if (visionSystem != null)
        {
            bool isVisible = visionSystem.IsTileVisible(tile);
            bool isIlluminated = visionSystem.IsTileIlluminated(tile);
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
                text += GetHitChanceText(tile.occupant);
            }
        }
        
        if (tile.searchCanBeChosen)
        {
            text += "\nSelectable";
            if (tile.searchDistance > 0)
            {
                text += $" (AP Cost: {tile.searchDistance})";
            }
        }

        var activePc = FindActivePlayer();
        if (activePc != null && tile.searchCanBeChosen && !tile.searchHardCostsAffordable)
            text += "\n<color=#FF8800>Cannot pay costs for this action (ammo / resources).</color>";

        if (activePc != null)
        {
            var action = activePc.GetSelectedAction();
            if (action is ActionAttack atk &&
                CombatActionAffordance.ShouldWarnSanityRisk(atk.GetAbilityDataForCosts(), activePc.characterSheet))
            {
                text += "\n<color=#FF8800>Warning: this action may cause insanity!</color>";
            }
        }

        return text;
    }

    private string GetHitChanceText(CombatController enemy)
    {
        var activePlayer = FindActivePlayer();
        if (activePlayer == null) return "";

        var action = activePlayer.GetSelectedAction();
        if (action == null || action.TARGET_TYPE != Action.TargetType.RANGED) return "";

        var playerSheet = activePlayer.characterSheet;
        if (playerSheet == null || enemy?.characterSheet == null) return "";

        var playerTile = activePlayer.GetCurrentTile();
        var enemyTile = enemy.GetCurrentTile();
        if (playerTile == null || enemyTile == null) return "";

        Tile launchTile = AttackLaunchTile.Resolve(enemyTile, playerTile);
        if (launchTile == null) return "";
        int dist = Mathf.Abs(launchTile.x - enemyTile.x) + Mathf.Abs(launchTile.y - enemyTile.y);
        var playerWeapon = playerSheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
        var context = AttackContext.Ranged(playerWeapon, dist);

        float hitChance = HitCalculator.CalculateHitChance(playerSheet, enemy.characterSheet, context);
        int pct = Mathf.RoundToInt(hitChance * 100f);
        return $"\nHit Chance: {pct}%";
    }

    private CombatController FindActivePlayer()
    {
        var turnManager = FindFirstObjectByType<TurnManager>(FindObjectsInactive.Exclude);
        if (turnManager == null) return null;

        var pcs = turnManager.AllLivingPCs();
        foreach (var pc in pcs)
        {
            if (pc.isTurn) return pc;
        }
        return null;
    }
}