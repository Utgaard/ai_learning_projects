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
		var leftUnits = new List<UnitState>();
		var rightUnits = new List<UnitState>();
		for (int i = 0; i < units.Count; i++)
		{
			var u = units[i];
			if (!u.Alive) continue;
			if (u.Side == Side.Left) leftUnits.Add(u);
			else rightUnits.Add(u);
		}

		leftUnits.Sort(CompareByXThenId);
		rightUnits.Sort(CompareByXThenId);

		EnforceSpacing(leftUnits, isLeftSide: true);
		EnforceSpacing(rightUnits, isLeftSide: false);

		var leftIndex = new Dictionary<int, int>(leftUnits.Count);
		var rightIndex = new Dictionary<int, int>(rightUnits.Count);
		for (int i = 0; i < leftUnits.Count; i++) leftIndex[leftUnits[i].Id] = i;
		for (int i = 0; i < rightUnits.Count; i++) rightIndex[rightUnits[i].Id] = i;

		var newX = new float[units.Count];

		// Combat + movement
		for (int i = 0; i < units.Count; i++)
		{
			var u = units[i];
			newX[i] = u.X;
			if (!u.Alive) continue;

			float dir = u.Side == Side.Left ? 1f : -1f;
			float enemyBaseX = u.Side == Side.Left ? _cfg.BattlefieldLength : 0f;

			var target = FindNearestEnemyInFront(u, units, dir, out float targetDist);
			float selfRadius = UnitSpacingRadius(u);
			float targetRadius = target != null ? UnitSpacingRadius(target) : 0f;
			bool hasEnemyInRange = target != null && targetDist <= u.Def.Range;
			bool inContact = target != null && targetDist <= selfRadius + targetRadius;

			if (hasEnemyInRange)
			{
				float dmg = u.Def.Dps * dt;
				target!.Hp -= dmg;
				continue;
			}

			if (target == null)
			{
				float distToBase = Math.Abs(enemyBaseX - u.X);
				if (distToBase <= _cfg.BaseAttackRange)
				{
					float dmg = u.Def.Dps * dt;
					if (u.Side == Side.Left) State.RightBaseHp -= dmg;
					else State.LeftBaseHp -= dmg;
					continue;
				}
			}

			float desiredX = u.X;
			if (!inContact)
			{
				float moveDist = u.Def.Speed * dt;
				moveDist = ClampMoveByAllySpacing(u, moveDist, leftUnits, rightUnits, leftIndex, rightIndex);
				desiredX = u.X + dir * moveDist;
			}

			if (target != null)
			{
				float minDist = selfRadius + targetRadius;
				if (dir > 0f) desiredX = Math.Min(desiredX, target.X - minDist);
				else desiredX = Math.Max(desiredX, target.X + minDist);
			}

			if (u.Side == Side.Left) desiredX = Math.Min(desiredX, _cfg.BattlefieldLength);
			else desiredX = Math.Max(desiredX, 0f);

			newX[i] = desiredX;
		}

		for (int i = 0; i < units.Count; i++)
		{
			if (units[i].Alive) units[i].X = newX[i];
		}

		leftUnits.Clear();
		rightUnits.Clear();
		for (int i = 0; i < units.Count; i++)
		{
			var u = units[i];
			if (!u.Alive) continue;
			if (u.Side == Side.Left) leftUnits.Add(u);
			else rightUnits.Add(u);
		}
		leftUnits.Sort(CompareByXThenId);
		rightUnits.Sort(CompareByXThenId);
		EnforceSpacing(leftUnits, isLeftSide: true);
		EnforceSpacing(rightUnits, isLeftSide: false);

		// Cleanup dead units occasionally (cheap)
		// (Could do each step for now)
		for (int i = units.Count - 1; i >= 0; i--)
		{
			if (!units[i].Alive) units.RemoveAt(i);
		}
	}

	private float UnitSpacingRadius(UnitState unit)
	{
		float r = _cfg.UnitRadiusForTier(unit.Def.Tier);
		float mul = unit.Def.FormationSpacingMul;
		if (mul <= 0f) mul = 1f;
		return r * mul;
	}

	private static int CompareByXThenId(UnitState a, UnitState b)
	{
		int cmp = a.X.CompareTo(b.X);
		if (cmp != 0) return cmp;
		return a.Id.CompareTo(b.Id);
	}

	private void EnforceSpacing(List<UnitState> sideUnits, bool isLeftSide)
	{
		if (sideUnits.Count < 2) return;

		if (isLeftSide)
		{
			for (int i = sideUnits.Count - 2; i >= 0; i--)
			{
				var back = sideUnits[i];
				var front = sideUnits[i + 1];
				float minGap = UnitSpacingRadius(back) + UnitSpacingRadius(front);
				float maxBackX = front.X - minGap;
				if (back.X > maxBackX) back.X = maxBackX;
			}
		}
		else
		{
			for (int i = 1; i < sideUnits.Count; i++)
			{
				var front = sideUnits[i - 1];
				var back = sideUnits[i];
				float minGap = UnitSpacingRadius(back) + UnitSpacingRadius(front);
				float minBackX = front.X + minGap;
				if (back.X < minBackX) back.X = minBackX;
			}
		}
	}

	private static UnitState? FindNearestEnemyInFront(UnitState u, List<UnitState> units, float dir, out float dist)
	{
		dist = float.MaxValue;
		UnitState? target = null;

		for (int i = 0; i < units.Count; i++)
		{
			var e = units[i];
			if (!e.Alive) continue;
			if (e.Side == u.Side) continue;

			float d = (e.X - u.X) * dir;
			if (d <= 0f) continue;

			if (d < dist)
			{
				dist = d;
				target = e;
			}
		}

		return target;
	}

	private float ClampMoveByAllySpacing(
		UnitState u,
		float moveDist,
		List<UnitState> leftUnits,
		List<UnitState> rightUnits,
		Dictionary<int, int> leftIndex,
		Dictionary<int, int> rightIndex)
	{
		if (moveDist <= 0f) return 0f;

		if (u.Side == Side.Left)
		{
			if (leftIndex.TryGetValue(u.Id, out int idx) && idx < leftUnits.Count - 1)
			{
				var front = leftUnits[idx + 1];
				float minGap = UnitSpacingRadius(u) + UnitSpacingRadius(front);
				float gap = front.X - u.X;
				float maxAdvance = gap - minGap;
				if (maxAdvance <= 0f) return 0f;
				return Math.Min(moveDist, maxAdvance);
			}
		}
		else
		{
			if (rightIndex.TryGetValue(u.Id, out int idx) && idx > 0)
			{
				var front = rightUnits[idx - 1];
				float minGap = UnitSpacingRadius(u) + UnitSpacingRadius(front);
				float gap = u.X - front.X;
				float maxAdvance = gap - minGap;
				if (maxAdvance <= 0f) return 0f;
				return Math.Min(moveDist, maxAdvance);
			}
		}

		return moveDist;
	}
}
