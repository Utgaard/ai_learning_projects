#nullable enable

using System.Collections.Generic;
using Godot;
using PixelArmies.GameHost;

namespace PixelArmies.Presentation;

public partial class RandomizerPanel : PanelContainer
{
	private const int TierCount = 4;
	private static readonly Color LockedColor = new(1f, 1f, 1f, 0.35f);
	private static readonly Color UnlockedColor = new(1f, 1f, 1f, 1f);

	public bool IsRightSide { get; set; }

	private Label? _titleLabel;
	private Label? _tierUnlockedLabel;
	private readonly BucketRow[] _bucketRows = new BucketRow[TierCount];

	public override void _Ready()
	{
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
			};
		}

		AddChild(root);
	}

	public void ApplySnapshot(HudSideSnapshot snapshot)
	{
		if (_tierUnlockedLabel != null)
		{
			_tierUnlockedLabel.Text = $"Tier Unlocked: {snapshot.UnlockedTier}";
		}

		for (int i = 0; i < _bucketRows.Length; i++)
		{
			int tier = i + 1;
			var row = _bucketRows[i];
			bool unlocked = tier <= snapshot.UnlockedTier;
			row.RowRoot.Modulate = unlocked ? UnlockedColor : LockedColor;

			float fill = 0f;
			if (tier == snapshot.CurrentBucketTier && snapshot.CurrentTargetCost > 0f)
			{
				fill = Mathf.Clamp(snapshot.CurrentBucketProgress / snapshot.CurrentTargetCost, 0f, 1f);
			}
			row.Bar.Value = fill;
		}
	}

	public void OnUnitSpawnedEvents(IReadOnlyList<HudUnitSpawnedEvent> spawnEvents)
	{
		_ = spawnEvents;
	}

	private struct BucketRow
	{
		public Control RowRoot;
		public Label Label;
		public ProgressBar Bar;
	}
}
