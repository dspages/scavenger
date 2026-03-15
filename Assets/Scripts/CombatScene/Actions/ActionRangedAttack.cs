using System.Collections;
using UnityEngine;

public class ActionRangedAttack : ActionAttack
{
    private AbilityData abilityData;
    private AttackResult? precomputedResult;

    public void ConfigureFromAbility(AbilityData data)
    {
        abilityData = data;
        actionDisplayName = data.displayName;
        minRange = data.minRange;
        maxRange = data.maxRange;
        BASE_ACTION_COST = data.actionPointCost;
        visualLungeDistance = data.lungeDistance;
    }

    public override void ConfigureAction()
    {
        if (abilityData != null)
        {
            ConfigureFromAbility(abilityData);
            return;
        }
        minRange = 2;
        maxRange = 4;
        actionDisplayName = "Ranged Attack";
    }

    public override TargetType TARGET_TYPE { get { return TargetType.RANGED; } }
    public override bool RequiresLineOfSight { get { return true; } }
    public override bool TargetsEnemiesOnly { get { return true; } }
    public override bool CanTargetEmptyTiles { get { return false; } }

    protected override void AttackPhase()
    {
        Tile targetTile = pendingAttackTarget;
        currentPhase = Phase.RESOLVING_ATTACK;
        StartCoroutine(RangedAttackSequence(targetTile));
    }

    private IEnumerator RangedAttackSequence(Tile targetTile)
    {
        if (targetTile == null)
        {
            EndAction();
            yield break;
        }

        Vector3 originalPos = transform.position;
        Vector3 targetPos = targetTile.transform.position;
        Vector3 direction = CalculateDirection(targetPos);
        if (direction != Vector3.zero)
            transform.up = new Vector3(direction.x, direction.y, 0f);

        float lungeDist = visualLungeDistance;
        Vector3 lungePos = originalPos + (direction * lungeDist);

        float forwardTime = Mathf.Lerp(0.3f, 0.5f, Mathf.Clamp01(Mathf.Abs(lungeDist) / 0.4f));
        float backTime = Mathf.Lerp(0.25f, 0.45f, Mathf.Clamp01(Mathf.Abs(lungeDist) / 0.4f));

        yield return VfxHelpers.MoveWithEase(transform, originalPos, lungePos, forwardTime, VfxHelpers.EaseInQuad, true, direction);

        // Resolve hit/miss BEFORE spawning the projectile so we know where to aim
        precomputedResult = null;
        if (targetTile.occupant?.characterSheet != null)
        {
            var weapon = GetEquippedWeaponForThisAttack();
            int dist = CalculateManhattanDistance(combatController.GetCurrentTile(), targetTile);
            var context = AttackContext.Ranged(weapon, dist, abilityData);
            precomputedResult = AttackResolver.Resolve(characterSheet, targetTile.occupant.characterSheet, context);
        }

        Vector3 projectileFrom = originalPos + direction * 0.15f;

        if (precomputedResult.HasValue && !precomputedResult.Value.hit)
        {
            float angle = Random.Range(0, 2) == 0 ? Random.Range(10f, 15f) : Random.Range(-15f, -10f);
            Vector3 deflected = Quaternion.Euler(0, 0, angle) * direction;
            Vector3 missTarget = targetPos + deflected.normalized * 1.5f;
            yield return SpawnProjectileAndWait(projectileFrom, missTarget);
        }
        else
        {
            yield return SpawnProjectileAndWait(projectileFrom, targetPos);
        }

        PerformAttack(targetTile);

        yield return new WaitForSeconds(0.06f);
        OnTargetsAttacked(targetTile);

        yield return VfxHelpers.MoveWithEase(transform, lungePos, originalPos, backTime, VfxHelpers.EaseOutQuad, true, direction);

        actionPointCost += BASE_ACTION_COST;
        yield return new WaitForSeconds(Mathf.Max(0.0f, GetAttackDuration() - forwardTime - backTime - 0.06f));
        SnapToCardinalDirection();
        EndAction();
    }

    protected override void PerformAttack(Tile targetTile)
    {
        var enemy = targetTile.occupant;
        var popupTarget = enemy != null ? enemy.transform : null;

        if (enemy?.characterSheet == null) return;

        AttackResult result;
        if (precomputedResult.HasValue)
        {
            result = precomputedResult.Value;
            precomputedResult = null;
        }
        else
        {
            var weapon = GetEquippedWeaponForThisAttack();
            int dist = CalculateManhattanDistance(combatController.GetCurrentTile(), targetTile);
            var context = AttackContext.Ranged(weapon, dist, abilityData);
            result = AttackResolver.Resolve(characterSheet, enemy.characterSheet, context);
        }

        CombatLog.Log(result.logMessage);

        if (popupTarget != null)
        {
            if (result.hit)
            {
                string text = result.critical ? $"CRIT {result.damageDealt}" : result.damageDealt.ToString();
                PopupTextController.CreatePopupText(text, popupTarget);
            }
            else
            {
                PopupTextController.CreatePopupText("MISS", popupTarget);
            }
        }
    }

    protected override float GetAttackDuration()
    {
        return 0.5f;
    }

    protected override bool UsesProjectile()
    {
        return true;
    }

    protected override IEnumerator SpawnProjectileAndWait(Vector3 from, Vector3 to)
    {
        yield return VfxHelpers.ProjectileWhooshingBall(from, to, 20f);
    }

    public override string Description()
    {
        string desc = base.Description();
        if (abilityData != null)
        {
            desc = $"{abilityData.displayName} deals {abilityData.damage} damage. Range: {minRange}-{maxRange}.";
        }
        return desc;
    }
}
