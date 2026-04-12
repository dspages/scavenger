using UnityEngine;

public class ActionMeleeAttack : ActionAttack
{
    private AbilityData abilityData;

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
        minRange = 1;
        maxRange = 1;
        actionDisplayName = "Kick";
    }

    override public TargetType TARGET_TYPE { get { return (minRange >= 2 || maxRange >= 2) ? TargetType.MELEE_REACH : TargetType.MELEE; } }
    public override bool RequiresLineOfSight { get { return false; } }
    public override bool TargetsEnemiesOnly { get { return true; } }
    public override bool CanTargetEmptyTiles { get { return false; } }

    public override AbilityData GetAbilityDataForCosts() => abilityData;

    public override bool IsCoolingDown() => abilityData != null && IsAbilityOnCooldown(abilityData.id);

    protected override void PerformAttack(Tile targetTile)
    {
        var enemy = targetTile.occupant;
        var popupTarget = enemy != null ? enemy.transform : null;

        if (enemy?.characterSheet != null)
        {
            int backstabCount = CountBackstabConditions(enemy, targetTile);
            var weapon = GetEquippedWeaponForThisAttack();
            var context = AttackContext.Melee(weapon, abilityData, backstabCount);

            // Hidden backstab consumes the status effect before resolution
            if (backstabCount > 0 && characterSheet.HasStatusEffect(StatusEffect.EffectType.HIDDEN))
                characterSheet.RemoveStatusEffect(StatusEffect.EffectType.HIDDEN);

            var result = AttackResolver.Resolve(characterSheet, enemy.characterSheet, context);
            CombatLog.Log(result.logMessage, result.hit ? result.damageType : (DamageType?)null);

            if (popupTarget != null)
            {
                ShowAttackPopup(result, popupTarget);
            }
        }
    }

    /// <summary>
    /// Count how many backstab conditions apply (0–2).
    /// Condition 1: attacker has HIDDEN status and attack is from distance 1.
    /// Condition 2: attacker is in the one tile directly behind the defender (distance 1 only).
    /// </summary>
    private int CountBackstabConditions(CombatController defender, Tile defenderTile)
    {
        Tile attackerTile = combatController.GetCurrentTile();
        if (attackerTile == null || defenderTile == null) return 0;

        int dist = Mathf.Abs(attackerTile.x - defenderTile.x) + Mathf.Abs(attackerTile.y - defenderTile.y);
        if (dist != 1) return 0;

        int count = 0;

        // Condition 1: attacker is hidden
        if (characterSheet.HasStatusEffect(StatusEffect.EffectType.HIDDEN))
            count++;

        // Condition 2: attacker is directly behind the defender (opposite of defender's facing direction)
        Vector2 defenderFacing = defender.GetFacingDirection();
        Vector2 attackerDir = new Vector2(attackerTile.x - defenderTile.x, attackerTile.y - defenderTile.y);
        // "Behind" = attacker direction is opposite to defender facing
        // With cardinal directions, dot product of -1 means directly behind
        float dot = Vector2.Dot(defenderFacing.normalized, attackerDir.normalized);
        if (dot <= -0.9f)
            count++;

        return count;
    }

    protected override float GetAttackDuration()
    {
        return 1.0f;
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
