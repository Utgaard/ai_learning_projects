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
	private const float DefaultAttackDuration = 0.22f;
	private const float AirAltitudeBase = -40f;
	private const float AirAltitudeStep = 2f;
	private const float AirBobAmplitude = 6f;
	private const float AirBobSpeed = 3.2f;
	private const float GroundBobAmplitudeMoving = 2.2f;
	private const float GroundBobAmplitudeIdle = 0.2f;
	private const float GroundBobFrequency = 2.6f;
	private const float MoveEpsilon = 0.5f;
	private const float CleaveSweepDuration = 0.30f;
	private const float BaseFlashDuration = 0.25f;
	private const float HitParticleLifetime = 0.18f;
	private const int   HitParticleCount = 4;

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
	private readonly Dictionary<int, float> _attackDurations = new();
	private readonly Dictionary<int, DamageBucket> _damageBuckets = new();
	private readonly List<FloatingNumber> _floatingNumbers = new();
	private readonly List<Tracer> _tracers = new();
	private readonly List<CleaveSweep> _cleaveSweeps = new();
	private readonly List<HitParticle> _hitParticles = new();
	private readonly List<int> _scratchKeys = new();
	private readonly HitReactionSystem _hitReactions = new();
	private readonly IUnitAnimationProfile[] _profiles =
	{
		new StickTier1Profile(),
		new DefaultRectProfile()
	};
	private ArmyVisualProfile _leftVisualProfile = ArmyVisualProfiles.GetProfile("legion");
	private ArmyVisualProfile _rightVisualProfile = ArmyVisualProfiles.GetProfile("legion");

	// Base damage flash state
	private float _prevLeftBaseHp;
	private float _prevRightBaseHp;
	private float _leftBaseFlashTimer;
	private float _rightBaseFlashTimer;

	// Events raised for BattleGameHost to react to (shake, audio)
	public event Action? BaseDamagedLeft;
	public event Action? BaseDamagedRight;

	public float GroundY { get; private set; } = 120f;

	public void Configure(BattleSimulator sim, SimConfig cfg, float groundY)
	{
		_sim = sim;
		_cfg = cfg;
		GroundY = groundY;
		_prevLeftBaseHp  = cfg.BaseMaxHp;
		_prevRightBaseHp = cfg.BaseMaxHp;
		_leftVisualProfile  = ArmyVisualProfiles.GetProfile(sim.LeftArmy.VisualProfileId);
		_rightVisualProfile = ArmyVisualProfiles.GetProfile(sim.RightArmy.VisualProfileId);
	}

	public void Advance(float dt, IReadOnlyList<DamageEvent> damageEvents, IReadOnlyList<UnitDiedEvent> deathEvents)
	{
		if (_sim == null) return;
		_lastDt = dt;

		UpdateUnitPositions();
		CheckBaseDamage();

		if (damageEvents.Count > 0)
			ApplyDamageEvents(damageEvents);

		_hitReactions.Advance(dt, damageEvents, _unitPositions);
		HandleDeathEvents(deathEvents);

		UpdateFlashTimers(dt);
		UpdateAttackTimers(dt);
		UpdateDamageBuckets(dt);
		UpdateFloatingNumbers(dt);
		UpdateTracers(dt);
		UpdateCleaveSweeps(dt);
		UpdateHitParticles(dt);
		UpdateBaseFlash(dt);
		UpdateProfiles(dt);
	}

	public override void _Draw()
	{
		if (_sim == null || _cfg == null) return;

		var lColor = _leftVisualProfile.PrimaryColor;
		var rColor = _rightVisualProfile.PrimaryColor;

		// --- Bases ---
		DrawBase(side: SimSide.Left,  x: -30f,                      lColor, _leftBaseFlashTimer,  _sim.State.LeftBaseHp);
		DrawBase(side: SimSide.Right, x: _cfg.BattlefieldLength,     rColor, _rightBaseFlashTimer, _sim.State.RightBaseHp);

		// --- Base HP bars ---
		float barW = 120f, barH = 10f;
		float lPct = Mathf.Clamp(_sim.State.LeftBaseHp  / _cfg.BaseMaxHp, 0f, 1f);
		float rPct = Mathf.Clamp(_sim.State.RightBaseHp / _cfg.BaseMaxHp, 0f, 1f);

		Color lBarColor = lPct < 0.30f ? new Color(1f, 0.15f, 0.15f) : lColor;
		Color rBarColor = rPct < 0.30f ? new Color(1f, 0.15f, 0.15f) : rColor;

		DrawRect(new Rect2(10, GroundY + 20, barW, barH), Colors.Black);
		DrawRect(new Rect2(10, GroundY + 20, barW * lPct, barH), lBarColor);
		DrawRect(new Rect2(_cfg.BattlefieldLength - barW - 10, GroundY + 20, barW, barH), Colors.Black);
		DrawRect(new Rect2(_cfg.BattlefieldLength - barW - 10, GroundY + 20, barW * rPct, barH), rBarColor);

		var font = ThemeDB.FallbackFont;

		DrawTierOverlay(font);
		DrawTracers();
		DrawCleaveSweeps();

		// --- Units ---
		const int tierFontSize = 10;
		var drawContext = new UnitDrawContext(this, font, tierFontSize);

		foreach (var u in _sim.State.Units)
		{
			float y = GroundY;
			float h = 12 + (u.Def.Tier - 1) * 4;
			float w = 10 + (u.Def.Tier - 1) * 4;

			if (u.Def.MovementClass == MovementClass.Air)
				y += AirAltitudeBase + GetAirJitter(u.Id);

			var c = u.Side == SimSide.Left ? lColor : rColor;

			var center = _unitPositions.TryGetValue(u.Id, out var cached)
				? cached
				: GetUnitCenter(u, w, h, y);
			if (_hitReactions.TryGetRenderCenter(u.Id, out var renderCenter))
				center = renderCenter;

			bool moving = _unitMoving.TryGetValue(u.Id, out var mv) && mv;
			float phase = _walkPhases.TryGetValue(u.Id, out var ph) ? ph : 0f;
			float attackPhase = GetAttackPhase(u.Id);
			float flashAlpha = 0f;
			if (_hitFlashTimers.TryGetValue(u.Id, out float flash))
				flashAlpha = Mathf.Clamp(flash / FlashDuration, 0f, 1f);

			var armyProfile = u.Side == SimSide.Left ? _leftVisualProfile : _rightVisualProfile;
			var stickStyle = armyProfile.ResolveStickProfile(u.Def.Id);
			var profile = GetProfile(u.Def, armyProfile);
			var data = new UnitDrawData(
				center, w, h, c, u.Side, moving, phase, attackPhase,
				u.Def.WeaponLength, flashAlpha, stickStyle);
			profile.DrawUnit(drawContext, u, data);

			// HP bar for T3+ units
			if (u.Def.Tier >= 3)
				DrawUnitHpBar(u, center, w, c);
		}

		DrawHitParticles();
		DrawDamageNumbers(font);
		DrawProfileOverlays();

		if (_sim.State.IsOver)
		{
			var winner = _sim.State.Winner == SimSide.Left ? "LEFT WINS" : "RIGHT WINS";
			DrawString(font, new Vector2(20, 40), winner, fontSize: 32, modulate: Colors.White);
		}
	}

	private void DrawBase(SimSide side, float x, Color armyColor, float flashTimer, float hp)
	{
		float flashAlpha = Mathf.Clamp(flashTimer / BaseFlashDuration, 0f, 1f);
		Color baseColor = flashAlpha > 0f
			? armyColor.Lerp(new Color(1f, 0.15f, 0.15f), flashAlpha)
			: armyColor.Lerp(Colors.White, 0.4f);
		DrawRect(new Rect2(x, GroundY - 60, 30, 60), baseColor);
	}

	private void DrawUnitHpBar(UnitState u, Vector2 center, float w, Color color)
	{
		const float BarH = 3f;
		const float BarPadding = 6f;
		float barW = w + 6f;
		float pct = Mathf.Clamp(u.Hp / u.Def.MaxHp, 0f, 1f);
		float left = center.X - barW * 0.5f;
		float top = center.Y - (12f + (u.Def.Tier - 1) * 4f) * 0.5f - BarPadding - BarH;

		DrawRect(new Rect2(left, top, barW, BarH), new Color(0f, 0f, 0f, 0.7f));
		if (pct > 0f)
		{
			Color barColor = pct < 0.30f ? new Color(1f, 0.15f, 0.15f) : color;
			DrawRect(new Rect2(left, top, barW * pct, BarH), barColor);
		}
	}

	private void DrawTierOverlay(Font font)
	{
		if (_sim == null || _cfg == null) return;

		int tier = _cfg.UnlockedTierForTime(_sim.State.Time);
		string timeText = $"t={_sim.State.Time:0}s  Tier={tier}";
		DrawString(font, new Vector2(10f, 18f), timeText, fontSize: 12, modulate: Colors.White);
	}

	private void CheckBaseDamage()
	{
		if (_sim == null) return;
		if (_sim.State.LeftBaseHp  < _prevLeftBaseHp)  { _leftBaseFlashTimer  = BaseFlashDuration; BaseDamagedLeft?.Invoke(); }
		if (_sim.State.RightBaseHp < _prevRightBaseHp) { _rightBaseFlashTimer = BaseFlashDuration; BaseDamagedRight?.Invoke(); }
		_prevLeftBaseHp  = _sim.State.LeftBaseHp;
		_prevRightBaseHp = _sim.State.RightBaseHp;
	}

	private void UpdateBaseFlash(float dt)
	{
		_leftBaseFlashTimer  = Math.Max(0f, _leftBaseFlashTimer  - dt);
		_rightBaseFlashTimer = Math.Max(0f, _rightBaseFlashTimer - dt);
	}

	private void UpdateUnitPositions()
	{
		_unitPositions.Clear();
		if (_sim == null) return;

		var lColor = _leftVisualProfile.PrimaryColor;
		var rColor = _rightVisualProfile.PrimaryColor;

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
				Width  = w,
				Height = h,
				Color  = u.Side == SimSide.Left ? lColor : rColor
			};
			_lastKnownDefs[u.Id]  = u.Def;
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
			var armyProfile = side == SimSide.Left ? _leftVisualProfile : _rightVisualProfile;
			var info = new UnitDeathInfo(ev.UnitId, pos, feet, visual, side, def.WeaponLength, armyProfile.StickDeathProfile);
			var profile = GetProfile(def, armyProfile);
			profile.OnDeath(info);
		}
	}

	private void UpdateProfiles(float dt)
	{
		for (int i = 0; i < _profiles.Length; i++)
			_profiles[i].Update(dt);
	}

	private void DrawProfileOverlays()
	{
		for (int i = 0; i < _profiles.Length; i++)
			_profiles[i].DrawOverlay(this);
	}

	private IUnitAnimationProfile GetProfile(UnitDef def, ArmyVisualProfile armyProfile)
	{
		if (armyProfile.UseStickFor(def)) return _profiles[0];
		return _profiles[1];
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
		=> new(u.X, y - h * 0.5f);

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
			float duration = _lastKnownDefs.TryGetValue(ev.AttackerId, out var def) && def.AttackDuration > 0f
				? def.AttackDuration
				: DefaultAttackDuration;
			_attackTimers[ev.AttackerId] = duration;
			_attackDurations[ev.AttackerId] = duration;

			// Aggregate damage for floating numbers (track IsAoe for coloring)
			if (_damageBuckets.TryGetValue(ev.TargetId, out var bucket))
			{
				bucket.Damage += ev.Damage;
				if (ev.IsAoe) bucket.IsAoe = true;
				if (_lastKnownSides.TryGetValue(ev.AttackerId, out var attSide))
					bucket.AttackerSide = attSide;
				_damageBuckets[ev.TargetId] = bucket;
			}
			else
			{
				bool attSideKnown = _lastKnownSides.TryGetValue(ev.AttackerId, out var attSide2);
				_damageBuckets[ev.TargetId] = new DamageBucket
				{
					Damage = ev.Damage,
					Time = 0f,
					IsAoe = ev.IsAoe,
					AttackerSide = attSideKnown ? attSide2 : SimSide.Left,
				};
			}

			// Ranged tracer
			if (ev.IsRanged &&
				_unitPositions.TryGetValue(ev.AttackerId, out var from) &&
				_unitPositions.TryGetValue(ev.TargetId, out var to))
			{
				_tracers.Add(new Tracer { Start = from, End = to, Time = 0f });
			}

			// Cleave sweep arc
			if (ev.IsAoe &&
				_unitPositions.TryGetValue(ev.AttackerId, out var attackerPos))
			{
				var side = _lastKnownSides.TryGetValue(ev.AttackerId, out var s) ? s : SimSide.Left;
				var color = side == SimSide.Left ? _leftVisualProfile.PrimaryColor : _rightVisualProfile.PrimaryColor;
				float facing = side == SimSide.Left ? 0f : Mathf.Pi;  // facing direction

				bool found = false;
				for (int j = 0; j < _cleaveSweeps.Count; j++)
				{
					if (_cleaveSweeps[j].AttackerId == ev.AttackerId) { found = true; break; }
				}
				if (!found)
				{
					_cleaveSweeps.Add(new CleaveSweep
					{
						AttackerId = ev.AttackerId,
						Center = attackerPos,
						Radius = _lastKnownDefs.TryGetValue(ev.AttackerId, out var aDef) ? aDef.AbilityParam : 40f,
						Facing = facing,
						Time = 0f,
						Color = color,
					});
				}
			}

			// Hit impact sparks (melee only)
			if (!ev.IsRanged && !ev.IsAoe &&
				_unitPositions.TryGetValue(ev.TargetId, out var hitPos) &&
				_unitPositions.TryGetValue(ev.AttackerId, out var srcPos))
			{
				SpawnHitParticles(hitPos, srcPos);
			}
		}
	}

	private void SpawnHitParticles(Vector2 hitPos, Vector2 srcPos)
	{
		var dir = (hitPos - srcPos).Normalized();
		var rng = new Random(hitPos.GetHashCode());

		for (int p = 0; p < HitParticleCount; p++)
		{
			float angle = (float)(rng.NextDouble() * Math.PI - Math.PI * 0.5f);
			float speed = 60f + (float)rng.NextDouble() * 60f;
			float cosA = Mathf.Cos(angle), sinA = Mathf.Sin(angle);
			var vel = new Vector2(
				dir.X * cosA - dir.Y * sinA,
				dir.X * sinA + dir.Y * cosA
			) * speed;
			_hitParticles.Add(new HitParticle { Position = hitPos, Velocity = vel, Time = 0f });
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
			if (remaining <= 0f)
			{
				_attackTimers.Remove(key);
				_attackDurations.Remove(key);
			}
			else _attackTimers[key] = remaining;
		}
	}

	private float GetAttackPhase(int unitId)
	{
		if (!_attackTimers.TryGetValue(unitId, out float timer)) return 0f;
		float duration = _attackDurations.TryGetValue(unitId, out var d) && d > 0f ? d : DefaultAttackDuration;
		return 1f - Mathf.Clamp(timer / duration, 0f, 1f);
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
						Color numColor = Colors.White;
						int fontSize = 12;

						if (bucket.IsAoe)
						{
							// Cleave/AoE: attacker army color
							numColor = bucket.AttackerSide == SimSide.Left
								? _leftVisualProfile.PrimaryColor
								: _rightVisualProfile.PrimaryColor;
							numColor = numColor.Lightened(0.3f);
							fontSize = 13;
						}
						else if (value >= 20)
						{
							// Large single hit: gold
							numColor = new Color(1f, 0.85f, 0.2f);
							fontSize = 15;
						}

						_floatingNumbers.Add(new FloatingNumber
						{
							Position = spawnPos,
							Time = 0f,
							Value = value,
							Color = numColor,
							FontSize = fontSize,
						});
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

			if (num.Time >= DamageNumberLifetime) _floatingNumbers.RemoveAt(i);
			else _floatingNumbers[i] = num;
		}
	}

	private void UpdateTracers(float dt)
	{
		for (int i = _tracers.Count - 1; i >= 0; i--)
		{
			var tr = _tracers[i];
			tr.Time += dt;
			if (tr.Time >= TracerLifetime) _tracers.RemoveAt(i);
			else _tracers[i] = tr;
		}
	}

	private void UpdateCleaveSweeps(float dt)
	{
		for (int i = _cleaveSweeps.Count - 1; i >= 0; i--)
		{
			var cs = _cleaveSweeps[i];
			cs.Time += dt;
			if (cs.Time >= CleaveSweepDuration) _cleaveSweeps.RemoveAt(i);
			else _cleaveSweeps[i] = cs;
		}
	}

	private void UpdateHitParticles(float dt)
	{
		for (int i = _hitParticles.Count - 1; i >= 0; i--)
		{
			var p = _hitParticles[i];
			p.Time += dt;
			p.Position += p.Velocity * dt;
			p.Velocity *= Mathf.Exp(-5f * dt);  // drag

			if (p.Time >= HitParticleLifetime) _hitParticles.RemoveAt(i);
			else _hitParticles[i] = p;
		}
	}

	private void DrawTracers()
	{
		for (int i = 0; i < _tracers.Count; i++)
		{
			var tr = _tracers[i];
			float t = Mathf.Clamp(1f - (tr.Time / TracerLifetime), 0f, 1f);
			DrawLine(tr.Start, tr.End, new Color(1f, 1f, 1f, t), 2f);
		}
	}

	private void DrawCleaveSweeps()
	{
		for (int i = 0; i < _cleaveSweeps.Count; i++)
		{
			var cs = _cleaveSweeps[i];
			float t = 1f - Mathf.Clamp(cs.Time / CleaveSweepDuration, 0f, 1f);
			const float HalfSpread = Mathf.Pi * 0.45f;  // ±81°
			float from = cs.Facing - HalfSpread;
			float to   = cs.Facing + HalfSpread;
			var color = new Color(cs.Color.R, cs.Color.G, cs.Color.B, t * 0.55f);
			DrawArc(cs.Center, cs.Radius, from, to, 20, color, cs.Radius * 0.35f);
		}
	}

	private void DrawHitParticles()
	{
		for (int i = 0; i < _hitParticles.Count; i++)
		{
			var p = _hitParticles[i];
			float t = Mathf.Clamp(1f - (p.Time / HitParticleLifetime), 0f, 1f);
			var end = p.Position + p.Velocity.Normalized() * 4f;
			DrawLine(p.Position, end, new Color(1f, 0.9f, 0.5f, t), 1.5f);
		}
	}

	private void DrawDamageNumbers(Font font)
	{
		for (int i = 0; i < _floatingNumbers.Count; i++)
		{
			var num = _floatingNumbers[i];
			float t = Mathf.Clamp(1f - (num.Time / DamageNumberLifetime), 0f, 1f);
			var color = new Color(num.Color.R, num.Color.G, num.Color.B, t);
			string text = num.Value.ToString();
			var size = font.GetStringSize(text, fontSize: num.FontSize);
			var pos = new Vector2(num.Position.X - size.X * 0.5f, num.Position.Y);
			DrawString(font, pos, text, fontSize: num.FontSize, modulate: color);
		}
	}

	// --- Structs ---

	private struct DamageBucket
	{
		public float Damage;
		public float Time;
		public bool IsAoe;
		public SimSide AttackerSide;
	}

	private struct FloatingNumber
	{
		public Vector2 Position;
		public float Time;
		public int Value;
		public Color Color;
		public int FontSize;
	}

	private struct Tracer
	{
		public Vector2 Start;
		public Vector2 End;
		public float Time;
	}

	private struct CleaveSweep
	{
		public int AttackerId;
		public Vector2 Center;
		public float Radius;
		public float Facing;
		public float Time;
		public Color Color;
	}

	private struct HitParticle
	{
		public Vector2 Position;
		public Vector2 Velocity;
		public float Time;
	}

	public struct UnitVisual
	{
		public float Width;
		public float Height;
		public Color Color;
	}
}
