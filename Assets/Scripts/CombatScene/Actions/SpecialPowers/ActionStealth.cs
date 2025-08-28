using System.Collections;
using UnityEngine;

public class ActionStealth : ActionSelfCast
{
	[SerializeField] public int durationRounds = 3;

	public override string DisplayName()
	{
		return "Stealth";
	}

	protected override void ApplySelfStatusEffect()
	{
		// Apply HIDDEN status; vision system is notified via CharacterSheet/CombatController
		new StatusEffect(StatusEffect.EffectType.HIDDEN, durationRounds, characterSheet);
	}

	override public string Description()
	{
		string desc = base.Description();
		desc += $"Stealth lasts for {durationRounds} rounds. While in stealth, the character is invisible to enemies. Canceled if attacking or an enemy moves directly into your square.";
		return desc;
	}
}


