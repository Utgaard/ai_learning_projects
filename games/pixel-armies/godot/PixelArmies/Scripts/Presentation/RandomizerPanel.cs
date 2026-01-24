#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using PixelArmies.GameHost;
using PixelArmies.SimCore;
using SimSide = PixelArmies.SimCore.Side;

namespace PixelArmies.Presentation;

public partial class RandomizerPanel : PanelContainer
{
	private const int TierCount = 4;
	private static readonly Color LockedColor = new(1f, 1f, 1f, 0.35f);
	private static readonly Color UnlockedColor = new(1f, 1f, 1f, 1f);
	private const float FillLerpSpeed = 8f;
	private const float SpawnPulseDuration = 0.25f;
	private const float UnlockPulseDuration = 0.4f;
	private const float PulseScale = 0.08f;

	public bool IsRightSide { get; set; }

	private Label? _titleLabel;
	private Label? _tierUnlockedLabel;
	private readonly BucketRow[] _bucketRows = new BucketRow[TierCount];
	private readonly float[] _targetFill = new float[TierCount];
	private readonly float[] _displayFill = new float[TierCount];
	private readonly float[] _spawnPulseTimer = new float[TierCount];
	private readonly float[] _unlockPulseTimer = new float[TierCount];
	private int _lastUnlockedTier;
	private OrbLayer? _orbLayer;
	private readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		SetProcess(true);

