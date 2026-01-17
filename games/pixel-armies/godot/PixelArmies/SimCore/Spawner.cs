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

		// Choose among affordable units. Weight slightly toward higher tier over time.
		var affordable = new List<(UnitDef item, float weight)>();
		foreach (var u in _army.Units)
		{
			if (u.Cost <= _power)
			{
				// Weight: prefer higher tier but still allow variety
				float tierBoost = 1f + (u.Tier - 1) * 0.35f;
				float costPenalty = 1f / Math.Max(1f, u.Cost);
				affordable.Add((u, tierBoost * costPenalty));
			}
		}
		if (affordable.Count == 0) return;

		var chosen = _rng.PickWeighted(affordable);

		_power -= chosen.Cost;

		float spawnX = _side == Side.Left ? 0f : _cfg.BattlefieldLength;
		s.Units.Add(new UnitState(s.NextUnitId++, _side, chosen, spawnX));
	}
}
