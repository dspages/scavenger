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
        
        if (character.weaponEquipped != null)
        {
            text += $"\nWeapon: {character.weaponEquipped.itemName}";
        }
        
        if (combatController.IsPC())
        {
            text += "\nType: Player Character";
        }
        else
        {
            text += "\nType: Enemy";
        }
        
        return text;
    }
}