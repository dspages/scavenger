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
        if (FindSelectableChargeTiles(action.BASE_ACTION_COST, action.minRange, action.maxRange))
        {
            selectedAction = action;
            return true;
        }
        if (displayReason) characterSheet.DisplayPopup("Nothing in range");
        return false;
    }

    private bool TargetMeleeSpecialMove(Action action, bool displayReason)
    {
        if (FindSelectableMeleeAttackTiles(action.BASE_ACTION_COST))
        {
            selectedAction = action;
            return true;
        }
        if (displayReason) characterSheet.DisplayPopup("Nothing in range");
        return false;
    }

    private bool TargetRangedSpecialMove(Action action, bool displayReason, int minRange, int maxRange)
    {
        if (FindSelectableRangedAttackTiles(action.BASE_ACTION_COST))
        {
            selectedAction = action;
            return true;
        }
        if (displayReason) characterSheet.DisplayPopup("Nothing in range");
        return false;
    }

    private bool TargetAllyBuffSpecialMove(Action action, bool displayReason, int minRange, int maxRange)
    {
        if (FindSelectableAllyBuffTiles(action.BASE_ACTION_COST, action.minRange, action.maxRange))
        {
            selectedAction = action;
            return true;
        }
        if (displayReason) characterSheet.DisplayPopup("Nothing in range");
        return false;
    }

    private bool TargetMeleeReachSpecialMove(ActionMeleeAttack action, bool displayReason)
    {
        if (FindSelectableMeleeReachAttackTiles(action.BASE_ACTION_COST))
        {
            selectedAction = action;
            return true;
        }
        if (displayReason) characterSheet.DisplayPopup("Nothing in range");
        return false;
    }

    private bool TargetGroundAttackSpecialMove(ActionGroundAttack action, bool displayReason, int minRange, int maxRange)
    {
        if (FindSelectableGroundAttackTiles(action.BASE_ACTION_COST, action.minRange, action.maxRange))
        {
            selectedAction = action;
            return true;
        }
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
        if (actionPoints < action.BASE_ACTION_COST)
        {
            if (displayReason) characterSheet.DisplayPopup("Not enough AP");
            return false;
        }
        if (characterSheet.CurrentMana < action.MANA_COST)
        {
            if (displayReason) characterSheet.DisplayPopup("Not enough mana");
            return false;
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
                return TargetReachSpecialMove(action, displayReason);
        }
        return false;
    }
}