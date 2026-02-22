#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using PixelArmies.SimCore;

namespace PixelArmies.Presentation;

// Keyboard-driven army selection screen shown before battle.
// Left army: ← / → to cycle.  Right army: A / D to cycle.  Space / Enter to start.
public partial class ArmySelectScreen : Control
{
	public Action<ArmyDef, ArmyDef>? OnConfirmed;

	private readonly IReadOnlyList<ArmyDef> _armies;
	private readonly IReadOnlyList<MatchRecord> _history;
	private int _leftIndex;
	private int _rightIndex;

	private Label? _leftName;
	private Label? _leftUnits;
	private Label? _rightName;
	private Label? _rightUnits;
	private Label? _historyLabel;

	public ArmySelectScreen(IReadOnlyList<ArmyDef> armies, IReadOnlyList<MatchRecord>? history = null)
	{
		_armies  = armies;
		_history = history ?? Array.Empty<MatchRecord>();
		if (_armies.Count < 2)
			throw new ArgumentException("Need at least 2 armies for selection.");

		_leftIndex  = 0;
		_rightIndex = Math.Min(1, _armies.Count - 1);

		AnchorLeft = 0f; AnchorTop = 0f; AnchorRight = 1f; AnchorBottom = 1f;
	}

	public override void _Ready()
	{
		var bg = new ColorRect
		{
			AnchorLeft = 0f, AnchorTop = 0f, AnchorRight = 1f, AnchorBottom = 1f,
			Color = new Color(0.06f, 0.07f, 0.12f, 1f),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(bg);

		var layout = new VBoxContainer
		{
			AnchorLeft = 0.1f, AnchorTop = 0.08f, AnchorRight = 0.9f, AnchorBottom = 0.95f,
		};
		layout.AddThemeConstantOverride("separation", 16);
		AddChild(layout);

		var title = new Label
		{
			Text = "SELECT ARMIES",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 36);
		title.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.3f));
		layout.AddChild(title);

		var selectionRow = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical   = SizeFlags.ExpandFill,
		};
		selectionRow.AddThemeConstantOverride("separation", 20);
		layout.AddChild(selectionRow);

		var leftPanel  = BuildSidePanel(isLeft: true,  out _leftName,  out _leftUnits);
		selectionRow.AddChild(leftPanel);
		selectionRow.AddChild(new VSeparator { SizeFlagsVertical = SizeFlags.ExpandFill });
		var rightPanel = BuildSidePanel(isLeft: false, out _rightName, out _rightUnits);
		selectionRow.AddChild(rightPanel);

		var startHint = new Label
		{
			Text = "Space  /  Enter  to start battle",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		startHint.AddThemeFontSizeOverride("font_size", 18);
		startHint.AddThemeColorOverride("font_color", new Color(0.55f, 0.85f, 1f));
		layout.AddChild(startHint);

		// Match history panel
		if (_history.Count > 0)
		{
			var sep = new HSeparator();
			layout.AddChild(sep);

			_historyLabel = new Label
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				AutowrapMode = TextServer.AutowrapMode.Off,
			};
			_historyLabel.AddThemeFontSizeOverride("font_size", 13);
			_historyLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.65f, 0.65f));
			layout.AddChild(_historyLabel);
		}

		RefreshLabels();
		RefreshHistory();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

		switch (key.Keycode)
		{
			case Key.Left:
				_leftIndex = Mod(_leftIndex - 1, _armies.Count);
				RefreshLabels();
				break;
			case Key.Right:
				_leftIndex = Mod(_leftIndex + 1, _armies.Count);
				RefreshLabels();
				break;
			case Key.A:
				_rightIndex = Mod(_rightIndex - 1, _armies.Count);
				RefreshLabels();
				break;
			case Key.D:
				_rightIndex = Mod(_rightIndex + 1, _armies.Count);
				RefreshLabels();
				break;
			case Key.Space:
			case Key.Enter:
			case Key.KpEnter:
				Confirm();
				break;
		}
	}

	private void Confirm()
	{
		var left  = _armies[_leftIndex];
		var right = _armies[_rightIndex];
		OnConfirmed?.Invoke(left, right);
		QueueFree();
	}

	private Control BuildSidePanel(bool isLeft, out Label nameLabel, out Label unitsLabel)
	{
		var panel = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical   = SizeFlags.ExpandFill,
		};
		panel.AddThemeConstantOverride("separation", 10);

		string hint = isLeft ? "← / → to select" : "A / D to select";
		var hintLabel = new Label { Text = hint, HorizontalAlignment = HorizontalAlignment.Center };
		hintLabel.AddThemeFontSizeOverride("font_size", 13);
		hintLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
		panel.AddChild(hintLabel);

		nameLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		nameLabel.AddThemeFontSizeOverride("font_size", 26);
		nameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
		panel.AddChild(nameLabel);

		unitsLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		unitsLabel.AddThemeFontSizeOverride("font_size", 14);
		unitsLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.85f, 0.75f));
		panel.AddChild(unitsLabel);

		return panel;
	}

	private void RefreshLabels()
	{
		var left  = _armies[_leftIndex];
		var right = _armies[_rightIndex];

		if (_leftName  != null) _leftName.Text  = left.Name;
		if (_rightName != null) _rightName.Text = right.Name;
		if (_leftUnits  != null) _leftUnits.Text  = DescribeUnits(left);
		if (_rightUnits != null) _rightUnits.Text = DescribeUnits(right);
	}

	private void RefreshHistory()
	{
		if (_historyLabel == null || _history.Count == 0) return;

		var sb = new System.Text.StringBuilder();
		sb.AppendLine("— Recent battles —");
		int start = Math.Max(0, _history.Count - 5);
		for (int i = _history.Count - 1; i >= start; i--)
		{
			var r = _history[i];
			string stomp = r.Stomp ? "  STOMP" : "";
			sb.AppendLine($"{r.LeftArmy} vs {r.RightArmy}  →  {r.Winner}  ({r.BattleTime:0.0}s  L:{r.LeftKills} R:{r.RightKills}){stomp}");
		}
		_historyLabel.Text = sb.ToString().TrimEnd();
	}

	private static string DescribeUnits(ArmyDef army)
	{
		var sb = new System.Text.StringBuilder();
		foreach (var u in army.Units)
		{
			string extra = u.Ability != AbilityType.None ? $"  [{u.Ability}]" : "";
			sb.AppendLine($"T{u.Tier}  {u.Id}  HP:{u.MaxHp:0}  DMG:{u.Damage:0}{extra}");
		}
		return sb.ToString().TrimEnd();
	}

	private static int Mod(int x, int m) => ((x % m) + m) % m;
}
