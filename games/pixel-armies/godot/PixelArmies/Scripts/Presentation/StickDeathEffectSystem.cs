#nullable enable

using System.Collections.Generic;
using Godot;
using SimSide = PixelArmies.SimCore.Side;

namespace PixelArmies.Presentation;

public sealed class StickDeathEffectSystem
{
	private const float FallDuration = 0.24f;
	private const float DeathDuration = 0.7f;
	private const float FragmentLifetime = 0.9f;
	private const float SparkLifetime = 0.5f;
	private const float FragmentSpeed = 95f;
	private const float SparkSpeed = 190f;

	private const float TorsoLen = 14f;
	private const float LegLen = 10f;
	private const float HeadRadius = 3f;
	private const float LineWidth = 2.4f;

	private readonly List<StickDeath> _deaths = new();
	private readonly List<Fragment> _fragments = new();
	private readonly List<Spark> _sparks = new();
	private readonly RandomNumberGenerator _rng = new();

	public StickDeathEffectSystem()
	{
		_rng.Randomize();
	}

	public void AddDeath(Vector2 feetPos, SimSide side, Color color)
	{
		_deaths.Add(new StickDeath
		{
			Feet = feetPos,
			Side = side,
			Color = color,
			Time = 0f,
		});

		SpawnFragments(feetPos, color);
		SpawnSparks(feetPos);
	}

	public void Update(float dt)
	{
		for (int i = _deaths.Count - 1; i >= 0; i--)
		{
			var d = _deaths[i];
			d.Time += dt;
			if (d.Time >= DeathDuration) _deaths.RemoveAt(i);
			else _deaths[i] = d;
		}

		for (int i = _fragments.Count - 1; i >= 0; i--)
		{
			var f = _fragments[i];
			f.Time += dt;
			f.Position += f.Velocity * dt;
			f.Velocity *= 0.92f;
			if (f.Time >= f.Lifetime) _fragments.RemoveAt(i);
			else _fragments[i] = f;
		}

		for (int i = _sparks.Count - 1; i >= 0; i--)
		{
			var s = _sparks[i];
			s.Time += dt;
			s.Position += s.Velocity * dt;
			s.Velocity *= 0.88f;
			if (s.Time >= s.Lifetime) _sparks.RemoveAt(i);
			else _sparks[i] = s;
		}
	}

	public void Draw(CanvasItem canvas)
	{
		DrawDeaths(canvas);
		DrawFragments(canvas);
		DrawSparks(canvas);
	}

	private void DrawDeaths(CanvasItem canvas)
	{
		for (int i = 0; i < _deaths.Count; i++)
		{
			var d = _deaths[i];
			float t = Mathf.Clamp(d.Time / DeathDuration, 0f, 1f);
			float fallT = Mathf.Clamp(d.Time / FallDuration, 0f, 1f);
			float eased = Smoothstep(fallT);
			float dir = d.Side == SimSide.Left ? 1f : -1f;
			float angle = Mathf.Lerp(0f, dir * 1.6f, eased);
			float alpha = 1f - t;

			var hip = d.Feet + new Vector2(0f, -LegLen);
			var torsoDir = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
			var torsoEnd = hip + torsoDir * TorsoLen;
			var headCenter = torsoEnd + torsoDir * HeadRadius;
			var legLeft = d.Feet + new Vector2(-3f, -2f);
			var legRight = d.Feet + new Vector2(3f, -2f);

			var color = new Color(d.Color, alpha);
			canvas.DrawLine(hip, torsoEnd, color, LineWidth);
			canvas.DrawCircle(headCenter, HeadRadius, color);
			canvas.DrawLine(d.Feet, legLeft, color, LineWidth);
			canvas.DrawLine(d.Feet, legRight, color, LineWidth);
		}
	}

	private void DrawFragments(CanvasItem canvas)
	{
		for (int i = 0; i < _fragments.Count; i++)
		{
			var f = _fragments[i];
			float t = Mathf.Clamp(1f - (f.Time / f.Lifetime), 0f, 1f);
			var color = new Color(f.Color, t);
			var end = f.Position + f.Direction * f.Length;
			canvas.DrawLine(f.Position, end, color, LineWidth);
		}
	}

	private void DrawSparks(CanvasItem canvas)
	{
		for (int i = 0; i < _sparks.Count; i++)
		{
			var s = _sparks[i];
			float t = Mathf.Clamp(1f - (s.Time / s.Lifetime), 0f, 1f);
			var color = new Color(0.95f, 0.2f, 0.2f, t);
			canvas.DrawCircle(s.Position, 2f, color);
		}
	}

	private void SpawnFragments(Vector2 origin, Color color)
	{
		int count = _rng.RandiRange(8, 12);
		for (int i = 0; i < count; i++)
		{
			float angle = _rng.RandfRange(0f, Mathf.Tau);
			var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			float speed = FragmentSpeed * _rng.RandfRange(0.7f, 1.2f);
			_fragments.Add(new Fragment
			{
				Position = origin + new Vector2(_rng.RandfRange(-8f, 8f), _rng.RandfRange(-22f, 4f)),
				Velocity = dir * speed,
				Direction = dir,
				Length = _rng.RandfRange(6f, 12f),
				Lifetime = _rng.RandfRange(0.45f, FragmentLifetime),
				Time = 0f,
				Color = color
			});
		}
	}

	private void SpawnSparks(Vector2 origin)
	{
		int count = _rng.RandiRange(10, 16);
		for (int i = 0; i < count; i++)
		{
			float angle = _rng.RandfRange(-Mathf.Pi * 0.7f, -Mathf.Pi * 0.3f);
			var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			float speed = SparkSpeed * _rng.RandfRange(0.7f, 1.3f);
			_sparks.Add(new Spark
			{
				Position = origin + new Vector2(_rng.RandfRange(-6f, 6f), _rng.RandfRange(-10f, 6f)),
				Velocity = dir * speed,
				Lifetime = _rng.RandfRange(0.3f, SparkLifetime),
				Time = 0f
			});
		}
	}

	private static float Smoothstep(float t)
	{
		t = Mathf.Clamp(t, 0f, 1f);
		return t * t * (3f - 2f * t);
	}

	private struct StickDeath
	{
		public Vector2 Feet;
		public SimSide Side;
		public Color Color;
		public float Time;
	}

	private struct Fragment
	{
		public Vector2 Position;
		public Vector2 Velocity;
		public Vector2 Direction;
		public float Length;
		public float Lifetime;
		public float Time;
		public Color Color;
	}

	private struct Spark
	{
		public Vector2 Position;
		public Vector2 Velocity;
		public float Lifetime;
		public float Time;
	}
}
