using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionRangedAttack : ActionAttack
{
    public override TargetType TARGET_TYPE { get { return TargetType.RANGED; } }

    // Ranged attacks target enemies only and require line of sight
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
