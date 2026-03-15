using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionGroundAttack : ActionAttack
{
	[SerializeField] public int radius = 1;

	public override int AOE_RADIUS { get { return radius; } }

	public override TargetType TARGET_TYPE { get { return TargetType.GROUND_TILE; } }

	private AbilityData abilityData;

	public void ConfigureFromAbility(AbilityData data)
	{
		abilityData = data;
		actionDisplayName = data.displayName;
		minRange = data.minRange;
		maxRange = data.maxRange;
		radius = data.radius;
		BASE_ACTION_COST = data.actionPointCost;
	}

	public override bool RequiresLineOfSight { get { return true; } }
	public override bool TargetsEnemiesOnly { get { return false; } }
	public override bool CanTargetEmptyTiles { get { return true; } }

	protected override void PerformAttack(Tile targetTile)
	{
		ApplyAreaDamage(targetTile);
		StartCoroutine(PlayAoEVisual(targetTile));
	}

	private void ApplyAreaDamage(Tile center)
	{
		if (center == null) return;
		int dist = CalculateManhattanDistance(combatController.GetCurrentTile(), center);
		var context = AttackContext.AreaEffect(dist, abilityData);
		var affected = AttackPreviewHelper.EnumerateAoETiles(center, radius);
		foreach (Tile t in affected)
		{
			var occupant = t.occupant;
			var popupTarget = occupant != null ? occupant.transform : null;

			if (occupant?.characterSheet != null)
			{
				var result = AttackResolver.Resolve(characterSheet, occupant.characterSheet, context);
				CombatLog.Log(result.logMessage);

				if (result.hit && popupTarget != null)
				{
					string text = result.critical ? $"CRIT {result.damageDealt}" : result.damageDealt.ToString();
					PopupTextController.CreatePopupText(text, popupTarget);
				}
			}
		}
	}

	protected override void OnTargetsAttacked(Tile targetTile)
	{
		if (targetTile == null) return;
		var affected = AttackPreviewHelper.EnumerateAoETiles(targetTile, radius);
		foreach (Tile t in affected)
		{
			if (t.occupant != null)
			{
				MakeUnitFaceThisActor(t.occupant);
			}
		}
	}

	private IEnumerator PlayAoEVisual(Tile center)
	{
		if (center == null) yield break;
		yield return VfxHelpers.AoEExpandingRing(center.transform.position, radius, 0.35f);
	}


	protected override float GetAttackDuration()
	{
		return 0.45f;
	}

	protected override bool UsesProjectile()
	{
		return true;
	}

	protected override IEnumerator SpawnProjectileAndWait(Vector3 from, Vector3 to)
	{
		yield return SpawnMusketProjectileAndWait(from, to);
	}

	public override string Description()
	{
		string desc = base.Description();
		if (abilityData != null)
		{
			desc = $"{abilityData.displayName} deals {abilityData.damage} damage in a {radius}-tile radius.";
		}
		else
		{
			desc += $" in a {radius}-tile radius.";
		}
		return desc;
	}
}
