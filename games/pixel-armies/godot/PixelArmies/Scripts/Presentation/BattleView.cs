#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using PixelArmies.SimCore;
using SimSide = PixelArmies.SimCore.Side;

namespace PixelArmies.Presentation;

public partial class BattleView : Node2D
{
	private const float FlashDuration = 0.12f;
	private const float DamageAggregationWindow = 0.15f;
	private const float DamageNumberLifetime = 1.2f;
	private const float DamageNumberRiseSpeed = 35f;
	private const float TracerLifetime = 0.16f;
	private const float AttackSwingDuration = 0.14f;
	private const float AirAltitudeBase = -40f;
	private const float AirAltitudeStep = 2f;
	private const float AirBobAmplitude = 6f;
	private const float AirBobSpeed = 3.2f;
	private const float GroundBobAmplitudeMoving = 2.2f;
	private const float GroundBobAmplitudeIdle = 0.2f;
	private const float GroundBobFrequency = 2.6f;
	private const float MoveEpsilon = 0.5f;

	private BattleSimulator? _sim;
	private SimConfig? _cfg;
	private float _lastDt;

	private readonly Dictionary<int, Vector2> _unitPositions = new();
	private readonly Dictionary<int, Vector2> _prevUnitCenters = new();
	private readonly Dictionary<int, Vector2> _lastKnownPositions = new();
	private readonly Dictionary<int, UnitVisual> _lastKnownVisuals = new();
	private readonly Dictionary<int, UnitDef> _lastKnownDefs = new();
	private readonly Dictionary<int, SimSide> _lastKnownSides = new();
	private readonly Dictionary<int, float> _hitFlashTimers = new();
	private readonly Dictionary<int, float> _walkPhases = new();
	private readonly Dictionary<int, bool> _unitMoving = new();
	private readonly Dictionary<int, float> _attackTimers = new();
	private readonly Dictionary<int, DamageBucket> _damageBuckets = new();
	private readonly List<FloatingNumber> _floatingNumbers = new();
	private readonly List<Tracer> _tracers = new();
	private readonly List<int> _scratchKeys = new();
	private readonly HitReactionSystem _hitReactions = new();
	private readonly IUnitAnimationProfile[] _profiles =
	{
		new StickTier1Profile(),
		new DefaultRectProfile()
	};

	public float GroundY { get; private set; } = 120f;

	public void Configure(BattleSimulator sim, SimConfig cfg, float groundY)
	{
		_sim = sim;
		_cfg = cfg;
		GroundY = groundY;
	}

	public void Advance(float dt, IReadOnlyList<DamageEvent> damageEvents, IReadOnlyList<UnitDiedEvent> deathEvents)
	{
		if (_sim == null) return;
		_lastDt = dt;

		UpdateUnitPositions();

		if (damageEvents.Count > 0)
		{
			ApplyDamageEvents(damageEvents);
		}

		_hitReactions.Advance(dt, damageEvents, _unitPositions);
		HandleDeathEvents(deathEvents);

		UpdateFlashTimers(dt);
		UpdateAttackTimers(dt);
		UpdateDamageBuckets(dt);
		UpdateFloatingNumbers(dt);
		UpdateTracers(dt);
		UpdateProfiles(dt);
	}

