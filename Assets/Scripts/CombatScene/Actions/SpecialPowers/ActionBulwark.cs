using System.Collections;
using UnityEngine;

public class ActionBulwark : ActionSelfCast
{
    [SerializeField] public int durationRounds = 3;
    [SerializeField] public int armorBonus = 3;

    public override string DisplayName()
    {
        return "Bulwark";
    }

    protected override void ApplySelfStatusEffect()
    {
        // Apply BULWARK status effect that provides armor bonus
        new StatusEffect(StatusEffect.EffectType.BULWARK, durationRounds, characterSheet);
    }

    override public string Description()
    {
        string desc = base.Description();
        desc += $"Bulwark increases armor by {armorBonus} for {durationRounds} rounds, providing enhanced protection against attacks.";
        return desc;
    }
}