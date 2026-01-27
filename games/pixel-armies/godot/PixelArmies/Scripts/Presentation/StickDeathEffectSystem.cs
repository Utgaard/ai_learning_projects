#nullable enable

using System.Collections.Generic;
using Godot;
using SimSide = PixelArmies.SimCore.Side;

namespace PixelArmies.Presentation;

public sealed class StickDeathEffectSystem
{
	private readonly List<StickDeath> _deaths = new();
	private readonly List<Fragment> _fragments = new();
	private readonly List<Spark> _sparks = new();
	private readonly RandomNumberGenerator _rng = new();

	public StickDeathEffectSystem()
	{
		_rng.Randomize();
	}

	public void AddDeath(Vector2 feetPos, SimSide side, Color color, StickDeathProfile profile)
	{
		_deaths.Add(new StickDeath
		{
			Feet = feetPos,
			Side = side,
			Color = color,
			Time = 0f,
			Profile = profile,
		});

		SpawnFragments(feetPos, color, profile);
		SpawnSparks(feetPos, profile);
	}

	public void Update(float dt)
	{
		for (int i = _deaths.Count - 1; i >= 0; i--)
		{
			var d = _deaths[i];
			d.Time += dt;
			if (d.Time >= d.Profile.DeathDuration) _deaths.RemoveAt(i);
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
			var profile = d.Profile;
			float t = Mathf.Clamp(d.Time / profile.DeathDuration, 0f, 1f);
			float fallT = Mathf.Clamp(d.Time / profile.FallDuration, 0f, 1f);
			float eased = Smoothstep(fallT);
			float dir = d.Side == SimSide.Left ? 1f : -1f;
			float angle = Mathf.Lerp(0f, dir * profile.FallAngle, eased);
			float alpha = 1f - t;

			var hip = d.Feet + new Vector2(0f, -profile.LegLen);
			var torsoDir = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
			var torsoEnd = hip + torsoDir * profile.TorsoLen;
			var headCenter = torsoEnd + torsoDir * profile.HeadRadius;
			var legLeft = d.Feet + new Vector2(-3f, -2f);
			var legRight = d.Feet + new Vector2(3f, -2f);

			var color = new Color(d.Color, alpha);
			canvas.DrawLine(hip, torsoEnd, color, profile.LineWidth);
			canvas.DrawCircle(headCenter, profile.HeadRadius, color);
			canvas.DrawLine(d.Feet, legLeft, color, profile.LineWidth);
			canvas.DrawLine(d.Feet, legRight, color, profile.LineWidth);
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
			canvas.DrawLine(f.Position, end, color, f.LineWidth);
		}
	}

	private void DrawSparks(CanvasItem canvas)
	{
		for (int i = 0; i < _sparks.Count; i++)
		{
			var s = _sparks[i];
			float t = Mathf.Clamp(1f - (s.Time / s.Lifetime), 0f, 1f);
			var color = new Color(s.Color, t);
			canvas.DrawCircle(s.Position, 2f, color);
		}
	}

	private void SpawnFragments(Vector2 origin, Color color, StickDeathProfile profile)
	{
		int count = _rng.RandiRange(profile.FragmentCountMin, profile.FragmentCountMax);
		for (int i = 0; i < count; i++)
		{
			float angle = _rng.RandfRange(0f, Mathf.Tau);
			var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			float speed = profile.FragmentSpeed * _rng.RandfRange(0.7f, 1.2f);
			_fragments.Add(new Fragment
			{
				Position = origin + new Vector2(_rng.RandfRange(-8f, 8f), _rng.RandfRange(-22f, 4f)),
				Velocity = dir * speed,
				Direction = dir,
				Length = _rng.RandfRange(profile.FragmentLengthMin, profile.FragmentLengthMax),
				Lifetime = _rng.RandfRange(profile.FragmentLifetimeMin, profile.FragmentLifetimeMax),
				Time = 0f,
				Color = color,
				LineWidth = profile.LineWidth
			});
		}
	}

	private void SpawnSparks(Vector2 origin, StickDeathProfile profile)
	{
		int count = _rng.RandiRange(profile.SparkCountMin, profile.SparkCountMax);
		for (int i = 0; i < count; i++)
		{
			float angle = _rng.RandfRange(-Mathf.Pi * 0.7f, -Mathf.Pi * 0.3f);
			var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			float speed = profile.SparkSpeed * _rng.RandfRange(0.7f, 1.3f);
			_sparks.Add(new Spark
			{
				Position = origin + new Vector2(_rng.RandfRange(-6f, 6f), _rng.RandfRange(-10f, 6f)),
				Velocity = dir * speed,
				Lifetime = _rng.RandfRange(profile.SparkLifetimeMin, profile.SparkLifetimeMax),
				Time = 0f,
				Color = profile.SparkColor
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
		public StickDeathProfile Profile;
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
		public float LineWidth;
	}

	private struct Spark
	{
		public Vector2 Position;
		public Vector2 Velocity;
		public float Lifetime;
		public float Time;
		public Color Color;
	}
}
