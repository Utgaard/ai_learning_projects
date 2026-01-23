#nullable enable

using System.Collections.Generic;
using Godot;
using PixelArmies.GameHost;
using PixelArmies.SimCore;

namespace PixelArmies.Presentation;

public partial class StatusPanel : PanelContainer
{
	private const float PulseDuration = 0.25f;
	private const float PulseScale = 0.25f;

	private Label? _leftKillsLabel;
	private Label? _rightKillsLabel;
	private Label? _leftBaseLabel;
	private Label? _rightBaseLabel;
	private Label? _leftDamageLabel;
	private Label? _rightDamageLabel;

	private int _leftKills;
	private int _rightKills;
	private float _leftPulseTime;
	private float _rightPulseTime;

	public override void _Ready()
	{
		SetProcess(true);

		var root = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};
		root.AddThemeConstantOverride("separation", 6);

		var title = new Label
		{
			Text = "BATTLE STATUS",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 18);
		root.AddChild(title);

		_leftKillsLabel = BuildValueLabel(32, HorizontalAlignment.Left);
		_rightKillsLabel = BuildValueLabel(32, HorizontalAlignment.Right);
		_leftBaseLabel = BuildValueLabel(16, HorizontalAlignment.Left);
		_rightBaseLabel = BuildValueLabel(16, HorizontalAlignment.Right);
		_leftDamageLabel = BuildValueLabel(16, HorizontalAlignment.Left);
		_rightDamageLabel = BuildValueLabel(16, HorizontalAlignment.Right);

		root.AddChild(BuildRow("KILLS", _leftKillsLabel, _rightKillsLabel));
		root.AddChild(BuildRow("BASE HP", _leftBaseLabel, _rightBaseLabel));
		root.AddChild(BuildRow("DAMAGE", _leftDamageLabel, _rightDamageLabel));

		AddChild(root);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		UpdatePulse(_leftKillsLabel, ref _leftPulseTime, dt);
		UpdatePulse(_rightKillsLabel, ref _rightPulseTime, dt);
	}

	public void ApplySnapshot(HudSideSnapshot left, HudSideSnapshot right)
	{
		if (_leftKills != left.Kills)
		{
			_leftKills = left.Kills;
			_leftPulseTime = PulseDuration;
		}

		if (_rightKills != right.Kills)
		{
			_rightKills = right.Kills;
			_rightPulseTime = PulseDuration;
		}

		if (_leftKillsLabel != null) _leftKillsLabel.Text = left.Kills.ToString();
		if (_rightKillsLabel != null) _rightKillsLabel.Text = right.Kills.ToString();
		if (_leftBaseLabel != null) _leftBaseLabel.Text = left.BaseHp.ToString("0");
		if (_rightBaseLabel != null) _rightBaseLabel.Text = right.BaseHp.ToString("0");
		if (_leftDamageLabel != null) _leftDamageLabel.Text = left.DamageDealt.ToString("0");
		if (_rightDamageLabel != null) _rightDamageLabel.Text = right.DamageDealt.ToString("0");
	}

	public void OnUnitDiedEvents(IReadOnlyList<UnitDiedEvent> deathEvents)
	{
		_ = deathEvents;
	}

	public void OnDamageEvents(IReadOnlyList<DamageEvent> damageEvents)
	{
		_ = damageEvents;
	}

	private static Label BuildValueLabel(int fontSize, HorizontalAlignment alignment)
	{
		var label = new Label
		{
			Text = "0",
			HorizontalAlignment = alignment,
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		return label;
	}

	private static HBoxContainer BuildRow(string label, Label leftValue, Label rightValue)
	{
		var row = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 6);

		leftValue.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		rightValue.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

		var mid = new Label
		{
			Text = label,
			HorizontalAlignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(90f, 0f),
		};

		row.AddChild(leftValue);
		row.AddChild(mid);
		row.AddChild(rightValue);
		return row;
	}

	private static void UpdatePulse(Label? label, ref float timer, float dt)
	{
		if (label == null) return;

		if (timer > 0f)
		{
			timer = Mathf.Max(0f, timer - dt);
			float t = timer / PulseDuration;
			float pulse = 1f + PulseScale * Mathf.Sin(t * Mathf.Pi);
			label.Scale = new Vector2(pulse, pulse);
		}
		else
		{
			label.Scale = Vector2.One;
		}
	}
}
