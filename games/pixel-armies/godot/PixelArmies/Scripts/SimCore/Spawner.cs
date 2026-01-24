using System;
using System.Collections.Generic;

namespace PixelArmies.SimCore;

public sealed class Spawner
{
	private const float OrbChunkValue = 1.0f;
	private const int MaxOrbsPerStep = 6;

	private readonly Side _side;
	private readonly ArmyDef _army;
	private readonly SimConfig _cfg;
	private readonly Rng _rng;

	private float _powerPool;
	private int _currentBucketTier;
	private UnitDef? _currentBucketUnit;
	private float _currentBucketProgress;
	private float _orbRemainder;

	private List<PowerAllocatedEvent> _powerAllocatedEvents = new();
	private List<PowerAllocatedEvent> _powerAllocatedEventsBuffer = new();
	private List<UnitSpawnedEvent> _unitSpawnedEvents = new();
	private List<UnitSpawnedEvent> _unitSpawnedEventsBuffer = new();

	public float PowerPool => _powerPool;
	public int CurrentBucketTier => _currentBucketTier;
	public float CurrentBucketProgress => _currentBucketProgress;
	public float CurrentTargetCost => _currentBucketUnit.HasValue ? _currentBucketUnit.Value.Cost : 0f;

	public Spawner(Side side, ArmyDef army, SimConfig cfg, Rng rng)
	{
		_side = side;
		_army = army;
		_cfg = cfg;
		_rng = rng;

		_powerPool = cfg.StartingPower;
		_currentBucketTier = 1;
		_currentBucketUnit = null;
		_currentBucketProgress = 0f;
	}

	public void Step(BattleState s, float dt)
	{
		_powerPool += _cfg.PowerPerSecond * dt;

		int unlockedTier = _cfg.UnlockedTierForTime(s.Time);

		EnsureBucketTarget(unlockedTier);

		float alloc = _powerPool;
		if (alloc <= 0f) return;
		_powerPool -= alloc;
		_currentBucketProgress += alloc;
		EmitPowerAllocatedEvents(alloc);

		int spawns = 0;
		while (_currentBucketUnit != null &&
			_currentBucketProgress >= _currentBucketUnit.Value.Cost &&
			spawns < 5)
		{
			var unit = _currentBucketUnit.Value;
			_currentBucketProgress -= unit.Cost;

			float spawnX = _side == Side.Left ? 0f : _cfg.BattlefieldLength;
			var spawned = new UnitState(s.NextUnitId++, _side, unit, spawnX);
			s.Units.Add(spawned);
			_unitSpawnedEvents.Add(new UnitSpawnedEvent(_side, unit.Tier, spawned.Id, unit.Id));
			spawns++;

			SelectNewBucketTarget(unlockedTier);
		}
	}

	private void EmitPowerAllocatedEvents(float allocatedPower)
	{
		_orbRemainder += allocatedPower;
		int emitted = 0;
		while (_orbRemainder >= OrbChunkValue && emitted < MaxOrbsPerStep)
		{
			_powerAllocatedEvents.Add(new PowerAllocatedEvent(_side, _currentBucketTier, OrbChunkValue));
			_orbRemainder -= OrbChunkValue;
			emitted++;
		}
	}

	public IReadOnlyList<PowerAllocatedEvent> ConsumePowerAllocatedEvents()
	{
		var events = _powerAllocatedEvents;
		_powerAllocatedEvents = _powerAllocatedEventsBuffer;
		_powerAllocatedEventsBuffer = events;
		_powerAllocatedEvents.Clear();
		return events;
	}

	public IReadOnlyList<UnitSpawnedEvent> ConsumeUnitSpawnedEvents()
	{
		var events = _unitSpawnedEvents;
		_unitSpawnedEvents = _unitSpawnedEventsBuffer;
		_unitSpawnedEventsBuffer = events;
		_unitSpawnedEvents.Clear();
		return events;
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

	private void EnsureBucketTarget(int unlockedTier)
	{
		if (_currentBucketTier > unlockedTier) _currentBucketTier = unlockedTier;
		if (_currentBucketUnit == null || _currentBucketUnit.Value.Tier != _currentBucketTier)
		{
			SelectNewBucketTarget(unlockedTier);
		}
	}

	private void SelectNewBucketTarget(int unlockedTier)
	{
		int chosenTier = PickTier(unlockedTier);
		for (int tier = chosenTier; tier >= 1; tier--)
		{
			var candidates = new List<UnitDef>();
			for (int i = 0; i < _army.Units.Count; i++)
			{
				var u = _army.Units[i];
				if (u.Tier == tier) candidates.Add(u);
			}
			if (candidates.Count == 0) continue;

			_currentBucketTier = tier;
			_currentBucketUnit = candidates[_rng.NextInt(0, candidates.Count)];
			return;
		}

		throw new InvalidOperationException($"Army '{_army.Name}' has no units in tiers 1..{unlockedTier}");
	}
}
