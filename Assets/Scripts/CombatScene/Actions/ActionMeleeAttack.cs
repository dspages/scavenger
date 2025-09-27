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

    // If configured with minRange >= 2 (e.g., pikes), treat as MELEE_REACH so
    // systems can compute a launch tile two tiles away from the target.
    override public TargetType TARGET_TYPE { get { return (minRange >= 2 || maxRange >= 2) ? TargetType.MELEE_REACH : TargetType.MELEE; } }
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
