using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mixin for special moves used in PlayerController and EnemyController.
public partial class CombatController
{
    private bool ActivateSelfSpecialMove(Action action, bool displayReason)
    {
        // For PCs, simply begin the action.
        if (displayReason) action.BeginAction(null);
        else selectedAction = action;
        return true;
    }

    private bool TargetChargeSpecialMove(Action action, bool displayReason)
    {
        selectedAction = action;
        FindSelectableChargeTiles();
        if (selectableTiles != null && selectableTiles.Count > 0) return true;
        if (displayReason) characterSheet.DisplayPopup("Nothing in range");
        return false;
    }

    private bool TargetMeleeSpecialMove(Action action, bool displayReason)
    {
        selectedAction = action;
        FindSelectableMeleeAttackTiles();
        if (selectableTiles != null && selectableTiles.Count > 0) return true;
        if (displayReason) characterSheet.DisplayPopup("Nothing in range");
        return false;
    }

    private bool TargetRangedSpecialMove(Action action, bool displayReason)
    {
        selectedAction = action;
        FindSelectableRangedAttackTiles();
        if (selectableTiles != null && selectableTiles.Count > 0) return true;
        if (displayReason) characterSheet.DisplayPopup("Nothing in range");
        return false;
    }

    private bool TargetAllyBuffSpecialMove(Action action, bool displayReason)
    {
        selectedAction = action;
        FindSelectableAllyBuffTiles();
        if (selectableTiles != null && selectableTiles.Count > 0) return true;
        if (displayReason) characterSheet.DisplayPopup("Nothing in range");
        return false;
    }

    private bool TargetMeleeReachSpecialMove(Action action, bool displayReason)
    {
        selectedAction = action;
        FindSelectableMeleeReachAttackTiles();
        if (selectableTiles != null && selectableTiles.Count > 0) return true;
        if (displayReason) characterSheet.DisplayPopup("Nothing in range");
        return false;
    }

    private bool TargetGroundAttackSpecialMove(Action action, bool displayReason)
    {
        selectedAction = action;
        FindSelectableGroundAttackTiles();
        if (selectableTiles != null && selectableTiles.Count > 0) return true;
        if (displayReason) characterSheet.DisplayPopup("No valid tiles");
        return false;
    }

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
        return true;
    }

    // Returns false if target selection fails.
    protected bool FindAllValidTargets(Action action, bool displayReason)
    {
        switch (action.TARGET_TYPE)
        {
            case Action.TargetType.SELF_ONLY:
                return ActivateSelfSpecialMove(action, displayReason);
            case Action.TargetType.CHARGE:
                return TargetChargeSpecialMove(action, displayReason);
            case Action.TargetType.MELEE:
                return TargetMeleeSpecialMove(action, displayReason);
            case Action.TargetType.RANGED:
                return TargetRangedSpecialMove(action, displayReason);
            case Action.TargetType.SELF_OR_ALLY:
                return TargetAllyBuffSpecialMove(action, displayReason);
            case Action.TargetType.MELEE_REACH:
                return TargetMeleeReachSpecialMove(action, displayReason);
            case Action.TargetType.GROUND_TILE:
                return TargetGroundAttackSpecialMove(action, displayReason);
        }
        return false;
    }
}