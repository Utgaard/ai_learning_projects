#nullable enable

using System.Collections.Generic;
using Godot;
using PixelArmies.Content;
using PixelArmies.Presentation;
using PixelArmies.SimCore;
using SimSide = PixelArmies.SimCore.Side;

namespace PixelArmies.GameHost;

public partial class BattleGameHost : Node2D
{
	private const float GroundY = 120f;
	private const float PaddingFactor = 1.18f;
	private const float MinSpan = 200f;
	private const float MinZoom = 0.10f;
	private const float MaxZoom = 1.0f;
	private const float PosSpeed = 6f;
	private const float ZoomSpeed = 5f;

	private BattleSimulator? _sim;
	private SimConfig? _cfg;

	private float _accum;
	private float _printTimer;

	private Camera2D? _cam;
	private Battlefield? _battlefield;
	private BattleView? _view;
	private readonly List<DamageEvent> _frameDamageEvents = new();

	public override void _Ready()
	{
		_cfg = new SimConfig();

		var left = DemoArmies.LeftBasic();
		var right = DemoArmies.RightBasic();
		_sim = new BattleSimulator(_cfg, left, right, seed: 12345);

		// Battlefield background
		_battlefield = new Battlefield
		{
			BattlefieldLength = _cfg.BattlefieldLength,
			GroundY = GroundY,
			ZIndex = -10,
			ZAsRelative = true,
		};
		AddChild(_battlefield);

		// Units and base rendering
		_view = new BattleView();
		_view.Configure(_sim, _cfg, GroundY);
		AddChild(_view);

		// Camera
		_cam = new Camera2D
		{
			Enabled = true,
			PositionSmoothingEnabled = false,
			Zoom = new Vector2(MaxZoom, MaxZoom),
		};
		AddChild(_cam);

		GD.Print("Started demo battle sim (visual debug).");
	}

	public override void _Process(double delta)
	{
		if (_sim == null || _cfg == null || _view == null) return;

		_frameDamageEvents.Clear();

		if (!_sim.State.IsOver)
		{
			_accum += (float)delta;
			while (_accum >= SimConfig.FixedDt)
			{
				_sim.Step(SimConfig.FixedDt);
				var events = _sim.ConsumeDamageEvents();
				if (events.Count > 0) _frameDamageEvents.AddRange(events);
				_accum -= SimConfig.FixedDt;
			}
		}
		else
		{
			var events = _sim.ConsumeDamageEvents();
			if (events.Count > 0) _frameDamageEvents.AddRange(events);
		}

		// Debug prints
		_printTimer += (float)delta;
		if (_printTimer >= 1.0f && !_sim.State.IsOver)
		{
			_printTimer = 0f;
			GD.Print($"t={_sim.State.Time:0.0}s units={_sim.State.Units.Count} LBase={_sim.State.LeftBaseHp:0} RBase={_sim.State.RightBaseHp:0}");
		}

		_view.Advance((float)delta, _frameDamageEvents);
		UpdateCamera((float)delta);
		_view.QueueRedraw();
	}

	private void UpdateCamera(float dt)
	{
		if (_sim == null || _cfg == null || _cam == null) return;

		// Find foremost unit for each side (closest to enemy base)
		float leftBaseX = 0f;
		float rightBaseX = _cfg.BattlefieldLength;
		float leftFrontX = leftBaseX;
		float rightFrontX = rightBaseX;

		foreach (var u in _sim.State.Units)
		{
			if (!u.Alive) continue;

			if (u.Side == SimSide.Left)
				leftFrontX = Mathf.Max(leftFrontX, u.X);
			else
				rightFrontX = Mathf.Min(rightFrontX, u.X);
		}

		float baseSpan = rightBaseX - leftBaseX;
		float frontSpan = Mathf.Max(MinSpan, rightFrontX - leftFrontX);
		float t = 1f - Mathf.Clamp(frontSpan / Mathf.Max(1f, baseSpan), 0f, 1f);

		float baseCenter = (leftBaseX + rightBaseX) * 0.5f;
		float frontCenter = (leftFrontX + rightFrontX) * 0.5f;
		float targetCenterX = Mathf.Lerp(baseCenter, frontCenter, t);
		float targetSpan = Mathf.Lerp(baseSpan, frontSpan, t);

		float viewportWidth = GetViewportRect().Size.X;
		float desiredVisibleWidth = Mathf.Max(1f, targetSpan * PaddingFactor);
		float zoomX = viewportWidth / desiredVisibleWidth;
		zoomX = Mathf.Clamp(zoomX, MinZoom, MaxZoom);
		var targetZoom = new Vector2(zoomX, zoomX);
		var targetPos = new Vector2(targetCenterX, 0f);

		float posAlpha = 1f - Mathf.Exp(-PosSpeed * dt);
		float zoomAlpha = 1f - Mathf.Exp(-ZoomSpeed * dt);

		_cam.Position = _cam.Position.Lerp(targetPos, posAlpha);
		_cam.Zoom = _cam.Zoom.Lerp(targetZoom, zoomAlpha);
	}
}
