using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionRangedAttack : ActionAttack
{
    public override void ConfigureAction()
    {
        // Default ranged configuration
        minRange = 2;
        maxRange = 4;
        actionDisplayName = "Ranged Attack";
        baseDamage = 4;
    }

    public override TargetType TARGET_TYPE { get { return TargetType.RANGED; } }
    public override bool RequiresLineOfSight { get { return true; } }
    public override bool TargetsEnemiesOnly { get { return true; } }
    public override bool CanTargetEmptyTiles { get { return false; } }

    protected override void PerformAttack(Tile targetTile)
    {
        if (targetTile.occupant?.characterSheet != null)
        {
            targetTile.occupant.characterSheet.ReceiveDamage(baseDamage);
        }
    }

    protected override float GetAttackDuration()
    {
        return 0.5f;
    }
}
