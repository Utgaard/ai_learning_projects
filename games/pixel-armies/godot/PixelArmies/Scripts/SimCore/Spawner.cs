using System;
using System.Collections.Generic;

namespace PixelArmies.SimCore;

public sealed class Spawner
{
	private readonly Side _side;
	private readonly ArmyDef _army;
	private readonly SimConfig _cfg;
	private readonly Rng _rng;

	private float _power;
	private float _spawnTimer;

	public Spawner(Side side, ArmyDef army, SimConfig cfg, Rng rng)
	{
		_side = side;
		_army = army;
		_cfg = cfg;
		_rng = rng;

		_power = cfg.StartingPower;
		_spawnTimer = 0f;
	}

	public void Step(BattleState s, float dt)
	{
		_power += _cfg.PowerGainPerSecond * dt;
		_spawnTimer -= dt;

		if (_spawnTimer > 0f) return;
		_spawnTimer = _cfg.SpawnTryInterval;

		// If we can't afford even the cheapest unit, do nothing this tick
		if (_power < _army.MinCost()) return;

		int unlockedTier = _cfg.UnlockedTierForTime(s.Time);
		int chosenTier = PickTier(unlockedTier);

		UnitDef? chosen = null;
		for (int tier = chosenTier; tier >= 1; tier--)
		{
			var candidates = new List<(UnitDef item, float weight)>();
			foreach (var u in _army.Units)
			{
				if (u.Tier != tier) continue;
				if (u.Cost > _power) continue;
				candidates.Add((u, 1f));
			}
			if (candidates.Count == 0) continue;

			chosen = _rng.PickWeighted(candidates);
			break;
		}
		if (chosen == null) return;

		_power -= chosen.Value.Cost;

		float spawnX = _side == Side.Left ? 0f : _cfg.BattlefieldLength;
		s.Units.Add(new UnitState(s.NextUnitId++, _side, chosen.Value, spawnX));
	}

	private static float TierWeight(int tier)
	{
		return tier switch
		{
			1 => 6f,
			2 => 4f,
			3 => 2f,
			4 => 1f,
			_ => 1f
		};
	}

	private int PickTier(int unlockedTier)
	{
		int maxTier = Math.Clamp(unlockedTier, 1, 4);
		var weighted = new List<(int item, float weight)>(maxTier);
		for (int tier = 1; tier <= maxTier; tier++)
		{
			weighted.Add((tier, TierWeight(tier)));
		}
		return _rng.PickWeighted(weighted);
	}
}
