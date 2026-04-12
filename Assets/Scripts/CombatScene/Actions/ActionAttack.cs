using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base class for all attack actions (melee, ranged, ground attacks)
// Handles movement, range validation, and common attack logic
public abstract class ActionAttack : ActionMove
{
    [SerializeField] public string actionDisplayName = "Attack";
    
    // Expose base action cost for configuration via items/spells
    public override int BASE_ACTION_COST { get { return baseActionCost; } set { baseActionCost = value; } }

    // Attack-specific properties to be overridden by subclasses
    public abstract bool RequiresLineOfSight { get; }
    public abstract bool TargetsEnemiesOnly { get; }
    public abstract bool CanTargetEmptyTiles { get; }

    // Optional AoE radius for ground-target actions; default 0 (no AoE)
    public virtual int AOE_RADIUS { get { return 0; } }

    public override string DisplayName()
    {
        return actionDisplayName;
    }

    // The tile we intend to attack after movement completes
    protected Tile pendingAttackTarget;

    // Cosmetic lunge/recoil distance in tile units (+toward target, -away from target)
    [SerializeField] public float visualLungeDistance = 0.3f;

    // It's the responsibility of the controller to validate the target, so no need to do it here.
    public override void BeginAction(Tile targetTile)
    {
        // Store the attack target and move toward the launch tile (separate attack parent)
        pendingAttackTarget = targetTile;

        Tile launchTile = AttackLaunchTile.Resolve(targetTile, combatController.GetCurrentTile());

        currentPhase = Phase.MOVING;
        PreparePath(launchTile);
        // Call Action.BeginAction semantics without re-preparing a path to the target tile
        inProgress = true;
        characterSheet.DisplayPopupDuringCombat(DisplayName());
        combatController.BeginAction();
    }

    // Override Update to handle attack phase after movement
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

    protected virtual void AttackPhase()
    {
        Tile targetTile = pendingAttackTarget;
        
        // Begin attack sequence (handles facing, lunge/recoil, projectile, damage, and return)
        currentPhase = Phase.RESOLVING_ATTACK;
        StartCoroutine(AttackSequence(targetTile));
    }

    // Main attack coroutine orchestrating lunge/recoil motion, projectile (if any), damage, and return
    protected IEnumerator AttackSequence(Tile targetTile)
    {
        if (targetTile == null)
        {
            EndAction();
            yield break;
        }

        ConsumeAttackResources();

        Vector3 originalPos = transform.position;
        Vector3 targetPos = targetTile.transform.position;
        Vector3 direction = CalculateDirection(targetPos);
        if (direction != Vector3.zero)
        {
            transform.up = new Vector3(direction.x, direction.y, 0f);
        }

        // Compute lunge target (negative distance becomes recoil)
        float lungeDist = visualLungeDistance;
        Vector3 lungePos = originalPos + (direction * lungeDist);

        // Durations scale mildly with distance to keep feel consistent
        float forwardTime = Mathf.Lerp(0.3f, 0.5f, Mathf.Clamp01(Mathf.Abs(lungeDist) / 0.4f));
        float backTime = Mathf.Lerp(0.25f, 0.45f, Mathf.Clamp01(Mathf.Abs(lungeDist) / 0.4f));

        // Forward lunge with slight acceleration (ease-in)
        yield return VfxHelpers.MoveWithEase(transform, originalPos, lungePos, forwardTime, VfxHelpers.EaseInQuad, true, direction);

        // If this attack uses a projectile, spawn it now and wait for impact
        if (UsesProjectile())
        {
            yield return SpawnProjectileAndWait(originalPos + direction * 0.15f, targetPos);
        }

        // Apply damage/effect at lunge apex (or after projectile arrival)
        PerformAttack(targetTile);

        // Small impact hold to sell the hit (does not block too long)
        yield return new WaitForSeconds(0.06f);

        // After the attack resolves, make the target(s) face the attacker, snapped to 90°
        OnTargetsAttacked(targetTile);

        // Return to original position with slight deceleration (ease-out)
        yield return VfxHelpers.MoveWithEase(transform, lungePos, originalPos, backTime, VfxHelpers.EaseOutQuad, true, direction);

        // Accrue base action cost (movement accrued per tile in ActionMove.Move)
        actionPointCost += BASE_ACTION_COST;

        // End after a short cosmetic delay and snap facing to grid
        yield return new WaitForSeconds(Mathf.Max(0.0f, GetAttackDuration() - forwardTime - backTime - 0.06f));

        // Snap rotation to nearest cardinal direction
        SnapToCardinalDirection();

        EndAction();
        yield break;
    }

    protected void SnapToCardinalDirection()
    {
        transform.up = GetNearestCardinal(transform.up);
    }

    // Whether this attack spawns a visible projectile (ranged/ground spells will override)
    protected virtual bool UsesProjectile()
    {
        return false;
    }

    // Default projectile implementation: quick glowing streak toward target
    protected virtual IEnumerator SpawnProjectileAndWait(Vector3 from, Vector3 to)
    {
        // Use generic streak projectile by default
        yield return VfxHelpers.ProjectileStreak(from, to, 12f);
        yield break;
    }

