using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Action for ground-attack spells that target a tile and optionally affect a radius
public class ActionGroundAttack : ActionAttack
{
	[SerializeField] public int radius = 1;   // How far the effect spreads from impact tile (tile + adjacencies, etc.)

	public override int AOE_RADIUS { get { return radius; } }

	public override TargetType TARGET_TYPE { get { return TargetType.GROUND_TILE; } }

	// Ground attacks can target any tile and require line of sight
	public override bool RequiresLineOfSight { get { return true; } }
	public override bool TargetsEnemiesOnly { get { return false; } }
	public override bool CanTargetEmptyTiles { get { return true; } }

	protected override void PerformAttack(Tile targetTile)
	{
		// Damage should be applied on projectile arrival. The base AttackSequence
		// already waits for SpawnProjectileAndWait before calling PerformAttack,
		// so just apply damage and spawn the AoE effect here.
		ApplyAreaDamage(targetTile);
		StartCoroutine(PlayAoEVisual(targetTile));
	}

	private void ApplyAreaDamage(Tile center)
	{
		if (center == null) return;
		var affected = AttackPreviewHelper.EnumerateAoETiles(center, radius);
		foreach (Tile t in affected)
		{
			if (t.occupant != null)
			{
				CharacterSheet target = t.occupant.characterSheet;
				if (target != null)
				{
					target.ReceiveDamage(baseDamage);
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
		// Reuse the bright whooshing ball for ground attacks as well (thrown grenade feel)
		yield return SpawnMusketProjectileAndWait(from, to);
	}

	public override string Description()
	{
		string desc = base.Description();
		desc += $" in a {radius}-tile radius.";
		return desc;
	}
}