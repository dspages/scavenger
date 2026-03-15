using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionAllyBuff : ActionRangedAttack
{
    override public TargetType TARGET_TYPE { get { return TargetType.SELF_OR_ALLY; } }

    private AbilityData buffAbilityData;

    public new void ConfigureFromAbility(AbilityData data)
    {
        buffAbilityData = data;
        base.ConfigureFromAbility(data);
    }

    virtual protected void ApplyTargetStatusEffect(Tile targetTile)
    {
        if (buffAbilityData != null && buffAbilityData.statusDuration > 0 &&
            targetTile?.occupant?.characterSheet != null)
        {
            new StatusEffect(buffAbilityData.statusEffect, buffAbilityData.statusDuration,
                targetTile.occupant.characterSheet, buffAbilityData.statusPowerLevel);
        }
    }

    public override void BeginAction(Tile targetTile)
    {
        base.BeginAction(targetTile);
        ApplyTargetStatusEffect(targetTile);
    }

    void Update()
    {
        if (!inProgress) return;
        if (currentPhase == Phase.NONE)
        {
            currentPhase = Phase.CASTING;
            StartCoroutine(EndActionAfterDelay(1.0f));
        }
    }
}