using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionFireball : ActionGroundAttack
{
	public override void ConfigureAction()
	{
		// Configure fireball-specific properties
		actionDisplayName = "Fireball";
		minRange = 3;
		maxRange = 6;
		radius = 2;
		baseDamage = 12;
		BASE_ACTION_COST = 20; // Base action cost (excludes movement)
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
