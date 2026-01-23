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

	private BattleSimulator? _sim;
	private SimConfig? _cfg;

	private readonly Dictionary<int, Vector2> _unitPositions = new();
	private readonly Dictionary<int, Vector2> _lastKnownPositions = new();
	private readonly Dictionary<int, UnitVisual> _lastKnownVisuals = new();
	private readonly Dictionary<int, float> _hitFlashTimers = new();
	private readonly Dictionary<int, DamageBucket> _damageBuckets = new();
	private readonly List<FloatingNumber> _floatingNumbers = new();
	private readonly List<Tracer> _tracers = new();
	private readonly List<int> _scratchKeys = new();
	private readonly HitReactionSystem _hitReactions = new();
	private readonly DeathEffectSystem _deathEffects = new();

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

		UpdateUnitPositions();

		if (damageEvents.Count > 0)
		{
			ApplyDamageEvents(damageEvents);
		}

		_hitReactions.Advance(dt, damageEvents, _unitPositions);
		_deathEffects.Advance(dt, deathEvents, _lastKnownPositions, _lastKnownVisuals);

		UpdateFlashTimers(dt);
		UpdateDamageBuckets(dt);
		UpdateFloatingNumbers(dt);
		UpdateTracers(dt);
		_deathEffects.Update(dt);
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
		_deathEffects.Draw(this);

		// Units as rectangles
		const int tierFontSize = 10;

		foreach (var u in _sim.State.Units)
		{
			float y = GroundY - 12;
			float h = 12;
			float w = 10;

			// Make big units bigger (tier proxy)
			w += (u.Def.Tier - 1) * 4;
			h += (u.Def.Tier - 1) * 4;

			if (u.Def.MovementClass == MovementClass.Air)
				y -= 40f; // air height

			// Left units slightly different than right
			var c = u.Side == SimSide.Left ? Colors.Cyan : Colors.Orange;

			var center = GetUnitCenter(u, w, h, y);
			if (_hitReactions.TryGetRenderCenter(u.Id, out var renderCenter))
			{
				center = renderCenter;
			}

			var rect = new Rect2(center.X - w * 0.5f, center.Y - h * 0.5f, w, h);
			float outline = 1f + (u.Def.Tier - 1) * 1.2f;

			DrawRect(rect, c);
			DrawRect(rect, Colors.Black, false, outline);

			if (_hitFlashTimers.TryGetValue(u.Id, out float flash))
			{
				float t = Mathf.Clamp(flash / FlashDuration, 0f, 1f);
				var flashColor = new Color(1f, 1f, 1f, t);
				DrawRect(rect, flashColor);
			}

			string label = u.Def.Tier.ToString();
			var labelSize = font.GetStringSize(label, fontSize: tierFontSize);
			var labelPos = new Vector2(center.X - labelSize.X * 0.5f, center.Y - h * 0.5f - 4f);
			DrawString(font, labelPos, label, fontSize: tierFontSize, modulate: c);
		}

		DrawDamageNumbers(font);

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
			float y = GroundY - 12;
			float h = 12 + (u.Def.Tier - 1) * 4;
			float w = 10 + (u.Def.Tier - 1) * 4;
			if (u.Def.MovementClass == MovementClass.Air) y -= 40f;
			var center = GetUnitCenter(u, w, h, y);
			_unitPositions[u.Id] = center;
			_lastKnownPositions[u.Id] = center;
			_lastKnownVisuals[u.Id] = new UnitVisual
			{
				Width = w,
				Height = h,
				Color = u.Side == SimSide.Left ? Colors.Cyan : Colors.Orange
			};
		}
	}

	private static Vector2 GetUnitCenter(UnitState u, float w, float h, float y)
	{
		return new Vector2(u.X, y - h * 0.5f);
	}

	private void ApplyDamageEvents(IReadOnlyList<DamageEvent> damageEvents)
	{
		for (int i = 0; i < damageEvents.Count; i++)
		{
			var ev = damageEvents[i];
			if (ev.Damage <= 0f) continue;

			_hitFlashTimers[ev.TargetId] = FlashDuration;

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
