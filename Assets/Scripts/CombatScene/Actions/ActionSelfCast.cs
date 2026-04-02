using System.Collections;
using System.Collections.Generic;
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

    public override bool IsCoolingDown()
    {
        return abilityData != null && characterSheet != null &&
               characterSheet.GetAbilityCooldownRemaining(abilityData.id) > 0;
    }

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
        if (currentPhase == Phase.NONE)
        {
            currentPhase = Phase.CASTING;

            if (abilityData != null)
            {
                if (!CombatItemSpend.TrySpendAbilityHardCosts(abilityData, characterSheet))
                {
                    EndAction();
                    return;
                }
                CombatItemSpend.ApplySanityCost(abilityData, characterSheet);
                if (abilityData.cooldown > 0 && !string.IsNullOrEmpty(abilityData.id))
                    characterSheet.PutAbilityOnCooldown(abilityData.id, abilityData.cooldown);
            }

            ApplySelfStatusEffect();
            StartCoroutine(EndActionAfterDelay(1.0f));
        }
    }
}