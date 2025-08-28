using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionWeaponAttack : ActionAttack
{
    override public bool ATTACK_COST { get { return true; } }
    override public TargetType TARGET_TYPE { get { return TargetType.MELEE; } }

    // Weapon attacks target enemies only and don't need line of sight (melee range)
    public override bool RequiresLineOfSight { get { return false; } }
    public override bool TargetsEnemiesOnly { get { return true; } }
    public override bool CanTargetEmptyTiles { get { return false; } }

    protected override void PerformAttack(Tile targetTile)
    {
        if (targetTile.occupant?.characterSheet != null)
        {
            AttackEffects(targetTile.occupant.characterSheet);
        }
    }

    virtual protected void AttackEffects(CharacterSheet targetSheet)
    {
        characterSheet.PerformBasicAttack(targetSheet);
    }

    protected override float GetAttackDuration()
    {
        return 1.0f;
    }
}
