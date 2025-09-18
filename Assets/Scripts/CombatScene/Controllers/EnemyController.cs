using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : CombatController
{
    override public bool ContainsEnemy(Tile tile)
    {
        if (tile.occupant == null) return false;
        return tile.occupant.IsPC();
    }

    override public bool IsEnemy()
    {
        return true;
    }

    void Update()
    {
        if (!isTurn) return;
        if (isActing) return;
        Tile choice = AIChooseMove();
        if (choice == null)
        {
            EndTurn();
        }
        else
        {
            if (choice.occupant != null)
            {
                selectedAction.BeginAction(choice);
            }
            else
            {
                Action move = GetComponent<ActionMove>();
                move.BeginAction(choice);
            }
        }
    }

    // Gather all valid moves and choose a valid one.
    Tile AIChooseMove()
    {
        foreach (Action possibleAction in possibleActions)
        {
            if (FindAllValidTargets(possibleAction, false)) break;
        }
        if (selectedAction.TARGET_TYPE == Action.TargetType.SELF_ONLY) return GetCurrentTile();
        float bestScore = 0.0f;
        Tile bestChoice = null;
        foreach (Tile option in selectableTiles)
        {
            float score = EvaluateMove(option);
            if (score > bestScore)
            {
                bestScore = score;
                bestChoice = option;
            }
        }
        if (bestScore > 0.0f) return bestChoice;
        return null;
    }

    // If the tile can be attacked, returns 100. Otherwise,
    // returns 100 minus its distance from a target.
    // TODO: Make AI smarter but also more random in its choices.
    float EvaluateMove(Tile tile)
    {
        // A target is within attack range for this move. Best possible score.
        if (ContainsEnemy(tile))
        {
            return 100.0f;
        }
        // Ally buff or self buff has a lower score than attacks.
        if (ContainsAlly(tile))
        {
            return 75.0f;
        }
        // Pick a random visible target if the move can't reach an enemy directly.
        GameObject target = PickTarget();

        // If no attackable target is visible, all move tiles are equal, so rank them randomly but low.
        if (target == null) return Random.Range(0f, 1.0f);

        // A target is visible, but out of attack range, so the best move is the one that brings me closer to the target.
        return 50.0f - Vector3.Distance(tile.transform.position, target.transform.position);
    }

    override protected bool ContainsAlly(Tile tile)
    {
        if (tile == null || tile.occupant == null) return false;
        // From the perspective of EnemyController, allies are other enemies (IsEnemy == true)
        return tile.occupant.IsEnemy();
    }

    private GameObject PickTarget()
    {
        // Choose any visible player unit as a target; fallback to null
        VisionSystem vision = FindObjectOfType<VisionSystem>();
        if (vision == null) return null;

        // Scan all combatants and choose the first visible player character
        CombatController[] all = FindObjectsOfType<CombatController>();
        foreach (CombatController cc in all)
        {
            if (cc == null || cc.Dead()) continue;
            if (cc.IsPC() && vision.CanSeeUnit(this, cc))
            {
                return cc.gameObject;
            }
        }
        return null;
    }
}
