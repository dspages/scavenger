using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionMove : Action
{
    [SerializeField] private float moveSpeed = 2;
    protected Stack<Tile> path = new Stack<Tile>();
    protected int reserveTiles = 0;
    private int moveCost = 0;

    override public int MOVE_COST {
        get {
            return moveCost;
        }
    }

    override protected void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    virtual protected void Update()
    {
        if (!inProgress)
        {
            return;
        }
        if (currentPhase == Phase.MOVING)
        {
            Move();
        }
        else
        {
            EndAction();
        }
    }

    protected void Move()
    {
        if (path.Count > 0)
        {
            Tile tile = path.Peek();
            Vector3 targetPos = tile.transform.position;

            if (Vector3.Distance(transform.position, targetPos) >= 0.01f)
            {
                Vector3 direction = CalculateDirection(targetPos);
                transform.up = new Vector3(direction.x, direction.y, 0f);
                transform.Translate(direction * Time.deltaTime * moveSpeed, Space.World);
            }
            else
            {
                // Center of tile reached
                transform.position = targetPos;
                if (path.Count == 1) combatController.SetCurrentTile(tile);
                path.Pop();
            }
        }
        else
        {
            // Done moving.
            currentPhase = Phase.ATTACKING;
        }
    }

    override public void BeginAction(Tile targetTile)
    {
        currentPhase = Phase.MOVING;
        PreparePath(targetTile);
        base.BeginAction(targetTile);
    }

    private void PreparePath(Tile targetTile)
    {
        path.Clear();
        moveCost = 0;
        Tile next = targetTile;
        while (next != null)
        {
            // Only has a move cost if it isn't the origin tile.
            if (next.searchParent) moveCost += next.GetMoveCost();
            path.Push(next);
            next = next.searchParent;
        }
    }

    protected Vector3 CalculateDirection(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        return direction.normalized;
    }
}
