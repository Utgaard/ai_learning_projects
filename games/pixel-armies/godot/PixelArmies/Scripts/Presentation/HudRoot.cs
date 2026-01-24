#nullable enable

using System.Collections.Generic;
using Godot;
using PixelArmies.GameHost;
using PixelArmies.SimCore;

namespace PixelArmies.Presentation;

public partial class HudRoot : CanvasLayer
{
	private const float TopBarAnchorBottom = 0.33f;
	private const float PanelPadding = 8f;

	private RandomizerPanel? _leftPanel;
	private RandomizerPanel? _rightPanel;
	private StatusPanel? _statusPanel;

	public override void _Ready()
	{
		Layer = 10;

		var topBar = new PanelContainer
		{
			Name = "TopHudBar",
			AnchorLeft = 0f,
			AnchorTop = 0f,
			AnchorRight = 1f,
			AnchorBottom = TopBarAnchorBottom,
			OffsetLeft = 0f,
			OffsetTop = 0f,
			OffsetRight = 0f,
			OffsetBottom = 0f,
		};

		var plate = new ColorRect
		{
			Name = "HudPlate",
			AnchorLeft = 0f,
			AnchorTop = 0f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 0f,
			OffsetTop = 0f,
			OffsetRight = 0f,
			OffsetBottom = 0f,
			Color = new Color(0.06f, 0.07f, 0.10f, 0.88f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 0,
			ZAsRelative = true,
		};

		var border = new ColorRect
		{
			Name = "HudBorder",
			AnchorLeft = 0f,
			AnchorTop = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 0f,
			OffsetTop = -2f,
			OffsetRight = 0f,
			OffsetBottom = 0f,
			Color = new Color(1f, 1f, 1f, 0.08f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = 1,
			ZAsRelative = true,
		};

		var margin = new MarginContainer
		{
			AnchorLeft = 0f,
			AnchorTop = 0f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = PanelPadding,
			OffsetTop = PanelPadding,
			OffsetRight = -PanelPadding,
			OffsetBottom = -PanelPadding,
			ZIndex = 2,
			ZAsRelative = true,
		};

		var row = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 8);

		_leftPanel = new RandomizerPanel { IsRightSide = false };
		_statusPanel = new StatusPanel();
		_rightPanel = new RandomizerPanel { IsRightSide = true };

		_leftPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_statusPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_rightPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

		_leftPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_statusPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_rightPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

		row.AddChild(_leftPanel);
		row.AddChild(_statusPanel);
		row.AddChild(_rightPanel);

		topBar.AddChild(plate);
		topBar.AddChild(border);
		margin.AddChild(row);
		topBar.AddChild(margin);
		AddChild(topBar);
	}

	public void UpdateHud(
		HudSnapshot snapshot,
		IReadOnlyList<PowerAllocatedEvent> powerEvents,
		IReadOnlyList<UnitSpawnedEvent> spawnEvents,
		IReadOnlyList<UnitDiedEvent> deathEvents,
		IReadOnlyList<DamageEvent> damageEvents)
	{
		_leftPanel?.ApplySnapshot(snapshot.Left);
		_rightPanel?.ApplySnapshot(snapshot.Right);
		_statusPanel?.ApplySnapshot(snapshot.Left, snapshot.Right);

		_leftPanel?.OnPowerAllocatedEvents(powerEvents);
		_rightPanel?.OnPowerAllocatedEvents(powerEvents);
		_leftPanel?.OnUnitSpawnedEvents(spawnEvents);
		_rightPanel?.OnUnitSpawnedEvents(spawnEvents);
		_statusPanel?.OnUnitDiedEvents(deathEvents);
		_statusPanel?.OnDamageEvents(damageEvents);
	}
}
