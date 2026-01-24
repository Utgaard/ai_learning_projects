#nullable enable

using System.Collections.Generic;
using Godot;

namespace PixelArmies.Presentation;

public partial class OrbLayer : Control
{
	private const float Gravity = 2500f;
	private const float Restitution = 0.45f;
	private const float MinBounceSpeed = 120f;
	private const float SettleDuration = 0.18f;
	private const float FadeDuration = 0.12f;
	private const float OrbRadius = 5f;

	private readonly List<Orb> _orbs = new();
	private readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (_orbs.Count == 0) return;

		for (int i = _orbs.Count - 1; i >= 0; i--)
		{
			var orb = _orbs[i];
			UpdateOrb(ref orb, dt);
			if (orb.State == OrbState.Dead)
			{
				_orbs.RemoveAt(i);
			}
			else
			{
				_orbs[i] = orb;
			}
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		for (int i = 0; i < _orbs.Count; i++)
		{
			var orb = _orbs[i];
			float alpha = orb.Alpha;
			if (alpha <= 0f) continue;
			var color = new Color(1f, 1f, 1f, alpha);
			DrawCircle(orb.Position, OrbRadius * orb.Scale, color);
		}
	}

	public void SpawnOrb(Vector2 start, Vector2 target)
	{
		var orb = new Orb
		{
			Position = start,
			Target = target,
			Velocity = new Vector2(_rng.RandfRange(-40f, 40f), 0f),
			BouncesRemaining = _rng.RandiRange(1, 2),
			State = OrbState.Falling,
			Alpha = 1f,
			Scale = 1f,
		};
		_orbs.Add(orb);
	}

	private static void UpdateOrb(ref Orb orb, float dt)
	{
		if (orb.State == OrbState.Falling)
		{
			orb.Velocity = new Vector2(orb.Velocity.X, orb.Velocity.Y + Gravity * dt);
			orb.Position += orb.Velocity * dt;

			if (orb.Position.Y >= orb.Target.Y)
			{
				orb.Position = new Vector2(orb.Position.X, orb.Target.Y);
				orb.Velocity = new Vector2(orb.Velocity.X * 0.7f, -orb.Velocity.Y * Restitution);
				orb.BouncesRemaining--;

				if (orb.BouncesRemaining <= 0 || Mathf.Abs(orb.Velocity.Y) < MinBounceSpeed)
				{
					orb.State = OrbState.Settling;
					orb.SettleTime = 0f;
				}
			}
		}
		else if (orb.State == OrbState.Settling)
		{
			orb.SettleTime += dt;
			float t = Mathf.Clamp(orb.SettleTime / SettleDuration, 0f, 1f);
			orb.Position = orb.Position.Lerp(orb.Target, t);
			if (t >= 1f)
			{
				orb.State = OrbState.Fading;
				orb.FadeTime = 0f;
			}
		}
		else if (orb.State == OrbState.Fading)
		{
			orb.FadeTime += dt;
			float t = Mathf.Clamp(orb.FadeTime / FadeDuration, 0f, 1f);
			orb.Alpha = 1f - t;
			orb.Scale = 1f - t * 0.4f;
			if (t >= 1f)
			{
				orb.State = OrbState.Dead;
			}
		}
	}

	private enum OrbState
	{
		Falling,
		Settling,
		Fading,
		Dead
	}

	private struct Orb
	{
		public Vector2 Position;
		public Vector2 Velocity;
		public Vector2 Target;
		public int BouncesRemaining;
		public float SettleTime;
		public float FadeTime;
		public float Alpha;
		public float Scale;
		public OrbState State;
	}
}
