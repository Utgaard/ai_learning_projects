#nullable enable

using Godot;
using PixelArmies.Content;
using PixelArmies.Presentation;
using PixelArmies.SimCore;
using SimSide = PixelArmies.SimCore.Side;

namespace PixelArmies.GameHost;

public partial class BattleGameHost : Node2D
{
	private const float GroundY = 120f;

	private BattleSimulator? _sim;
	private SimConfig? _cfg;

	private float _accum;
	private float _printTimer;

	private Camera2D? _cam;
	private Battlefield? _battlefield;
	private BattleView? _view;

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
			PositionSmoothingEnabled = true,
			PositionSmoothingSpeed = 6f,
			Zoom = new Vector2(1.2f, 1.2f), // tweak later
		};
		AddChild(_cam);

		GD.Print("Started demo battle sim (visual debug).");
	}

	public override void _Process(double delta)
	{
		if (_sim == null || _cfg == null || _view == null) return;

		if (!_sim.State.IsOver)
		{
			_accum += (float)delta;
			while (_accum >= SimConfig.FixedDt)
			{
				_sim.Step(SimConfig.FixedDt);
				_accum -= SimConfig.FixedDt;
			}
		}

		// Debug prints
		_printTimer += (float)delta;
		if (_printTimer >= 1.0f && !_sim.State.IsOver)
		{
			_printTimer = 0f;
			GD.Print($"t={_sim.State.Time:0.0}s units={_sim.State.Units.Count} LBase={_sim.State.LeftBaseHp:0} RBase={_sim.State.RightBaseHp:0}");
		}

		UpdateCamera();
		_view.QueueRedraw();
	}

	private void UpdateCamera()
	{
		if (_sim == null || _cfg == null || _cam == null) return;

		// Find foremost unit for each side (closest to enemy base)
		float leftForemost = 0f;
		float rightForemost = _cfg.BattlefieldLength;

		foreach (var u in _sim.State.Units)
		{
			if (!u.Alive) continue;

			if (u.Side == SimSide.Left)
				leftForemost = Mathf.Max(leftForemost, u.X);
			else
				rightForemost = Mathf.Min(rightForemost, u.X);
		}

		float mid = (leftForemost + rightForemost) * 0.5f;

		// World coordinates: we'll map x -> pixel x directly for now
		_cam.Position = new Vector2(mid, 0f);
	}
}
