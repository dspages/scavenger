using UnityEngine;

// Mixin for special moves used in PlayerController and EnemyController.
public partial class CombatController
{
    // Returns false if invalid special action (e.g. not enough mana).
    // Only call this for special abilities: basic attacks can be assumed to always be valid.
    protected bool IsValid(Action action, bool displayReason)
    {
        if (action.IsCoolingDown())
        {
            if (displayReason) characterSheet.DisplayPopup("Cooling down");
            return false;
        }
        if (characterSheet.currentActionPoints < action.BASE_ACTION_COST)
        {
            if (displayReason) characterSheet.DisplayPopup("Not enough AP");
            return false;
        }
        // Legacy mana pool is no longer a hard gate. Ability and attack costs are now:
        // - weapon ammo / consumable weapon stacks
        // - ability inventory costs (extra items, mana crystals, tech components)
        // - sanity (warning only; may go negative by design)
        if (action is ActionAttack atk)
        {
            var weapon = GetEquippedWeaponForSelectedAction();
            var ability = atk.GetAbilityDataForCosts();

            if (!CombatActionAffordance.CanAffordFullAttackAction(this, weapon, ability))
            {
                if (displayReason)
                {
                    // Prefer a specific message when we can infer it cheaply.
                    if (weapon != null && weapon.requiresAmmo && !string.IsNullOrEmpty(weapon.ammoType) &&
                        !CombatActionAffordance.CanAffordWeaponAttackCosts(this, weapon, ability))
                        characterSheet.DisplayPopup("Not enough ammunition");
                    else if (!CombatActionAffordance.CanAffordAbilityHardInventoryCosts(ability, characterSheet))
                        characterSheet.DisplayPopup("Not enough materials");
                    else
                        characterSheet.DisplayPopup("Cannot afford");
                }
                return false;
            }

            if (displayReason && CombatActionAffordance.ShouldWarnSanityRisk(ability, characterSheet))
                characterSheet.DisplayPopup("Warning: sanity will go negative");
        }
        if (action is ActionSelfCast selfCast)
        {
            var ability = selfCast.GetAbilityDataForCosts();
            if (ability != null && !CombatActionAffordance.CanAffordAbilityHardInventoryCosts(ability, characterSheet))
            {
                if (displayReason) characterSheet.DisplayPopup("Not enough materials");
                return false;
            }
            if (displayReason && ability != null && CombatActionAffordance.ShouldWarnSanityRisk(ability, characterSheet))
                characterSheet.DisplayPopup("Warning: sanity will go negative");
        }
        return true;
    }
}
