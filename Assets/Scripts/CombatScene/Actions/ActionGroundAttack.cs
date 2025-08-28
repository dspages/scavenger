using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Action for ground-attack spells that target a tile and optionally affect a radius
public class ActionGroundAttack : ActionAttack
{
	[SerializeField] public int radius = 1;   // How far the effect spreads from impact tile (tile + adjacencies, etc.)

	public override TargetType TARGET_TYPE { get { return TargetType.GROUND_TILE; } }

	// Ground attacks can target any tile and require line of sight
	public override bool RequiresLineOfSight { get { return true; } }
	public override bool TargetsEnemiesOnly { get { return false; } }
	public override bool CanTargetEmptyTiles { get { return true; } }

	protected override void PerformAttack(Tile targetTile)
	{
		ApplyAreaDamage(targetTile);
	}

	private void ApplyAreaDamage(Tile center)
	{
		// Collect all tiles within radius using layered BFS from the center tile
		Queue<Tile> queue = new Queue<Tile>();
		HashSet<Tile> visited = new HashSet<Tile>();
		Dictionary<Tile, int> depth = new Dictionary<Tile, int>();
		queue.Enqueue(center);
		visited.Add(center);
		depth[center] = 0;

		List<Tile> affected = new List<Tile>();
		while (queue.Count > 0)
		{
			Tile t = queue.Dequeue();
			affected.Add(t);
			int d = depth[t];
			if (d >= radius) continue;
			foreach (Tile n in t.Neighbors())
			{
				if (n == null || visited.Contains(n)) continue;
				visited.Add(n);
				depth[n] = d + 1;
				queue.Enqueue(n);
			}
		}

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

	protected override float GetAttackDuration()
	{
		return 0.35f;
	}

	public override string Description()
	{
		string desc = base.Description();
		desc += $" in a {radius}-tile radius.";
		return desc;
	}
}