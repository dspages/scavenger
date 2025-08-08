using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Action : MonoBehaviour
{
    protected enum Phase
    {
        NONE,
        MOVING,
        INTERRUPTING,
        BEING_INTERRUPTED,
        ATTACKING,
        RESOLVING_ATTACK
    };

    public enum TargetType
    {
        NONE,
        MELEE,
        RANGED,
    };

    protected Animator anim;
    protected bool inProgress = false;
    protected CombatController combatController;
    protected CharacterSheet characterSheet;

    virtual public int MOVE_COST { get { return 0; } }
    virtual public bool ATTACK_COST { get { return false; } }
    virtual public int MANA_COST { get { return 0; } }

    // Target type for special moves, lets UI/AI know when it can use special moves.
    virtual public TargetType TARGET_TYPE { get { return TargetType.NONE; } }

    protected Phase currentPhase = Phase.NONE;

    protected IEnumerator EndActionAfterDelay(float fDuration)
    {
        currentPhase = Phase.NONE;
        yield return new WaitForSeconds(fDuration);
        EndAction();
        yield break;
    }

    virtual public string DisplayName()
    {
        return "";
    }

    // Start is called before the first frame update
    virtual protected void Start()
    {
        combatController = GetComponent<CombatController>();
        anim = GetComponentInChildren<Animator>();
        characterSheet = combatController.characterSheet;
    }

    virtual public void BeginAction(Tile targetTile)
    {
        inProgress = true;
        characterSheet.DisplayPopupDuringCombat(DisplayName());
        combatController.BeginAction();
    }

    protected void EndAction()
    {
        inProgress = false;
        currentPhase = Phase.NONE;
        characterSheet.currentMovePoints -= MOVE_COST;
        if (ATTACK_COST) characterSheet.canAttack = false;
        combatController.EndAction();
    }
}
