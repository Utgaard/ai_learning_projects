#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace PixelArmies.SimCore;

public sealed class BattleSimulator
{
	private const float RangedMinRange = 80f;
	private const float MinAttackRate = 0.1f;
	private readonly DebugSettings _debugSettings;
	private float _nextDebugPrintTime;

	private readonly SimConfig _cfg;
	private readonly ArmyDef _leftArmy;
	private readonly ArmyDef _rightArmy;
	private readonly Rng _rng;

	private readonly Spawner _leftSpawner;
	private readonly Spawner _rightSpawner;

	private List<DamageEvent> _damageEvents = new();
	private List<DamageEvent> _damageEventsBuffer = new();
	private List<UnitDiedEvent> _unitDiedEvents = new();
	private List<UnitDiedEvent> _unitDiedEventsBuffer = new();
	private readonly List<PowerAllocatedEvent> _powerAllocatedEvents = new();
	private readonly List<UnitSpawnedEvent> _unitSpawnedEvents = new();

	public BattleState State { get; }
	public Spawner LeftSpawner => _leftSpawner;
	public Spawner RightSpawner => _rightSpawner;
	public ArmyDef LeftArmy => _leftArmy;
	public ArmyDef RightArmy => _rightArmy;

	public BattleSimulator(SimConfig cfg, ArmyDef leftArmy, ArmyDef rightArmy, int seed, DebugSettings debugSettings = default)
	{
		_cfg = cfg;
		_leftArmy = leftArmy;
		_rightArmy = rightArmy;
		_rng = new Rng(seed);

		State = new BattleState(cfg.BaseMaxHp);

		_leftSpawner = new Spawner(Side.Left, _leftArmy, _cfg, _rng);
		_rightSpawner = new Spawner(Side.Right, _rightArmy, _cfg, _rng);

		_debugSettings = debugSettings.Enabled && debugSettings.IntervalSeconds > 0f
			? debugSettings
			: DebugSettings.Disabled;
		_nextDebugPrintTime = _debugSettings.Enabled ? _debugSettings.IntervalSeconds : float.PositiveInfinity;
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
		var leftGround = new List<UnitState>();
		var rightGround = new List<UnitState>();
		var leftAir = new List<UnitState>();
		var rightAir = new List<UnitState>();
		for (int i = 0; i < units.Count; i++)
		{
			var u = units[i];
			if (!u.Alive) continue;
			if (u.Side == Side.Left) leftUnits.Add(u);
			else rightUnits.Add(u);

			if (u.Def.MovementClass == MovementClass.Ground)
			{
				if (u.Side == Side.Left) leftGround.Add(u);
				else rightGround.Add(u);
			}
			else
			{
				if (u.Side == Side.Left) leftAir.Add(u);
				else rightAir.Add(u);
			}
		}

		leftUnits.Sort(CompareByXThenId);
		rightUnits.Sort(CompareByXThenId);
		leftGround.Sort(CompareByXThenId);
		rightGround.Sort(CompareByXThenId);
		leftAir.Sort(CompareByXThenId);
		rightAir.Sort(CompareByXThenId);

		var formationMulByUnitId = BuildFormationMultipliers(leftUnits, rightUnits);
		var allySpacingMulByUnitId = BuildAllySpacingMultipliers(leftUnits, rightUnits, formationMulByUnitId);

		EnforceSpacing(leftGround, isLeftSide: true, allySpacingMulByUnitId);
		EnforceSpacing(rightGround, isLeftSide: false, allySpacingMulByUnitId);
		EnforceAirSpacing(leftAir, formationMulByUnitId);
		EnforceAirSpacing(rightAir, formationMulByUnitId);

		var leftIndex = new Dictionary<int, int>(leftGround.Count);
		var rightIndex = new Dictionary<int, int>(rightGround.Count);
		for (int i = 0; i < leftGround.Count; i++) leftIndex[leftGround[i].Id] = i;
		for (int i = 0; i < rightGround.Count; i++) rightIndex[rightGround[i].Id] = i;

		var newX = new float[units.Count];

		// Combat + movement
		for (int i = 0; i < units.Count; i++)
		{
			var u = units[i];
			newX[i] = u.X;
			if (!u.Alive) continue;

			float dir = u.Side == Side.Left ? 1f : -1f;
			float enemyBaseX = u.Side == Side.Left ? _cfg.BattlefieldLength : 0f;

			var target = SelectTarget(u, units, out float targetDist);
			float selfRadius = UnitSpacingRadius(u, formationMulByUnitId);
			float targetRadius = target != null ? UnitSpacingRadius(target, formationMulByUnitId) : 0f;
			float attackRange = EffectiveRange(u.Def);
			bool hasEnemyInRange = target != null && targetDist <= attackRange;
			bool inContact = target != null && targetDist <= selfRadius + targetRadius;

			if (!hasEnemyInRange)
			{
				var contactTarget = FindContactEnemy(u, units, selfRadius, formationMulByUnitId);
				if (contactTarget != null)
				{
					target = contactTarget;
					targetRadius = UnitSpacingRadius(contactTarget, formationMulByUnitId);
					inContact = true;
					hasEnemyInRange = true;
				}
			}

			u.AttackCooldown = Math.Max(0f, u.AttackCooldown - dt);

			if (hasEnemyInRange)
			{
				if (u.AttackCooldown <= 0f)
				{
					float dmg = u.Def.Damage;
					bool wasAlive = target!.Hp > 0f;
					float applied = wasAlive ? MathF.Min(dmg, target.Hp) : 0f;
					target!.Hp -= dmg;
					RecordDamage(u.Side, applied);
					_damageEvents.Add(new DamageEvent(u.Id, target.Id, dmg, IsRanged(u.Def)));
					if (wasAlive && target.Hp <= 0f)
					{
						RecordKill(u.Side);
						_unitDiedEvents.Add(new UnitDiedEvent(target.Id, u.Id));
					}
					u.AttackCooldown = AttackCooldownFor(u.Def);
				}
				continue;
			}

			if (target == null)
			{
				float distToBase = Math.Abs(enemyBaseX - u.X);
				if (distToBase <= _cfg.BaseAttackRange)
				{
					if (u.AttackCooldown <= 0f)
					{
						float dmg = u.Def.Damage;
						if (u.Side == Side.Left)
						{
							float before = State.RightBaseHp;
							State.RightBaseHp -= dmg;
							RecordDamage(u.Side, MathF.Min(dmg, before));
						}
						else
						{
							float before = State.LeftBaseHp;
							State.LeftBaseHp -= dmg;
							RecordDamage(u.Side, MathF.Min(dmg, before));
						}
						u.AttackCooldown = AttackCooldownFor(u.Def);
					}
					continue;
				}
			}

			float desiredX = u.X;
			if (!inContact)
			{
				float moveDist = u.Def.Speed * dt;
				moveDist = ClampMoveByAllySpacing(u, moveDist, leftGround, rightGround, leftIndex, rightIndex, allySpacingMulByUnitId);
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
		leftGround.Clear();
		rightGround.Clear();
		leftAir.Clear();
		rightAir.Clear();
		for (int i = 0; i < units.Count; i++)
		{
			var u = units[i];
			if (!u.Alive) continue;
			if (u.Side == Side.Left) leftUnits.Add(u);
			else rightUnits.Add(u);

			if (u.Def.MovementClass == MovementClass.Ground)
			{
				if (u.Side == Side.Left) leftGround.Add(u);
				else rightGround.Add(u);
			}
			else
			{
				if (u.Side == Side.Left) leftAir.Add(u);
				else rightAir.Add(u);
			}
		}
		leftUnits.Sort(CompareByXThenId);
		rightUnits.Sort(CompareByXThenId);
		leftGround.Sort(CompareByXThenId);
		rightGround.Sort(CompareByXThenId);
		leftAir.Sort(CompareByXThenId);
		rightAir.Sort(CompareByXThenId);
		formationMulByUnitId = BuildFormationMultipliers(leftUnits, rightUnits);
		allySpacingMulByUnitId = BuildAllySpacingMultipliers(leftUnits, rightUnits, formationMulByUnitId);
		EnforceSpacing(leftGround, isLeftSide: true, allySpacingMulByUnitId);
		EnforceSpacing(rightGround, isLeftSide: false, allySpacingMulByUnitId);
		EnforceAirSpacing(leftAir, formationMulByUnitId);
		EnforceAirSpacing(rightAir, formationMulByUnitId);

		// Cleanup dead units occasionally (cheap)
		// (Could do each step for now)
		for (int i = units.Count - 1; i >= 0; i--)
		{
			if (!units[i].Alive) units.RemoveAt(i);
		}

		if (_debugSettings.Enabled && State.Time >= _nextDebugPrintTime)
		{
			PrintDebugLines();
		}
	}

	private float UnitSpacingRadius(UnitState unit, Dictionary<int, float> spacingMulByUnitId)
	{
		float r = _cfg.UnitRadiusForTier(unit.Def.Tier);
		float mul = 1f;
		if (!spacingMulByUnitId.TryGetValue(unit.Id, out mul)) mul = 1f;
		return r * mul;
	}

	private static int CompareByXThenId(UnitState a, UnitState b)
	{
		int cmp = a.X.CompareTo(b.X);
		if (cmp != 0) return cmp;
		return a.Id.CompareTo(b.Id);
	}

	private void EnforceSpacing(List<UnitState> sideUnits, bool isLeftSide, Dictionary<int, float> spacingMulByUnitId)
	{
		if (sideUnits.Count < 2) return;

		if (isLeftSide)
		{
			for (int i = sideUnits.Count - 2; i >= 0; i--)
			{
				var back = sideUnits[i];
				var front = sideUnits[i + 1];
				float minGap = UnitSpacingRadius(back, spacingMulByUnitId) + UnitSpacingRadius(front, spacingMulByUnitId);
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
				float minGap = UnitSpacingRadius(back, spacingMulByUnitId) + UnitSpacingRadius(front, spacingMulByUnitId);
				float minBackX = front.X + minGap;
				if (back.X < minBackX) back.X = minBackX;
			}
		}
	}

	private static UnitState? SelectTarget(UnitState attacker, List<UnitState> units, out float dist)
	{
		dist = float.MaxValue;
		switch (attacker.Def.TargetingPolicy)
		{
			case TargetingPolicy.Frontmost:
				return SelectFrontmost(attacker, units, out dist);
			case TargetingPolicy.ClosestInRange:
				return SelectClosest(attacker, units, inRangeOnly: true, out dist);
			default:
				return SelectClosest(attacker, units, inRangeOnly: false, out dist);
		}
	}

	private static UnitState? SelectFrontmost(UnitState attacker, List<UnitState> units, out float dist)
	{
		dist = float.MaxValue;
		UnitState? target = null;
		bool left = attacker.Side == Side.Left;

		for (int i = 0; i < units.Count; i++)
		{
			var e = units[i];
			if (!e.Alive) continue;
			if (e.Side == attacker.Side) continue;

			float cmp = left ? e.X : -e.X;
			float best = target == null ? float.MaxValue : (left ? target.X : -target.X);

			if (target == null || cmp < best || (Math.Abs(cmp - best) < 0.0001f && e.Id < target.Id))
			{
				target = e;
				dist = Math.Abs(e.X - attacker.X);
			}
		}

		return target;
	}

	private static UnitState? SelectClosest(UnitState attacker, List<UnitState> units, bool inRangeOnly, out float dist)
	{
		dist = float.MaxValue;
		UnitState? target = null;
		float range = EffectiveRange(attacker.Def);

		for (int i = 0; i < units.Count; i++)
		{
			var e = units[i];
			if (!e.Alive) continue;
			if (e.Side == attacker.Side) continue;

			float d = Math.Abs(e.X - attacker.X);
			if (inRangeOnly && d > range) continue;

			if (d < dist || (Math.Abs(d - dist) < 0.0001f && (target == null || e.Id < target.Id)))
			{
				dist = d;
				target = e;
			}
		}

		return target;
	}

	private UnitState? FindContactEnemy(
		UnitState u,
		List<UnitState> units,
		float selfRadius,
		Dictionary<int, float> spacingMulByUnitId)
	{
		UnitState? target = null;
		float bestDist = float.MaxValue;

		for (int i = 0; i < units.Count; i++)
		{
			var e = units[i];
			if (!e.Alive) continue;
			if (e.Side == u.Side) continue;

			float dist = Math.Abs(e.X - u.X);
			float contactDist = selfRadius + UnitSpacingRadius(e, spacingMulByUnitId);
			if (dist <= contactDist && dist < bestDist)
			{
				bestDist = dist;
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
		Dictionary<int, int> rightIndex,
		Dictionary<int, float> spacingMulByUnitId)
	{
		if (moveDist <= 0f) return 0f;
		if (u.Def.MovementClass == MovementClass.Air) return moveDist;

		if (u.Side == Side.Left)
		{
			if (leftIndex.TryGetValue(u.Id, out int idx) && idx < leftUnits.Count - 1)
			{
				var front = leftUnits[idx + 1];
				float minGap = UnitSpacingRadius(u, spacingMulByUnitId) + UnitSpacingRadius(front, spacingMulByUnitId);
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
				float minGap = UnitSpacingRadius(u, spacingMulByUnitId) + UnitSpacingRadius(front, spacingMulByUnitId);
				float gap = u.X - front.X;
				float maxAdvance = gap - minGap;
				if (maxAdvance <= 0f) return 0f;
				return Math.Min(moveDist, maxAdvance);
			}
		}

		return moveDist;
	}

	private Dictionary<int, float> BuildFormationMultipliers(List<UnitState> leftUnits, List<UnitState> rightUnits)
	{
		var spacingMulByUnitId = new Dictionary<int, float>(leftUnits.Count + rightUnits.Count);

		for (int i = 0; i < leftUnits.Count; i++)
		{
			var u = leftUnits[i];
			spacingMulByUnitId[u.Id] = NormalizeSpacingMul(u.Def.FormationSpacingMul);
		}

		for (int i = 0; i < rightUnits.Count; i++)
		{
			var u = rightUnits[i];
			spacingMulByUnitId[u.Id] = NormalizeSpacingMul(u.Def.FormationSpacingMul);
		}
		return spacingMulByUnitId;
	}

	private Dictionary<int, float> BuildAllySpacingMultipliers(
		List<UnitState> leftUnits,
		List<UnitState> rightUnits,
		Dictionary<int, float> formationMulByUnitId)
	{
		var spacingMulByUnitId = new Dictionary<int, float>(formationMulByUnitId);
		ApplyVanguardSpacing(leftUnits, isLeftSide: true, spacingMulByUnitId);
		ApplyVanguardSpacing(rightUnits, isLeftSide: false, spacingMulByUnitId);
		return spacingMulByUnitId;
	}

	private void ApplyVanguardSpacing(
		List<UnitState> sideUnits,
		bool isLeftSide,
		Dictionary<int, float> spacingMulByUnitId)
	{
		if (sideUnits.Count == 0) return;

		float frontline = isLeftSide ? float.MinValue : float.MaxValue;
		for (int i = 0; i < sideUnits.Count; i++)
		{
			float x = sideUnits[i].X;
			if (isLeftSide) frontline = Math.Max(frontline, x);
			else frontline = Math.Min(frontline, x);
		}

		var byType = new Dictionary<string, List<UnitState>>();
		for (int i = 0; i < sideUnits.Count; i++)
		{
			var u = sideUnits[i];
			string key = u.Def.Id;
			if (!byType.TryGetValue(key, out var list))
			{
				list = new List<UnitState>();
				byType[key] = list;
			}
			list.Add(u);
		}

		foreach (var kvp in byType)
		{
			var list = kvp.Value;
			if (list.Count == 0) continue;

			int depth = list[0].Def.VanguardDepth;
			if (depth <= 0) continue;

			list.Sort((a, b) =>
			{
				float da = isLeftSide ? (frontline - a.X) : (a.X - frontline);
				float db = isLeftSide ? (frontline - b.X) : (b.X - frontline);
				int cmp = da.CompareTo(db);
				if (cmp != 0) return cmp;
				return a.Id.CompareTo(b.Id);
			});

			float vanguardMul = list[0].Def.VanguardSpacingMul;
			if (vanguardMul <= 0f) vanguardMul = list[0].Def.FormationSpacingMul;
			vanguardMul = NormalizeSpacingMul(vanguardMul);

			int count = Math.Min(depth, list.Count);
			for (int i = 0; i < count; i++)
			{
				spacingMulByUnitId[list[i].Id] = vanguardMul;
			}
		}
	}

	private static float NormalizeSpacingMul(float mul) => mul > 0f ? mul : 1f;

	private static bool IsRanged(UnitDef attacker) => EffectiveRange(attacker) >= RangedMinRange;

	private static float EffectiveRange(UnitDef def)
	{
		return def.WeaponLength > 0f ? def.WeaponLength : def.Range;
	}
	private static float AttackCooldownFor(UnitDef attacker)
	{
		float rate = attacker.AttackRate;
		if (rate < MinAttackRate) rate = MinAttackRate;
		return 1f / rate;
	}

	public IReadOnlyList<DamageEvent> ConsumeDamageEvents()
	{
		var events = _damageEvents;
		_damageEvents = _damageEventsBuffer;
		_damageEventsBuffer = events;
		_damageEvents.Clear();
		return events;
	}

	public IReadOnlyList<UnitDiedEvent> ConsumeUnitDiedEvents()
	{
		var events = _unitDiedEvents;
		_unitDiedEvents = _unitDiedEventsBuffer;
		_unitDiedEventsBuffer = events;
		_unitDiedEvents.Clear();
		return events;
	}

	public IReadOnlyList<PowerAllocatedEvent> ConsumePowerAllocatedEvents()
	{
		_powerAllocatedEvents.Clear();
		var left = _leftSpawner.ConsumePowerAllocatedEvents();
		if (left.Count > 0) _powerAllocatedEvents.AddRange(left);
		var right = _rightSpawner.ConsumePowerAllocatedEvents();
		if (right.Count > 0) _powerAllocatedEvents.AddRange(right);
		return _powerAllocatedEvents;
	}

	public IReadOnlyList<UnitSpawnedEvent> ConsumeUnitSpawnedEvents()
	{
		_unitSpawnedEvents.Clear();
		var left = _leftSpawner.ConsumeUnitSpawnedEvents();
		if (left.Count > 0) _unitSpawnedEvents.AddRange(left);
		var right = _rightSpawner.ConsumeUnitSpawnedEvents();
		if (right.Count > 0) _unitSpawnedEvents.AddRange(right);
		return _unitSpawnedEvents;
	}

	private void EnforceAirSpacing(List<UnitState> airUnits, Dictionary<int, float> spacingMulByUnitId)
	{
		for (int i = 0; i < airUnits.Count - 1; i++)
		{
			var a = airUnits[i];
			float aRadius = UnitSpacingRadius(a, spacingMulByUnitId);

			for (int j = i + 1; j < airUnits.Count; j++)
			{
				var b = airUnits[j];
				float bRadius = UnitSpacingRadius(b, spacingMulByUnitId);
				float minGap = aRadius + bRadius;
				float dx = b.X - a.X;
				if (Math.Abs(dx) >= minGap) continue;

				float move = (minGap - Math.Abs(dx)) * 0.5f;
				float dir = dx >= 0f ? 1f : -1f;

				a.X = Math.Clamp(a.X - move * dir, 0f, _cfg.BattlefieldLength);
				b.X = Math.Clamp(b.X + move * dir, 0f, _cfg.BattlefieldLength);

				airUnits[i] = a;
				airUnits[j] = b;
			}
		}
	}

	private void RecordDamage(Side side, float amount)
	{
		if (amount <= 0f) return;
		if (side == Side.Left) State.LeftDamageDealt += amount;
		else State.RightDamageDealt += amount;
	}

	private void RecordKill(Side side)
	{
		if (side == Side.Left) State.LeftKills++;
		else State.RightKills++;
	}

	private void PrintDebugLines()
	{
		while (State.Time >= _nextDebugPrintTime)
		{
			var snap = State.BuildDebugSnapshot();
			string prefix = _debugSettings.Prefix ?? "";
			string line = string.Format(
				CultureInfo.InvariantCulture,
				"{0}t={1,5:0.0}  L base={2,4:0}  units={3}  tiers=[{4},{5},{6},{7}]  |  R base={8,4:0}  units={9}  tiers=[{10},{11},{12},{13}]",
				prefix,
				_nextDebugPrintTime,
				snap.LeftBaseHp,
				snap.LeftTotal,
				snap.L1, snap.L2, snap.L3, snap.L4,
				snap.RightBaseHp,
				snap.RightTotal,
				snap.R1, snap.R2, snap.R3, snap.R4);

			Console.WriteLine(line);
			_nextDebugPrintTime += _debugSettings.IntervalSeconds;
		}
	}
}
