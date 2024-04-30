using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : CombatController
{
    override protected bool ContainsEnemy(Tile tile)
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

    Tile AIChooseMove()
    {
        return null;
    }
}
