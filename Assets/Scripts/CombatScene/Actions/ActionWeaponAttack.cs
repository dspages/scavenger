using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionWeaponAttack : ActionMove
{
    virtual protected float ATTACK_DURATION { get { return 1.0f; } }
    override public bool ATTACK_COST { get { return true; } }
    override public TargetType TARGET_TYPE { get { return TargetType.MELEE; } }

    // Update is called once per frame
    protected override void Update()
    {
        if (!inProgress)
        {
            return;
        }
        if (currentPhase == Phase.MOVING)
        {
            Move();
        }
        else if (currentPhase == Phase.ATTACKING)
        {
            AttackPhase();
        }
        else
        {
            currentPhase = Phase.NONE;
        }
    }

    void ResolveAttack(CharacterSheet targetSheet)
    {
        // CharacterSheet targetSheet = target.GetComponent<CharacterSheet>();
        AttackEffects(targetSheet);
    }

    virtual protected void AttackEffects(CharacterSheet targetSheet)
    {
        characterSheet.PerformBasicAttack(targetSheet);
    }

    void AttackPhase()
    {
        if (path.Count == 1)
        {
            Tile targetTile = path.Pop();
            Vector3 direction = CalculateDirection(targetTile.transform.position);
            direction.y = 0f;
            if (direction != Vector3.zero)
                transform.forward = direction;
            ResolveAttack(targetTile.occupant.characterSheet);
        }
        else
        {
            StartCoroutine(EndActionAfterDelay(ATTACK_DURATION));
        }
    }
}
