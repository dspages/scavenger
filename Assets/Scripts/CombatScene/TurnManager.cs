using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class TurnManager : MonoBehaviour
{
    private List<CombatController> combatants = new List<CombatController>();
    private int moveIdx = -1;
    private bool enemyTurn = false;
    private bool frozen = false; // Freeze turn management while a move is in-progress.
    private bool gameOver = false;
    // private GUIPanel panel = null;

    public void InitiateCombat()
    {
        // panel = GameObject.FindObjectOfType<GUIPanel>();

        combatants = GetComponentsInChildren<CombatController>().ToList();
        // combatants.Sort(new SortCombatantDescendant());
    }

    private List<CombatController> GetLivingCombatants(System.Func<CombatController, bool> filter)
    {
        List<CombatController> result = new List<CombatController>();
        foreach (CombatController pick in combatants)
        {
            if (pick == null) continue;
            if (pick.Dead()) continue;
            if (!filter(pick)) continue;
            result.Add(pick);
        }
        return result;
    }

    public List<CombatController> AllLivingPCs()
    {
        return GetLivingCombatants(c => c.IsPC());
    }

    public List<CombatController> AllLivingEnemies()
    {
        return GetLivingCombatants(c => c.IsEnemy());
    }

    // Picks an arbitrary/random Player controlled character
    public GameObject PickArbitraryPC()
    {
        List<CombatController> pcs = AllLivingPCs();
        if (pcs.Count > 0) return pcs[Globals.rng.Next(pcs.Count)].gameObject;
        return null;
    }

    CombatController GetCurrentCombatController()
    {
        if (moveIdx == -1) return null;
        if (combatants[moveIdx] == null)
            return null;
        else if (!combatants[moveIdx].Dead())
        {
            return combatants[moveIdx];
        }
        return null;
    }

    public bool IsEnemyTurn()
    {
        return enemyTurn;
    }

    public bool IsPlayerTurn()
    {
        return !enemyTurn;
    }

    void BeginTurn()
    {
        CombatController controller = GetCurrentCombatController();
        enemyTurn = !controller.IsPC();
        controller.BeginTurn();
        // DisplayCurrentCreatureStats();
    }

    private IEnumerator BeginTurnAfterDelay(float fDuration)
    {
        frozen = true;
        float elapsed = 0f;
        while (elapsed < fDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        frozen = false;
        BeginTurn();
        yield break;
    }

    void AdvanceToNextTurn()
    {
        // Check to make sure there is someone left alive on each side.
        if (CheckGameOver())
        {
            gameOver = true;
            return;
        }
        do
        {
            moveIdx = (moveIdx + 1) % combatants.Count;
        }
        // Advance until you find the first one that isn't dead yet.
        while (GetCurrentCombatController() == null);

        StartCoroutine(BeginTurnAfterDelay(0.25f));
    }

    bool CheckGameOver()
    {
        if (AllLivingPCs().Count == 0)
            return true;
        if (AllLivingEnemies().Count == 0)
            return true;
        return false;
    }

    void Update()
    {
        if (frozen || gameOver) return;
        if (GetCurrentCombatController() == null || !GetCurrentCombatController().isTurn)
        {
            AdvanceToNextTurn();
        }
    }
}
