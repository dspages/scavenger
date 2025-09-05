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
        RESOLVING_ATTACK,
        CASTING,
    };

    public enum TargetType
    {
        NONE,
        MELEE,
        RANGED,
        SELF_ONLY,
        SELF_AND_ALLY,
        GROUND_TILE,
    };

    protected Animator anim;
    protected bool inProgress = false;
    protected CombatController combatController;
    protected CharacterSheet characterSheet;
    protected int actionPointCost = 0;
    protected int baseActionCost = 0;

    virtual public int BASE_ACTION_COST { get { return baseActionCost; } set { baseActionCost = value; } }
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

    // Virtual method for actions to configure themselves (spells, abilities, etc.)
    virtual public void ConfigureAction()
    {
        // Base implementation does nothing - spells/abilities override this
    }

    virtual public void BeginAction(Tile targetTile)
    {
        inProgress = true;
        characterSheet.DisplayPopupDuringCombat(DisplayName());
        combatController.BeginAction();
    }

    virtual public string Description()
    {
        string desc = "";
        if (BASE_ACTION_COST > 0)
        {
            desc += $"Action Cost: {BASE_ACTION_COST} AP\n";
        }
        return desc;
    }

    protected void EndAction()
    {
        inProgress = false;
        currentPhase = Phase.NONE;
        characterSheet.ModifyActionPoints(-actionPointCost);
        combatController.EndAction();
    }
}