	public override void _Draw()
	{
		if (_sim == null || _cfg == null) return;

		// Simple coordinate system:
		// X = sim X
		// Y = 0 is ground line; units drawn above it

		// Bases
		DrawRect(new Rect2(-30, GroundY - 60, 30, 60), Colors.White);
		DrawRect(new Rect2(_cfg.BattlefieldLength, GroundY - 60, 30, 60), Colors.White);

		// Base HP bars (simple)
		float barW = 120f;
		float barH = 10f;
		float lPct = Mathf.Clamp(_sim.State.LeftBaseHp / _cfg.BaseMaxHp, 0f, 1f);
		float rPct = Mathf.Clamp(_sim.State.RightBaseHp / _cfg.BaseMaxHp, 0f, 1f);

		DrawRect(new Rect2(10, GroundY + 20, barW, barH), Colors.Black);
		DrawRect(new Rect2(10, GroundY + 20, barW * lPct, barH), Colors.White);

		DrawRect(new Rect2(_cfg.BattlefieldLength - barW - 10, GroundY + 20, barW, barH), Colors.Black);
		DrawRect(new Rect2(_cfg.BattlefieldLength - barW - 10, GroundY + 20, barW * rPct, barH), Colors.White);

		var font = ThemeDB.FallbackFont;

		DrawTierOverlay(font);
		DrawTracers();

		// Units
		const int tierFontSize = 10;
		var drawContext = new UnitDrawContext(this, font, tierFontSize);

		foreach (var u in _sim.State.Units)
		{
			float y = GroundY;
			float h = 12;
			float w = 10;

			// Make big units bigger (tier proxy)
			w += (u.Def.Tier - 1) * 4;
			h += (u.Def.Tier - 1) * 4;

			if (u.Def.MovementClass == MovementClass.Air)
				y += AirAltitudeBase + GetAirJitter(u.Id);

			// Left units slightly different than right
			var c = u.Side == SimSide.Left ? Colors.Cyan : Colors.Orange;

			var center = _unitPositions.TryGetValue(u.Id, out var cached)
				? cached
				: GetUnitCenter(u, w, h, y);
			if (_hitReactions.TryGetRenderCenter(u.Id, out var renderCenter))
			{
				center = renderCenter;
			}

			bool moving = _unitMoving.TryGetValue(u.Id, out var mv) && mv;
			float phase = _walkPhases.TryGetValue(u.Id, out var ph) ? ph : 0f;
			float attackPhase = GetAttackPhase(u.Id);
			float flashAlpha = 0f;
			if (_hitFlashTimers.TryGetValue(u.Id, out float flash))
			{
				flashAlpha = Mathf.Clamp(flash / FlashDuration, 0f, 1f);
			}

			var profile = GetProfile(u.Def);
			var data = new UnitDrawData(
				center,
				w,
				h,
				c,
				u.Side,
				moving,
				phase,
				attackPhase,
				u.Def.WeaponLength,
				flashAlpha);
			profile.DrawUnit(drawContext, u, data);
		}

		DrawDamageNumbers(font);
		DrawProfileOverlays();

		// End text (minimal)
		if (_sim.State.IsOver)
		{
			var winner = _sim.State.Winner == SimSide.Left ? "LEFT WINS" : "RIGHT WINS";
			DrawString(font, new Vector2(20, 40), winner, fontSize: 32, modulate: Colors.White);
		}
	}

	private void DrawTierOverlay(Font font)
	{
		if (_sim == null || _cfg == null) return;

		int tier = _cfg.UnlockedTierForTime(_sim.State.Time);
		string timeText = $"t={_sim.State.Time:0}s  L_Tier={tier}  R_Tier={tier}";
		DrawString(font, new Vector2(10f, 18f), timeText, fontSize: 12, modulate: Colors.White);

		string tierText = $"L:{tier}  R:{tier}";
		var size = font.GetStringSize(tierText, fontSize: 12);
		float rightX = GetViewportRect().Size.X - size.X - 10f;
		DrawString(font, new Vector2(rightX, 18f), tierText, fontSize: 12, modulate: Colors.White);
	}

	private void UpdateUnitPositions()
	{
		_unitPositions.Clear();
		if (_sim == null) return;

		foreach (var u in _sim.State.Units)
		{
			if (!u.Alive) continue;
			float y = GroundY;
			float h = 12 + (u.Def.Tier - 1) * 4;
			float w = 10 + (u.Def.Tier - 1) * 4;
			if (u.Def.MovementClass == MovementClass.Air) y += AirAltitudeBase + GetAirJitter(u.Id);
			var baseCenter = GetUnitCenter(u, w, h, y);
			var center = baseCenter;

			if (u.Def.MovementClass == MovementClass.Air)
			{
				center.Y += GetAirBob(u.Id, _sim.State.Time);
				_unitMoving[u.Id] = true;
			}
			else
			{
				bool moving = IsMoving(u.Id, baseCenter, _lastDt);
				center.Y += GetGroundBob(u.Id, _sim.State.Time, moving);
				_unitMoving[u.Id] = moving;
			}

			_unitPositions[u.Id] = center;
			_prevUnitCenters[u.Id] = baseCenter;
			_lastKnownPositions[u.Id] = center;
			_lastKnownVisuals[u.Id] = new UnitVisual
			{
				Width = w,
				Height = h,
				Color = u.Side == SimSide.Left ? Colors.Cyan : Colors.Orange
			};
			_lastKnownDefs[u.Id] = u.Def;
			_lastKnownSides[u.Id] = u.Side;

			UpdateWalkPhase(u, _lastDt);
		}
	}

