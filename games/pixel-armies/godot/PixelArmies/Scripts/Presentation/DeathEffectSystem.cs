#nullable enable

using System.Collections.Generic;
using Godot;
using PixelArmies.SimCore;

namespace PixelArmies.Presentation;

public sealed class DeathEffectSystem
{
	private const float MinParticleLifetime = 0.25f;
	private const float MaxParticleLifetime = 0.5f;
	private const float MinDeathDuration = 0.2f;
	private const float MaxDeathDuration = 0.35f;
	private const float ParticleSpeed = 90f;
	private const float ParticleDrag = 0.85f;

	private readonly List<Particle> _particles = new();
	private readonly Dictionary<int, DyingUnit> _dyingUnits = new();
	private readonly List<int> _scratchIds = new();
	private readonly RandomNumberGenerator _rng = new();

	public DeathEffectSystem()
	{
		_rng.Randomize();
	}

	public void Advance(
		float dt,
		IReadOnlyList<UnitDiedEvent> deathEvents,
		Dictionary<int, Vector2> lastKnownPositions,
		Dictionary<int, BattleView.UnitVisual> lastKnownVisuals)
	{
		if (deathEvents.Count == 0) return;

		for (int i = 0; i < deathEvents.Count; i++)
		{
			var ev = deathEvents[i];
			if (!lastKnownPositions.TryGetValue(ev.UnitId, out var pos)) continue;
			if (!lastKnownVisuals.TryGetValue(ev.UnitId, out var visual)) continue;

			float duration = _rng.RandfRange(MinDeathDuration, MaxDeathDuration);
			_dyingUnits[ev.UnitId] = new DyingUnit
			{
				Center = pos,
				Duration = duration,
				Time = 0f,
				Width = visual.Width,
				Height = visual.Height,
				Color = visual.Color
			};

			SpawnParticles(pos, visual);
		}
	}

	public void Update(float dt)
	{
		for (int i = _particles.Count - 1; i >= 0; i--)
		{
			var p = _particles[i];
			p.Time += dt;
			p.Position += p.Velocity * dt;
			p.Velocity *= Mathf.Pow(ParticleDrag, dt * 60f);

			if (p.Time >= p.Lifetime)
			{
				_particles.RemoveAt(i);
			}
			else
			{
				_particles[i] = p;
			}
		}

		_scratchIds.Clear();
		foreach (var kvp in _dyingUnits) _scratchIds.Add(kvp.Key);

		for (int i = 0; i < _scratchIds.Count; i++)
		{
			int id = _scratchIds[i];
			var unit = _dyingUnits[id];
			unit.Time += dt;
			if (unit.Time >= unit.Duration) _dyingUnits.Remove(id);
			else _dyingUnits[id] = unit;
		}
	}

	public void Draw(CanvasItem canvas)
	{
		for (int i = 0; i < _particles.Count; i++)
		{
			var p = _particles[i];
			float t = Mathf.Clamp(1f - (p.Time / p.Lifetime), 0f, 1f);
			var color = new Color(p.Color, t);
			canvas.DrawCircle(p.Position, p.Size, color);
		}

		foreach (var kvp in _dyingUnits)
		{
			var unit = kvp.Value;
			float t = Mathf.Clamp(1f - (unit.Time / unit.Duration), 0f, 1f);
			float scale = Mathf.Lerp(0.6f, 1f, t);
			var color = new Color(unit.Color, t);

			float w = unit.Width * scale;
			float h = unit.Height * scale;
			var rect = new Rect2(unit.Center.X - w * 0.5f, unit.Center.Y - h * 0.5f, w, h);
			canvas.DrawRect(rect, color);
		}
	}

	private void SpawnParticles(Vector2 pos, BattleView.UnitVisual visual)
	{
		int count = Mathf.Clamp((int)(visual.Width * 0.2f), 8, 16);

		for (int i = 0; i < count; i++)
		{
			float angle = _rng.RandfRange(0f, Mathf.Tau);
			var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			float speed = ParticleSpeed * _rng.RandfRange(0.6f, 1.2f);

			_particles.Add(new Particle
			{
				Position = pos,
				Velocity = dir * speed,
				Time = 0f,
				Lifetime = _rng.RandfRange(MinParticleLifetime, MaxParticleLifetime),
				Size = _rng.RandfRange(2f, 4f) * Mathf.Clamp(visual.Width / 14f, 0.8f, 1.6f),
				Color = visual.Color
			});
		}
	}

	private struct Particle
	{
		public Vector2 Position;
		public Vector2 Velocity;
		public float Time;
		public float Lifetime;
		public float Size;
		public Color Color;
	}

	private struct DyingUnit
	{
		public Vector2 Center;
		public float Duration;
		public float Time;
		public float Width;
		public float Height;
		public Color Color;
	}
}
