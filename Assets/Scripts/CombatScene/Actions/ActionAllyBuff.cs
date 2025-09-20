using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is a base class designed to be inherited from by ally buff type actions.
public class ActionAllyBuff : ActionRangedAttack
{
    override public TargetType TARGET_TYPE { get { return TargetType.SELF_OR_ALLY; } }

    // For ally buffs that can also target self, minRange should be 0. MinRange > 0 means it can only target allies and not self.

    virtual protected void ApplyTargetStatusEffect(Tile targetTile) { }

    public override void BeginAction(Tile targetTile)
    {
        base.BeginAction(targetTile);
        ApplyTargetStatusEffect(targetTile);
    }

    // Update is called once per frame
    void Update()
    {
        if (!inProgress)
        {
            return;
        }
        if (currentPhase == Phase.NONE)
        {
            currentPhase = Phase.CASTING;
            StartCoroutine(EndActionAfterDelay(1.0f));
        }
    }
}