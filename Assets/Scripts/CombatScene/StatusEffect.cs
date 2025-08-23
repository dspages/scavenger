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

    public string GetIconName()
    {
        switch (type)
        {
            default:
                return null;
        }
        return null;
    }

    // Some status effects have varying power levels, others default to -1
    public StatusEffect(EffectType effectType, int durationRounds, CharacterSheet effectTarget, int effectPowerLevel = -1)
    {
        if (effectTarget == null)
        {
            return;
        }
        roundsRemaining = durationRounds;
        type = effectType;
        target = effectTarget;
        powerLevel = effectPowerLevel;
        target.RegisterStatusEffect(this);
        
        // Notify the CombatController if this is a HIDDEN effect
        if (effectType == EffectType.HIDDEN && target.avatar != null)
        {
            CombatController combatController = target.avatar.GetComponent<CombatController>();
            if (combatController != null)
            {
                combatController.NotifyStatusEffectChanged(EffectType.HIDDEN);
            }
        }
    }

    // The CharacterSheet is responsible for calling this every round before its action.
    // It can change the creature's action points.
    public int PerRoundEffect(int movementPoints)
    {
        switch (type)
        {
            case EffectType.REGENERATION:
                target.ReceiveHealing(5);
                break;
            case EffectType.KNOCKDOWN:
                movementPoints = 0;
                target.DisplayPopupDuringCombat("Knockdown");
                break;
            case EffectType.PETRIFIED:
                movementPoints = 0;
                target.DisplayPopupDuringCombat("Petrified");
                break;
            case EffectType.POISONED:
                target.ReceivePureDamage(5);
                break;
            case EffectType.SLOWED:
                // -4 AP, but never takes a unit below 2 AP.
                target.DisplayPopupDuringCombat("Slowed");
                if (movementPoints > 6) movementPoints -= 4;
                else if (movementPoints > 0) movementPoints = 2;
                break;
            case EffectType.FROZEN:
                target.DisplayPopupDuringCombat("Frozen");
                movementPoints = 0;
                break;
            case EffectType.MOBILITY:
                if (movementPoints > 0) movementPoints += 2; // +2 AP, as long as the unit isn't disabled.
                break;
            case EffectType.BURNING:
                target.ReceiveDamage(10);
                break;
        }
        roundsRemaining--;
        if (roundsRemaining == 0)
        {
            target.RefreshStatusIcons();
            expired = true;
        }
        return movementPoints;
    }
}