using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Status effect that will automatically apply itself to a target creature upon instantiation.
// Can buff or debuff the target.
public class StatusEffect
{
    public enum EffectType
    {
        REGENERATION,
        KNOCKDOWN,
        RAGE,
        CANNOT_DIE,
        EMPOWER,
        BLINDED,
        PETRIFIED,
        POISONED,
        FROZEN,
        BURNING,
        PERFIDY,
        MOBILITY,
        BULWARK,
        SLOWED,
        HIDDEN,
    };


    public EffectType type;
    int roundsRemaining = 0;
    CharacterSheet target;
    int powerLevel;
    public int PowerLevel => powerLevel;
    public int RoundsRemaining => roundsRemaining;
    public bool expired = false;

    static public bool HasEffectType(ref List<StatusEffect> check, EffectType effectType)
    {
        foreach (StatusEffect effect in check)
        {
            if (effect.type == effectType) return true;
        }
        return false;
    }

    static public void RemoveStatusEffect(List<StatusEffect> statusEffects, EffectType effectType)
    {
        for (int i = statusEffects.Count - 1; i >= 0; i--)
        {
            if (statusEffects[i].type == effectType)
            {
                statusEffects.RemoveAt(i);
            }
        }
    }

    // Some status effects have varying power levels, others default to -1
    public StatusEffect(EffectType effectType, int durationRounds, CharacterSheet effectTarget, int effectPowerLevel = -1)
    {
        if (effectTarget != null)
        {
            roundsRemaining = durationRounds;
            type = effectType;
            target = effectTarget;
            powerLevel = effectPowerLevel;
            target.RegisterStatusEffect(this);

            if (effectType == EffectType.HIDDEN && target.avatar != null)
            {
                CombatController combatController = target.avatar.GetComponent<CombatController>();
                if (combatController != null)
                    combatController.NotifyStatusEffectChanged(EffectType.HIDDEN);
            }
        }
    }

    public int PerRoundEffect(int movementPoints)
    {
        var data = ContentRegistry.GetEffectData(type);
        if (data != null)
            movementPoints = ApplyEffectData(data, movementPoints, target);

        roundsRemaining--;
        if (roundsRemaining == 0)
        {
            target.RefreshStatusIcons();
            expired = true;
        }
        return movementPoints;
    }

    public static int ApplyEffectData(StatusEffectData data, int ap, CharacterSheet target)
    {
        if (data.healPerRound > 0)
            target.ReceiveHealing(data.healPerRound);

        if (data.damagePerRound > 0)
        {
            if (data.isPureDamage)
                target.ReceivePureDamage(data.damagePerRound);
            else
                target.ReceiveDamage(data.damagePerRound);
        }

        if (!string.IsNullOrEmpty(data.popupText))
            target.DisplayPopupDuringCombat(data.popupText);

        switch (data.apEffect)
        {
            case StatusEffectData.APEffect.Zero:
                ap = 0;
                break;
            case StatusEffectData.APEffect.Modify:
                if (ap > 0)
                {
                    ap += data.apModifier;
                    if (ap < data.apFloor)
                        ap = data.apFloor;
                }
                break;
        }

        return ap;
    }

}