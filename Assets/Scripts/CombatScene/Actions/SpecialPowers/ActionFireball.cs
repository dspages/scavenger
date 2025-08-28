using System.Collections;
using UnityEngine;

public class ActionFireball : ActionGroundAttack
{
	private void Reset()
	{
		// Defaults tailored for fireball
		actionDisplayName = "Fireball";
		maxRange = 6;
		radius = 2;
		baseDamage = 12;
	}

	public override string DisplayName()
	{
		return string.IsNullOrEmpty(actionDisplayName) ? "Fireball" : actionDisplayName;
	}

	override public string Description()
	{
		string desc = base.Description();
		desc += $"Fireball deals {baseDamage} damage to all enemies in a {radius}-tile radius. Max range: {maxRange}.";
		return desc;
	}
}
