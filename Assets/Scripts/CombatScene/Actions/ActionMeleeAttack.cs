using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionMeleeAttack : ActionAttack
{
    public override void ConfigureAction()
    {
        // Default melee configuration
        minRange = 1;
        maxRange = 1;
        actionDisplayName = "Melee Attack";
        baseDamage = 5;
    }

    override public TargetType TARGET_TYPE { get { return TargetType.MELEE; } }
    public override bool RequiresLineOfSight { get { return false; } }
    public override bool TargetsEnemiesOnly { get { return true; } }
    public override bool CanTargetEmptyTiles { get { return false; } }

    protected override void PerformAttack(Tile targetTile)
    {
        if (targetTile.occupant?.characterSheet != null)
        {
            characterSheet.PerformBasicAttack(targetTile.occupant.characterSheet);
        }
    }

    protected override float GetAttackDuration()
    {
        return 1.0f;
    }
}