		var root = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};
		root.AddThemeConstantOverride("separation", 6);

		_titleLabel = new Label
		{
			Text = IsRightSide ? "RIGHT RANDOMIZER" : "LEFT RANDOMIZER",
			HorizontalAlignment = IsRightSide ? HorizontalAlignment.Right : HorizontalAlignment.Left,
		};
		_titleLabel.AddThemeFontSizeOverride("font_size", 18);

		_tierUnlockedLabel = new Label
		{
			Text = "Tier Unlocked: 1",
			HorizontalAlignment = IsRightSide ? HorizontalAlignment.Right : HorizontalAlignment.Left,
		};

		root.AddChild(_titleLabel);
		root.AddChild(_tierUnlockedLabel);

		for (int i = 0; i < TierCount; i++)
		{
			int tier = i + 1;
			var row = new HBoxContainer
			{
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			};
			row.AddThemeConstantOverride("separation", 6);

			var label = new Label
			{
				Text = $"T{tier}",
				HorizontalAlignment = IsRightSide ? HorizontalAlignment.Right : HorizontalAlignment.Left,
				CustomMinimumSize = new Vector2(32f, 0f),
			};

			var bar = new ProgressBar
			{
				MinValue = 0f,
				MaxValue = 1f,
				Value = 0f,
				ShowPercentage = false,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				CustomMinimumSize = new Vector2(0f, 16f),
			};

			row.AddChild(label);
			row.AddChild(bar);
			root.AddChild(row);

			_bucketRows[i] = new BucketRow
			{
				RowRoot = row,
				Label = label,
				Bar = bar,
				BaseModulate = UnlockedColor,
			};
		}

		AddChild(root);

		_orbLayer = new OrbLayer
		{
			Name = "OrbLayer",
			AnchorLeft = 0f,
			AnchorTop = 0f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 0f,
			OffsetTop = 0f,
			OffsetRight = 0f,
			OffsetBottom = 0f,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 5,
			ZAsRelative = true,
		};
		AddChild(_orbLayer);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		for (int i = 0; i < _bucketRows.Length; i++)
		{
			_displayFill[i] = SmoothFill(_displayFill[i], _targetFill[i], dt);
			_bucketRows[i].Bar.Value = _displayFill[i];

			float pulse = 0f;
			if (_spawnPulseTimer[i] > 0f)
			{
				_spawnPulseTimer[i] = MathF.Max(0f, _spawnPulseTimer[i] - dt);
				pulse = MathF.Max(pulse, PulseCurve(_spawnPulseTimer[i], SpawnPulseDuration));
			}
			if (_unlockPulseTimer[i] > 0f)
			{
				_unlockPulseTimer[i] = MathF.Max(0f, _unlockPulseTimer[i] - dt);
				pulse = MathF.Max(pulse, PulseCurve(_unlockPulseTimer[i], UnlockPulseDuration));
			}

			var row = _bucketRows[i];
			row.RowRoot.Modulate = ApplyPulse(row.BaseModulate, pulse);
			row.Bar.Scale = Vector2.One + new Vector2(PulseScale * pulse, PulseScale * pulse);
		}
	}

	public void ApplySnapshot(HudSideSnapshot snapshot)
	{
		if (_tierUnlockedLabel != null)
		{
			_tierUnlockedLabel.Text = $"Tier Unlocked: {snapshot.UnlockedTier}";
		}

		if (snapshot.UnlockedTier < _lastUnlockedTier)
		{
			_lastUnlockedTier = snapshot.UnlockedTier;
		}
		if (snapshot.UnlockedTier > _lastUnlockedTier)
		{
			for (int tier = _lastUnlockedTier + 1; tier <= snapshot.UnlockedTier && tier <= TierCount; tier++)
			{
				_unlockPulseTimer[tier - 1] = UnlockPulseDuration;
			}
			_lastUnlockedTier = snapshot.UnlockedTier;
		}

		for (int i = 0; i < _bucketRows.Length; i++)
		{
			int tier = i + 1;
			var row = _bucketRows[i];
			bool unlocked = tier <= snapshot.UnlockedTier;
			row.BaseModulate = unlocked ? UnlockedColor : LockedColor;
			row.RowRoot.Modulate = row.BaseModulate;

			float fill = 0f;
			if (tier == snapshot.CurrentBucketTier && snapshot.CurrentTargetCost > 0f)
			{
				fill = Mathf.Clamp(snapshot.CurrentBucketProgress / snapshot.CurrentTargetCost, 0f, 1f);
			}
			_targetFill[i] = fill;
		}
	}

	public void OnPowerAllocatedEvents(IReadOnlyList<PowerAllocatedEvent> powerEvents)
	{
		var side = IsRightSide ? SimSide.Right : SimSide.Left;
		if (_orbLayer == null) return;

		for (int i = 0; i < powerEvents.Count; i++)
		{
			var ev = powerEvents[i];
			if (ev.Side != side) continue;
			if (ev.Tier < 1 || ev.Tier > TierCount) continue;

			Vector2 target = GetBucketTargetPosition(ev.Tier);
			if (target == Vector2.Zero) continue;

			float width = Size.X > 0f ? Size.X : 200f;
			float jitter = _rng.RandfRange(0.15f, 0.85f);
			float startX = width * jitter;
			_orbLayer.SpawnOrb(new Vector2(startX, 0f), target);
		}
	}

	public void OnUnitSpawnedEvents(IReadOnlyList<UnitSpawnedEvent> spawnEvents)
	{
		var side = IsRightSide ? SimSide.Right : SimSide.Left;
		for (int i = 0; i < spawnEvents.Count; i++)
		{
			var ev = spawnEvents[i];
			if (ev.Side != side) continue;
			if (ev.Tier < 1 || ev.Tier > TierCount) continue;
			_spawnPulseTimer[ev.Tier - 1] = SpawnPulseDuration;
		}
	}

	private struct BucketRow
	{
		public Control RowRoot;
		public Label Label;
		public ProgressBar Bar;
		public Color BaseModulate;
	}

	private Vector2 GetBucketTargetPosition(int tier)
	{
		int index = tier - 1;
		if (index < 0 || index >= _bucketRows.Length) return Vector2.Zero;

		var bar = _bucketRows[index].Bar;
		var globalRect = bar.GetGlobalRect();
		if (globalRect.Size.X <= 1f || globalRect.Size.Y <= 1f) return Vector2.Zero;

		var center = globalRect.Position + globalRect.Size * 0.5f;
		if (_orbLayer == null) return Vector2.Zero;
		var xform = _orbLayer.GetGlobalTransformWithCanvas().AffineInverse();
		return xform * center;
	}

	private static float SmoothFill(float current, float target, float dt)
	{
		float t = 1f - Mathf.Exp(-FillLerpSpeed * dt);
		return Mathf.Lerp(current, target, t);
	}

	private static float PulseCurve(float timer, float duration)
	{
		if (duration <= 0f) return 0f;
		float t = timer / duration;
		return Mathf.Sin(t * Mathf.Pi);
	}

	private static Color ApplyPulse(Color baseColor, float pulse)
	{
		float boost = Mathf.Clamp(pulse, 0f, 1f);
		return new Color(
			baseColor.R + (1f - baseColor.R) * boost,
			baseColor.G + (1f - baseColor.G) * boost,
			baseColor.B + (1f - baseColor.B) * boost,
			baseColor.A);
	}
}
