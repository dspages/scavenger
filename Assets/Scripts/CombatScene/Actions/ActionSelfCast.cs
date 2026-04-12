using System.Collections;
using UnityEngine;

public class ActionSelfCast : Action
{
    override public TargetType TARGET_TYPE { get { return TargetType.SELF_ONLY; } }

    private AbilityData abilityData;

    public void ConfigureFromAbility(AbilityData data)
    {
        abilityData = data;
        BASE_ACTION_COST = data.actionPointCost;
    }

    /// <summary>Backing ability for inventory costs and affordance (mirrors <see cref="ActionAttack.GetAbilityDataForCosts"/>).</summary>
    public AbilityData GetAbilityDataForCosts() => abilityData;

    public override void BeginAction(Tile targetTile)
    {
        if (combatController == null) return;
        if (IsCoolingDown())
        {
            characterSheet?.DisplayPopup("Cooling down");
            return;
        }
        if (characterSheet != null && characterSheet.currentActionPoints < BASE_ACTION_COST)
        {
            characterSheet.DisplayPopup("Not enough AP");
            return;
        }
        Tile current = combatController.GetCurrentTile();
        if (targetTile == null || current == null || targetTile != current)
        {
            characterSheet?.DisplayPopup("Select your character's tile to cast.");
            return;
        }
        base.BeginAction(targetTile);
    }

    public override string DisplayName()
    {
        return abilityData != null ? abilityData.displayName : "";
    }

    public override string Description()
    {
        string desc = base.Description();
        if (abilityData != null)
            desc += abilityData.description;
        return desc;
    }

    public override bool IsCoolingDown() => abilityData != null && IsAbilityOnCooldown(abilityData.id);

    virtual protected void ApplySelfStatusEffect()
    {
        if (abilityData != null && abilityData.statusDuration > 0)
        {
            new StatusEffect(abilityData.statusEffect, abilityData.statusDuration,
                characterSheet, abilityData.statusPowerLevel);
        }
    }

    void Update()
    {
        if (!inProgress) return;
        if (currentPhase == Phase.CASTING) return;
        if (currentPhase == Phase.NONE)
        {
            currentPhase = Phase.CASTING;
            StartCoroutine(SelfCastSequence());
        }
    }

    private IEnumerator SelfCastSequence()
    {
        // Spend resources and apply cooldown once at cast start.
        if (abilityData != null)
        {
            if (!CombatItemSpend.TrySpendAbilityHardCosts(abilityData, characterSheet))
            {
                EndAction();
                yield break;
            }
            CombatItemSpend.ApplySanityCost(abilityData, characterSheet);
            if (abilityData.cooldown > 0 && !string.IsNullOrEmpty(abilityData.id))
                characterSheet.PutAbilityOnCooldown(abilityData.id, abilityData.cooldown);
        }

        // Cosmetic self-cast lunge: small bob along facing direction.
        Vector3 originalPos = transform.position;
        Vector3 dir = transform.up.sqrMagnitude > 0.0001f ? transform.up.normalized : Vector3.up;
        float lungeDist = 0.2f;
        Vector3 lungePos = originalPos + dir * lungeDist;

        float forwardTime = 0.25f;
        float backTime = 0.2f;

        yield return VfxHelpers.MoveWithEase(transform, originalPos, lungePos, forwardTime, VfxHelpers.EaseInQuad, true, dir);

        ApplySelfStatusEffect();
        actionPointCost = BASE_ACTION_COST;

        yield return VfxHelpers.MoveWithEase(transform, lungePos, originalPos, backTime, VfxHelpers.EaseOutQuad, true, dir);

        yield return EndActionAfterDelay(0.1f);
    }
}