	private void HandleDeathEvents(IReadOnlyList<UnitDiedEvent> deathEvents)
	{
		if (deathEvents.Count == 0) return;

		for (int i = 0; i < deathEvents.Count; i++)
		{
			var ev = deathEvents[i];
			if (!_lastKnownPositions.TryGetValue(ev.UnitId, out var pos)) continue;
			if (!_lastKnownVisuals.TryGetValue(ev.UnitId, out var visual)) continue;
			if (!_lastKnownDefs.TryGetValue(ev.UnitId, out var def)) continue;
			if (!_lastKnownSides.TryGetValue(ev.UnitId, out var side)) continue;

			var feet = new Vector2(pos.X, pos.Y + visual.Height * 0.5f);
			var info = new UnitDeathInfo(ev.UnitId, pos, feet, visual, side, def.WeaponLength);
			var profile = GetProfile(def);
			profile.OnDeath(info);
		}
	}

	private void UpdateProfiles(float dt)
	{
		for (int i = 0; i < _profiles.Length; i++)
		{
			_profiles[i].Update(dt);
		}
	}

	private void DrawProfileOverlays()
	{
		for (int i = 0; i < _profiles.Length; i++)
		{
			_profiles[i].DrawOverlay(this);
		}
	}

	private IUnitAnimationProfile GetProfile(UnitDef def)
	{
		for (int i = 0; i < _profiles.Length; i++)
		{
			if (_profiles[i].Applies(def)) return _profiles[i];
		}

		return _profiles[^1];
	}

	private void UpdateWalkPhase(UnitState u, float dt)
	{
		float phase = _walkPhases.TryGetValue(u.Id, out var value) ? value : 0f;
		if (_unitMoving.TryGetValue(u.Id, out var moving) && moving)
		{
			float freq = Mathf.Clamp(6f + u.Def.Speed * 0.08f, 6f, 10f);
			phase += dt * freq;
		}
		_walkPhases[u.Id] = phase;
	}

	private static Vector2 GetUnitCenter(UnitState u, float w, float h, float y)
	{
		return new Vector2(u.X, y - h * 0.5f);
	}

	private static float GetAirJitter(int unitId)
	{
		int slot = unitId % 5;
		return (slot - 2) * AirAltitudeStep;
	}

	private static float GetAirBob(int unitId, float timeSeconds)
	{
		float phase = (unitId % 7) * 0.7f;
		return Mathf.Sin(timeSeconds * AirBobSpeed + phase) * AirBobAmplitude;
	}

	private bool IsMoving(int unitId, Vector2 baseCenter, float dt)
	{
		if (dt <= 0f) return false;
		if (!_prevUnitCenters.TryGetValue(unitId, out var prev)) return true;
		float speed = (baseCenter - prev).Length() / dt;
		return speed > MoveEpsilon;
	}

	private static float GetGroundBob(int unitId, float timeSeconds, bool moving)
	{
		float amplitude = moving ? GroundBobAmplitudeMoving : GroundBobAmplitudeIdle;
		float phase = (unitId % 17) * 0.37f;
		float omega = GroundBobFrequency * Mathf.Tau;
		float wave = Mathf.Sin(timeSeconds * omega + phase);
		return (wave + 1f) * 0.5f * amplitude;
	}

	private void ApplyDamageEvents(IReadOnlyList<DamageEvent> damageEvents)
	{
		for (int i = 0; i < damageEvents.Count; i++)
		{
			var ev = damageEvents[i];
			if (ev.Damage <= 0f) continue;

			_hitFlashTimers[ev.TargetId] = FlashDuration;
			_attackTimers[ev.AttackerId] = AttackSwingDuration;

			if (_damageBuckets.TryGetValue(ev.TargetId, out var bucket))
			{
				bucket.Damage += ev.Damage;
				_damageBuckets[ev.TargetId] = bucket;
			}
			else
			{
				_damageBuckets[ev.TargetId] = new DamageBucket { Damage = ev.Damage, Time = 0f };
			}

			if (ev.IsRanged &&
				_unitPositions.TryGetValue(ev.AttackerId, out var from) &&
				_unitPositions.TryGetValue(ev.TargetId, out var to))
			{
				_tracers.Add(new Tracer { Start = from, End = to, Time = 0f });
			}
		}
	}

