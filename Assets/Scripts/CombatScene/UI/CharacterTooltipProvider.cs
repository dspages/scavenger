using UnityEngine;

public class CharacterTooltipProvider : TooltipProvider
{
    private CombatController combatController;
    
    protected override void Start()
    {
        base.Start();
        combatController = GetComponent<CombatController>();
    }
    
    protected override string GenerateDynamicText()
    {
        if (combatController?.characterSheet == null) return base.GenerateDynamicText();
        
        CharacterSheet character = combatController.characterSheet;
        string text = $"Name: {character.firstName}\n";
        text += $"Level: {character.level}\n";
        text += $"Health: {character.currentHealth}/{character.MaxHealth()}\n";
        text += $"Move Points: {character.currentActionPoints}\n";
        
        var weapon = character.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
        if (weapon != null)
        {
            text += $"\nWeapon: {weapon.itemName}";
        }
        
        if (combatController.IsPC())
        {
            text += "\nType: Player Character";
        }
        else
        {
            text += "\nType: Enemy";
            text += GetHitChanceText();
        }
        
        return text;
    }

    private string GetHitChanceText()
    {
        var activePlayer = FindActivePlayer();
        if (activePlayer == null) return "";

        var action = activePlayer.GetSelectedAction();
        if (action == null || action.TARGET_TYPE != Action.TargetType.RANGED) return "";

        var playerSheet = activePlayer.characterSheet;
        if (playerSheet == null || combatController.characterSheet == null) return "";

        var playerTile = activePlayer.GetCurrentTile();
        var myTile = combatController.GetCurrentTile();
        if (playerTile == null || myTile == null) return "";

        Tile launchTile = AttackLaunchTile.Resolve(myTile, playerTile);
        if (launchTile == null) return "";
        int dist = Mathf.Abs(launchTile.x - myTile.x) + Mathf.Abs(launchTile.y - myTile.y);
        var playerWeapon = playerSheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
        var context = AttackContext.Ranged(playerWeapon, dist);

        float hitChance = HitCalculator.CalculateHitChance(playerSheet, combatController.characterSheet, context);
        int pct = Mathf.RoundToInt(hitChance * 100f);
        return $"\nHit Chance: {pct}%";
    }

    private CombatController FindActivePlayer()
    {
        var turnManager = GetComponentInParent<TurnManager>();
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>(FindObjectsInactive.Exclude);
        if (turnManager == null) return null;

        var pcs = turnManager.AllLivingPCs();
        foreach (var pc in pcs)
        {
            if (pc.isTurn) return pc;
        }
        return null;
    }
}
