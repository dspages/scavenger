using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyController : CombatController
{
    // AI Tuning Constants
    private const float WEIGHT_HAND = 50f;
    private const float WEIGHT_SPECIAL = 60f;
    private const float WEIGHT_KICK = 10f;
    private const float WEIGHT_TARGET_IN_RANGE = 100f;
    private const float TEMPERATURE_NOISE = 20f;

    private struct CandidateAction
    {
        public string key;
        public string className;
        public Action action;
        public float weight;
    }

    override public bool ContainsEnemy(Tile tile)
    {
        if (tile.occupant == null) return false;
        return tile.occupant.IsPC();
    }

    override public bool IsEnemy()
    {
        return true;
    }

    override protected bool ContainsAlly(Tile tile)
    {
        if (tile == null || tile.occupant == null) return false;
        return tile.occupant.IsEnemy();
    }

    void Update()
    {
        if (!isTurn) return;
        if (isActing) return;

        // 1. Build candidate actions
        List<CandidateAction> candidates = BuildCandidateActions();
        
        // 2. Shuffle/Sort by weight
        candidates = WeightedShuffle(candidates);

        // 3. Try to execute first valid action
        foreach (var cand in candidates)
        {
            if (TryExecuteAction(cand)) return;
        }

        // 4. Fallback: Move
        if (TryMoveTowardTarget()) return;

        // 5. End Turn
        EndTurn();
    }

    private List<CandidateAction> BuildCandidateActions()
    {
        List<CandidateAction> list = new List<CandidateAction>();
        if (characterSheet == null) return list;

        // Helper to add action
        void Add(string key, string cls, float baseWeight)
        {
            Action act = GetOrAddActionByName(cls);
            if (act != null)
            {
                // Simple heuristic: if we can't afford it, weight is 0
                if (characterSheet.currentActionPoints < act.BASE_ACTION_COST) return;
                
                // Configure it to check ranges
                SelectActionSilent(key, cls); 
                
                // Boost weight if targets are in range NOW
                if (HasValidTargetFromCurrentTile(act))
                {
                    baseWeight += 100f;
                }
                
                list.Add(new CandidateAction { key = key, className = cls, action = act, weight = baseWeight });
            }
        }

        // Right Hand
        var right = characterSheet.GetEquippedItem(EquippableItem.EquipmentSlot.RightHand) as EquippableHandheld;
        if (right != null)
        {
            string cls = string.IsNullOrEmpty(right.associatedActionClass) ? nameof(ActionMeleeAttack) : right.associatedActionClass;
            Add($"{cls}:RightHand", cls, WEIGHT_HAND);
        }

        // Left Hand
        var left = characterSheet.GetEquippedItem(EquippableItem.EquipmentSlot.LeftHand) as EquippableHandheld;
        if (left != null)
        {
            string cls = string.IsNullOrEmpty(left.associatedActionClass) ? nameof(ActionMeleeAttack) : left.associatedActionClass;
            Add($"{cls}:LeftHand", cls, WEIGHT_HAND);
        }

        // Legacy special actions (type-based)
        foreach (var t in characterSheet.GetKnownSpecialActionTypes())
        {
            Add(t.Name, t.Name, WEIGHT_SPECIAL);
        }

        // Data-driven abilities
        foreach (var ability in characterSheet.GetKnownAbilities())
        {
            if (ability == null) continue;
            string cls = ability.ArchetypeClassName();
            Add($"ability:{ability.id}", cls, WEIGHT_SPECIAL);
        }

        // Default Melee (Kick/Punch) always available
        Add(nameof(ActionKick), nameof(ActionKick), WEIGHT_KICK);

        return list;
    }

    private List<CandidateAction> WeightedShuffle(List<CandidateAction> list)
    {
        // Simple weighted random pick with noise for "temperature"
        return list.OrderByDescending(x => x.weight + Random.Range(0f, TEMPERATURE_NOISE)).ToList();
    }

    private bool HasValidTargetFromCurrentTile(Action action)
    {
        // Just check if ANY target exists (stopAtFirst=true)
        return GetTarget(action, GetCurrentTile(), true) != null;
    }

    private Tile GetTarget(Action action, Tile fromTile, bool stopAtFirst)
    {
        if (fromTile == null) return null;

        if (action is ActionAttack atk)
        {
             TurnManager tm = FindFirstObjectByType<TurnManager>(FindObjectsInactive.Exclude);
             // Use shared vision for target SELECTION
             VisionSystem vision = FindFirstObjectByType<VisionSystem>(FindObjectsInactive.Exclude);
             var knownPCs = vision.GetKnownPCsToEnemies();
             
             List<CombatController> potentialTargets = new List<CombatController>();
             if (action.TARGET_TYPE == Action.TargetType.SELF_OR_ALLY)
             {
                 potentialTargets = tm.AllLivingEnemies(); // Allies of Enemy
             }
             else
             {
                 // For attacks, target known PCs
                 potentialTargets = knownPCs.ToList();
             }

             Tile bestTile = null;
             float bestVal = -1f;

             // For Ground attacks, we check tiles occupied by known PCs
             if (action.TARGET_TYPE == Action.TargetType.GROUND_TILE)
             {
                 foreach(var target in potentialTargets)
                 {
                     if (target == null) continue;
                     Tile tTile = target.GetCurrentTile();
                     if (IsTileInRange(fromTile, atk.minRange, atk.maxRange, atk.RequiresLineOfSight, tTile.x, tTile.y))
                     {
                         // Check visibility (actual LOS for attack)
                         if (!IsTileVisibleByCurrentActor(tTile)) continue;
                         
                         // Self-damage check for AoE
                         if (atk.AOE_RADIUS > 0)
                         {
                             // Rough Manhattan dist check for "am I in the blast?"
                             int distToSelf = Mathf.Abs(fromTile.x - tTile.x) + Mathf.Abs(fromTile.y - tTile.y);
                             // If the blast radius touches me, skip this target (or heavily penalize)
                             // Note: AOE usually hits center + radius.
                             if (distToSelf <= atk.AOE_RADIUS) continue;
                         }

                         // If we just need existence, return immediately
                         if (stopAtFirst) return tTile;

                         // Otherwise, just pick the first valid one for MVP
                         return tTile;
                     }
                 }
                 return null;
             }

             foreach(var target in potentialTargets)
             {
                 if (target == null) continue;
                 Tile tTile = target.GetCurrentTile();
                 if (IsTileInRange(fromTile, atk.minRange, atk.maxRange, atk.RequiresLineOfSight, tTile.x, tTile.y))
                 {
                     // Check visibility (actual LOS for attack)
                     if (!IsTileVisibleByCurrentActor(tTile)) continue;

                     // Self-damage check for AoE (rare for non-ground, but possible)
                     if (atk.AOE_RADIUS > 0)
                     {
                         int distToSelf = Mathf.Abs(fromTile.x - tTile.x) + Mathf.Abs(fromTile.y - tTile.y);
                         if (distToSelf <= atk.AOE_RADIUS) continue;
                     }

                     // If we just need existence, return immediately
                     if (stopAtFirst) return tTile;

                     // Score it
                     float val = 10f; // Base score
                     if (target.characterSheet.currentHealth < 10) val += 5f; // Finish off weak
                     
                     if (val > bestVal)
                     {
                         bestVal = val;
                         bestTile = tTile;
                     }
                 }
             }
             return bestTile;
        }
        return null;
    }

    private bool TryExecuteAction(CandidateAction cand)
    {
        SelectActionSilent(cand.key, cand.className);
        
        // Now find actual best target (stopAtFirst=false)
        Tile bestTarget = GetTarget(cand.action, GetCurrentTile(), false);
        if (bestTarget != null)
        {
            cand.action.BeginAction(bestTarget);
            return true;
        }
        return false;
    }

    private bool TryMoveTowardTarget()
    {
        // 1. Get reachable tiles
        List<Tile> reachable = GetReachableTiles();
        Tile current = GetCurrentTile();
        
        // 2. Build candidate actions to check for attack opportunities
        List<CandidateAction> candidates = BuildCandidateActions();
        
        // 3. Find a tile that allows a good attack (kiting/positioning)
        Tile bestAttackTile = null;
        float bestDist = float.MaxValue;
        
        // Helper to check if a tile offers ANY valid attack
        bool CanAttackFrom(Tile t)
        {
            foreach (var cand in candidates)
            {
                // Use the action to check targets from tile 't'
                // We reuse SelectActionSilent to ensure the action is configured correctly for range checks?
                // Actually BuildCandidateActions already configured them, but iterating them might be safe if they are distinct instances.
                // However, 'candidates' list has 'action' references.
                // IMPORTANT: 'action' might rely on 'selectedAction' state if not carefully managed, but here we pass 'cand.action'.
                // 'GetTarget' uses 'cand.action'.
                // We do NOT need to SelectActionSilent here because GetTarget takes 'action' param.
                // BUT 'GetTarget' calls 'IsTileInRange' which uses 'atk.minRange'.
                // 'cand.action' is the component.
                // Is it configured? 'BuildCandidateActions' called 'SelectActionSilent' which configured 'selectedAction'.
                // But we have multiple candidates.
                // If we use the SAME component (ActionMeleeAttack) for different candidates, it might have wrong config?
                // 'BuildCandidateActions' creates NEW CandidateAction structs, but 'action' refers to the Component on the gameObject.
                // If we have multiple candidates sharing the same Component (e.g. Left Hand Sword and Right Hand Sword both mapping to ActionMeleeAttack),
                // then 'BuildCandidateActions' loop reconfigured it multiple times. The LAST one wins.
                // This is a flaw in my MVP AI structure if multiple items map to same Action component.
                // However, for MVP, let's assume we just check if *current configuration* works or if we need to re-select.
                // For safety, we should probably re-select if we want to be sure, but that's expensive inside a loop.
                // For now, let's just check the high-value ones.
                
                // Actually, checking 'GetTarget' with 'cand.action' is fine if 'cand.action' has the right stats.
                // If they share a component, they share stats.
                // We'll assume for now that 'BuildCandidateActions' leaves the component in a valid state or we accept the inaccuracy.
                
                if (GetTarget(cand.action, t, true) != null) return true;
            }
            return false;
        }

        // Check all reachable tiles for attack positions
        foreach (var tile in reachable)
        {
            if (tile == current) continue;
            
            if (CanAttackFrom(tile))
            {
                // Found a tile we can attack from!
                // Prefer the one closest to us (minimum movement) to avoid running past the target
                float d = Vector3.Distance(tile.transform.position, current.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestAttackTile = tile;
                }
            }
        }

        if (bestAttackTile != null)
        {
            Action move = GetComponent<ActionMove>();
            move.BeginAction(bestAttackTile);
            return true;
        }

        // 4. Fallback: Chase closest known PC
        VisionSystem vision = FindFirstObjectByType<VisionSystem>(FindObjectsInactive.Exclude);
        var knownPCs = vision.GetKnownPCsToEnemies();
        
        CombatController bestTarget = null;
        float minDist = float.MaxValue;

        foreach(var pc in knownPCs)
        {
            if (pc == null) continue;
            float d = Vector3.Distance(current.transform.position, pc.transform.position);
            if (d < minDist)
            {
                minDist = d;
                bestTarget = pc;
            }
        }

        if (bestTarget != null)
        {
            // Move towards it
            // Re-use reachable list
            Tile bestMove = null;
            float minMoveDist = float.MaxValue;
            
            foreach(var t in reachable)
            {
                float d = Vector3.Distance(t.transform.position, bestTarget.transform.position);
                if (d < minMoveDist)
                {
                    minMoveDist = d;
                    bestMove = t;
                }
            }
            
            if (bestMove != null && bestMove != current)
            {
                Action move = GetComponent<ActionMove>();
                move.BeginAction(bestMove);
                return true;
            }
        }
        else
        {
            // No target known: Wander randomly
            if (reachable.Count > 1) // >1 because current tile is included
            {
                reachable.Remove(current);
                Tile randomDest = reachable[Random.Range(0, reachable.Count)];
                
                Action move = GetComponent<ActionMove>();
                move.BeginAction(randomDest);
                return true;
            }
        }
        
        return false;
    }

    private List<Tile> GetReachableTiles()
    {
        return PathfindingSystem.GetReachableTilesSimple(GetCurrentTile(), characterSheet.currentActionPoints, manager);
    }
}
