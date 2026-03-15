public class ActionFireball : ActionGroundAttack
{
	public override void ConfigureAction()
	{
		actionDisplayName = "Fireball";
		minRange = 3;
		maxRange = 6;
		radius = 2;
		BASE_ACTION_COST = 20;
	}

	public override string DisplayName()
	{
		return string.IsNullOrEmpty(actionDisplayName) ? "Fireball" : actionDisplayName;
	}

	override public string Description()
	{
		return $"Fireball deals fire damage to all enemies in a {radius}-tile radius. Max range: {maxRange}.";
	}
}