	private void UpdateFlashTimers(float dt)
	{
		_scratchKeys.Clear();
		foreach (var kvp in _hitFlashTimers) _scratchKeys.Add(kvp.Key);
		for (int i = 0; i < _scratchKeys.Count; i++)
		{
			int key = _scratchKeys[i];
			float remaining = _hitFlashTimers[key] - dt;
			if (remaining <= 0f) _hitFlashTimers.Remove(key);
			else _hitFlashTimers[key] = remaining;
		}
	}

	private void UpdateAttackTimers(float dt)
	{
		_scratchKeys.Clear();
		foreach (var kvp in _attackTimers) _scratchKeys.Add(kvp.Key);
		for (int i = 0; i < _scratchKeys.Count; i++)
		{
			int key = _scratchKeys[i];
			float remaining = _attackTimers[key] - dt;
			if (remaining <= 0f) _attackTimers.Remove(key);
			else _attackTimers[key] = remaining;
		}
	}

	private float GetAttackPhase(int unitId)
	{
		if (!_attackTimers.TryGetValue(unitId, out float timer)) return 0f;
		float phase = 1f - Mathf.Clamp(timer / AttackSwingDuration, 0f, 1f);
		return phase;
	}

	private void UpdateDamageBuckets(float dt)
	{
		_scratchKeys.Clear();
		foreach (var kvp in _damageBuckets) _scratchKeys.Add(kvp.Key);

		for (int i = 0; i < _scratchKeys.Count; i++)
		{
			int key = _scratchKeys[i];
			var bucket = _damageBuckets[key];
			bucket.Time += dt;

			if (bucket.Time >= DamageAggregationWindow)
			{
				if (_unitPositions.TryGetValue(key, out var pos))
				{
					int value = (int)MathF.Round(bucket.Damage);
					if (value != 0)
					{
						var spawnPos = new Vector2(pos.X, pos.Y - 18f);
						_floatingNumbers.Add(new FloatingNumber { Position = spawnPos, Time = 0f, Value = value });
					}
				}
				_damageBuckets.Remove(key);
			}
			else
			{
				_damageBuckets[key] = bucket;
			}
		}
	}

	private void UpdateFloatingNumbers(float dt)
	{
		for (int i = _floatingNumbers.Count - 1; i >= 0; i--)
		{
			var num = _floatingNumbers[i];
			num.Time += dt;
			num.Position = new Vector2(num.Position.X, num.Position.Y - DamageNumberRiseSpeed * dt);

			if (num.Time >= DamageNumberLifetime)
			{
				_floatingNumbers.RemoveAt(i);
			}
			else
			{
				_floatingNumbers[i] = num;
			}
		}
	}

	private void UpdateTracers(float dt)
	{
		for (int i = _tracers.Count - 1; i >= 0; i--)
		{
			var tr = _tracers[i];
			tr.Time += dt;
			if (tr.Time >= TracerLifetime)
			{
				_tracers.RemoveAt(i);
			}
			else
			{
				_tracers[i] = tr;
			}
		}
	}

	private void DrawTracers()
	{
		for (int i = 0; i < _tracers.Count; i++)
		{
			var tr = _tracers[i];
			float t = Mathf.Clamp(1f - (tr.Time / TracerLifetime), 0f, 1f);
			var color = new Color(1f, 1f, 1f, t);
			DrawLine(tr.Start, tr.End, color, 2f);
		}
	}

	private void DrawDamageNumbers(Font font)
	{
		for (int i = 0; i < _floatingNumbers.Count; i++)
		{
			var num = _floatingNumbers[i];
			float t = Mathf.Clamp(1f - (num.Time / DamageNumberLifetime), 0f, 1f);
			var color = new Color(1f, 1f, 1f, t);
			string text = num.Value.ToString();
			var size = font.GetStringSize(text, fontSize: 12);
			var pos = new Vector2(num.Position.X - size.X * 0.5f, num.Position.Y);
			DrawString(font, pos, text, fontSize: 12, modulate: color);
		}
	}

	private struct DamageBucket
	{
		public float Damage;
		public float Time;
	}

	private struct FloatingNumber
	{
		public Vector2 Position;
		public float Time;
		public int Value;
	}

	private struct Tracer
	{
		public Vector2 Start;
		public Vector2 End;
		public float Time;
	}

public struct UnitVisual
	{
		public float Width;
		public float Height;
		public Color Color;
	}
}
