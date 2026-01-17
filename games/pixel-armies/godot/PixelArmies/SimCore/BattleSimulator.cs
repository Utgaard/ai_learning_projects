#nullable enable

using System;
using System.Collections.Generic;

namespace PixelArmies.SimCore;

public sealed class BattleSimulator
{
	private readonly SimConfig _cfg;
	private readonly ArmyDef _leftArmy;
	private readonly ArmyDef _rightArmy;
	private readonly Rng _rng;

	private readonly Spawner _leftSpawner;
	private readonly Spawner _rightSpawner;

	public BattleState State { get; }

	public BattleSimulator(SimConfig cfg, ArmyDef leftArmy, ArmyDef rightArmy, int seed)
	{
		_cfg = cfg;
		_leftArmy = leftArmy;
		_rightArmy = rightArmy;
		_rng = new Rng(seed);

		State = new BattleState(cfg.BaseMaxHp);

		_leftSpawner = new Spawner(Side.Left, _leftArmy, _cfg, _rng);
		_rightSpawner = new Spawner(Side.Right, _rightArmy, _cfg, _rng);
	}

	public void Step(float dt)
	{
		if (State.IsOver) return;

		State.Time += dt;

		// Spawn
		_leftSpawner.Step(State, dt);
		_rightSpawner.Step(State, dt);

		// Build quick lists
		// (For now O(n^2) targeting is OK; we can optimize later)
		var units = State.Units;

		// Combat + movement
		for (int i = 0; i < units.Count; i++)
		{
			var u = units[i];
			if (!u.Alive) continue;

			// Find nearest enemy in front (simple)
			UnitState? target = null;
			float bestDist = float.MaxValue;

			for (int j = 0; j < units.Count; j++)
			{
				if (i == j) continue;
				var e = units[j];
				if (!e.Alive) continue;
				if (e.Side == u.Side) continue;

				float d = Math.Abs(e.X - u.X);
				if (d < bestDist)
				{
					bestDist = d;
					target = e;
				}
			}

			bool hasEnemyInRange = target != null && bestDist <= u.Def.Range;

			// Base position & direction
			float enemyBaseX = u.Side == Side.Left ? _cfg.BattlefieldLength : 0f;
			float dir = u.Side == Side.Left ? 1f : -1f;

			// If can hit enemy unit: deal damage
			if (hasEnemyInRange)
			{
				float dmg = u.Def.Dps * dt;
				target!.Hp -= dmg;
			}
			else
			{
				// If near enemy base: attack base; else move forward
				float distToBase = Math.Abs(enemyBaseX - u.X);
				if (distToBase <= _cfg.BaseAttackRange)
				{
					float dmg = u.Def.Dps * dt;
					if (u.Side == Side.Left) State.RightBaseHp -= dmg;
					else State.LeftBaseHp -= dmg;
				}
				else
				{
					u.X += dir * u.Def.Speed * dt;
					u.X = Math.Clamp(u.X, 0f, _cfg.BattlefieldLength);
				}
			}
		}

		// Cleanup dead units occasionally (cheap)
		// (Could do each step for now)
		for (int i = units.Count - 1; i >= 0; i--)
		{
			if (!units[i].Alive) units.RemoveAt(i);
		}
	}
}
