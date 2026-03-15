using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionMeleeAttack : ActionAttack
{
    private AbilityData abilityData;

    public void ConfigureFromAbility(AbilityData data)
    {
        abilityData = data;
        actionDisplayName = data.displayName;
        minRange = data.minRange;
        maxRange = data.maxRange;
        BASE_ACTION_COST = data.actionPointCost;
        visualLungeDistance = data.lungeDistance;
    }

    public override void ConfigureAction()
    {
        if (abilityData != null)
        {
            ConfigureFromAbility(abilityData);
            return;
        }
        minRange = 1;
        maxRange = 1;
        actionDisplayName = "Kick";
    }

    override public TargetType TARGET_TYPE { get { return (minRange >= 2 || maxRange >= 2) ? TargetType.MELEE_REACH : TargetType.MELEE; } }
    public override bool RequiresLineOfSight { get { return false; } }
    public override bool TargetsEnemiesOnly { get { return true; } }
    public override bool CanTargetEmptyTiles { get { return false; } }

    protected override void PerformAttack(Tile targetTile)
    {
        var enemy = targetTile.occupant;
        var popupTarget = enemy != null ? enemy.transform : null;

        if (enemy?.characterSheet != null)
        {
            var weapon = GetEquippedWeaponForThisAttack();
            var context = AttackContext.Melee(weapon, abilityData);
            var result = AttackResolver.Resolve(characterSheet, enemy.characterSheet, context);
            CombatLog.Log(result.logMessage);

            if (popupTarget != null)
            {
                ShowAttackPopup(result, popupTarget);
            }
        }
    }

    private void ShowAttackPopup(AttackResult result, Transform target)
    {
        if (result.hit)
        {
            string text = result.critical ? $"CRIT {result.damageDealt}" : result.damageDealt.ToString();
            PopupTextController.CreatePopupText(text, target);
        }
        else
        {
            PopupTextController.CreatePopupText("MISS", target);
        }
    }

    protected override float GetAttackDuration()
    {
        return 1.0f;
    }

    public override string Description()
    {
        string desc = base.Description();
        if (abilityData != null)
        {
            desc = $"{abilityData.displayName} deals {abilityData.damage} damage. Range: {minRange}-{maxRange}.";
        }
        return desc;
    }
}