    // Called after attack resolution to let targets face this attacker
    protected virtual void OnTargetsAttacked(Tile targetTile)
    {
        if (targetTile == null || targetTile.occupant == null) return;
        MakeUnitFaceThisActor(targetTile.occupant);
    }

    protected void MakeUnitFaceThisActor(CombatController unit)
    {
        if (unit == null || unit.Dead()) return;
        Transform t = unit.transform;
        Vector3 toAttacker = transform.position - t.position;
        if (toAttacker.sqrMagnitude <= 0.0001f) return;
        t.up = new Vector3(toAttacker.x, toAttacker.y, 0f).normalized;
        // Snap to nearest cardinal
        Vector3 snapped = GetNearestCardinal(t.up);
        t.up = snapped;
    }

    private Vector3 GetNearestCardinal(Vector3 dir)
    {
        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);
        if (absX > absY)
        {
            return dir.x >= 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            return dir.y >= 0 ? Vector3.up : Vector3.down;
        }
    }

    // Detect which weapon (if any) is driving this attack
    protected EquippableHandheld GetEquippedWeaponForThisAttack()
    {
        if (combatController == null || characterSheet == null) return null;
        string key = combatController.GetSelectedActionKey();
        // Only treat as weapon-driven if the selection explicitly references a hand
        if (string.IsNullOrEmpty(key)) return null;
        if (!CombatItemSpend.TryGetHandSlotFromActionKey(key, out var slot))
            return null; // Spells or non-hand actions should not inherit weapon visuals
        return characterSheet.GetEquippedItem(slot) as EquippableHandheld;
    }

    // Musket-specific projectile: visible ball with rotating whoosh ring and bright trail
    protected IEnumerator SpawnMusketProjectileAndWait(Vector3 from, Vector3 to)
    {
        yield return VfxHelpers.ProjectileWhooshingBall(from, to, 20f);
    }

    /// <summary>Optional ability data for ammo overrides and inventory costs. Weapon-only attacks return null.</summary>
    public virtual AbilityData GetAbilityDataForCosts() => null;

    /// <summary>Spend ammo / consumable weapon stacks before damage resolution.</summary>
    /// <remarks>
    /// Validation for affordability is performed earlier (BFS / affordance and IsValid). Reaching this
    /// method without sufficient resources indicates a logic error, so it fails loudly instead of
    /// returning a bool.
    /// </remarks>
    protected virtual void ConsumeAttackResources()
    {
        var ability = GetAbilityDataForCosts();
        var sheet = combatController.characterSheet;
#if UNITY_EDITOR
        bool affordExpected = CombatActionAffordance.CanAffordFullAttackAction(
            combatController, GetEquippedWeaponForThisAttack(), ability);
#endif
        if (!CombatItemSpend.TrySpendAbilityHardCosts(ability, sheet))
        {
#if UNITY_EDITOR
            if (affordExpected)
                CombatItemSpend.LogEditorInvariantFailed("TrySpendAbilityHardCosts failed after CanAffordFullAttackAction returned true.");
#endif
            throw new System.InvalidOperationException("ConsumeAttackResources reached without ability hard costs available.");
        }
        if (!CombatItemSpend.TryCommitWeaponAttackCosts(combatController, GetEquippedWeaponForThisAttack(), ability))
        {
#if UNITY_EDITOR
            if (affordExpected)
                CombatItemSpend.LogEditorInvariantFailed("TryCommitWeaponAttackCosts failed after CanAffordFullAttackAction returned true.");
#endif
            throw new System.InvalidOperationException("ConsumeAttackResources reached without weapon attack resources available.");
        }
        CombatItemSpend.ApplySanityCost(ability, sheet);
        if (ability != null && ability.cooldown > 0 && !string.IsNullOrEmpty(ability.id))
            sheet.PutAbilityOnCooldown(ability.id, ability.cooldown);
    }

    // Abstract method for subclasses to implement their specific attack logic
    protected abstract void PerformAttack(Tile targetTile);

    protected void ShowAttackPopup(AttackResult result, Transform target)
    {
        if (result.hit)
            PopupTextController.CreateDamagePopup(result.damageDealt, result.critical, result.damageType, target);
        else
            PopupTextController.CreateMissPopup(target);
    }

    // Virtual method for attack duration (can be overridden)
    protected virtual float GetAttackDuration()
    {
        return 1.0f;
    }

    // Helper method for range calculation (shared across all attack types)
    protected int CalculateManhattanDistance(Tile start, Tile end)
    {
        if (start == null || end == null) return -1;
        return Mathf.Abs(start.x - end.x) + Mathf.Abs(start.y - end.y);
    }

    public override string Description()
    {
        string desc = base.Description();
        var weapon = GetEquippedWeaponForThisAttack();
        int dmg = weapon != null ? weapon.damage : AttackResolver.UNARMED_DAMAGE;
        desc += $"{actionDisplayName} deals {dmg} damage. Range: {minRange}-{maxRange}.";
        return desc;
    }
}
