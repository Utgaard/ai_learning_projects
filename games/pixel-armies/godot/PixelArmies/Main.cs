#nullable enable

using Godot;
using PixelArmies.SimCore;
using SimSide = PixelArmies.SimCore.Side;


public partial class Main : Node2D
{
	private const float GroundY = 120f;
	private BattleSimulator? _sim;
	private SimConfig? _cfg;

	private float _accum;
	private float _printTimer;

	private Camera2D? _cam;
	private Battlefield? _battlefield;

	public override void _Ready()
	{
		GD.Print("Pixel Armies booting...");

		if (TryRunAnalyzerFromArgs())
		{
			GetTree().Quit();
			return;
		}

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
		if (_sim == null || _cfg == null) return;

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
		QueueRedraw();
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

		// World coordinates: we’ll map x -> pixel x directly for now
		_cam.Position = new Vector2(mid, 0f);
	}

	public override void _Draw()
	{
		if (_sim == null || _cfg == null) return;

		// Simple coordinate system:
		// X = sim X
		// Y = 0 is ground line; units drawn above it

		// Bases
		DrawRect(new Rect2(-30, GroundY - 60, 30, 60), Colors.White);
		DrawRect(new Rect2(_cfg.BattlefieldLength, GroundY - 60, 30, 60), Colors.White);

		// Base HP bars (simple)
		float barW = 120f;
		float barH = 10f;
		float lPct = Mathf.Clamp(_sim.State.LeftBaseHp / _cfg.BaseMaxHp, 0f, 1f);
		float rPct = Mathf.Clamp(_sim.State.RightBaseHp / _cfg.BaseMaxHp, 0f, 1f);

		DrawRect(new Rect2(10, GroundY + 20, barW, barH), Colors.Black);
		DrawRect(new Rect2(10, GroundY + 20, barW * lPct, barH), Colors.White);

		DrawRect(new Rect2(_cfg.BattlefieldLength - barW - 10, GroundY + 20, barW, barH), Colors.Black);
		DrawRect(new Rect2(_cfg.BattlefieldLength - barW - 10, GroundY + 20, barW * rPct, barH), Colors.White);

		// Units as rectangles
		foreach (var u in _sim.State.Units)
		{
			float y = GroundY - 12;
			float h = 12;
			float w = 10;

			// Make big units bigger (tier proxy)
			w += (u.Def.Tier - 1) * 4;
			h += (u.Def.Tier - 1) * 4;

			if (u.Def.IsAir)
				y -= 40f; // air height

			// Left units slightly different than right
			var c = u.Side == SimSide.Left ? Colors.Cyan : Colors.Orange;

			DrawRect(new Rect2(u.X - w * 0.5f, y - h, w, h), c);
		}

		// End text (minimal)
		if (_sim.State.IsOver)
		{
			var winner = _sim.State.Winner == SimSide.Left ? "LEFT WINS" : "RIGHT WINS";
			DrawString(ThemeDB.FallbackFont, new Vector2(20, 40), winner, fontSize: 32, modulate: Colors.White);
		}
	}

	private bool TryRunAnalyzerFromArgs()
	{
		var args = OS.GetCmdlineArgs();

		bool analyze = false;
		int runs = 1000;
		int seed = 42;

		foreach (var a in args)
		{
			if (a == "--analyze") analyze = true;
			else if (a.StartsWith("--runs=") && int.TryParse(a["--runs=".Length..], out var r)) runs = r;
			else if (a.StartsWith("--seed=") && int.TryParse(a["--seed=".Length..], out var s)) seed = s;
		}

		if (!analyze) return false;

		var cfg = new SimConfig();
		var left = DemoArmies.LeftBasic();
		var right = DemoArmies.RightBasic();

		var stats = Analyzer.RunMany(cfg, left, right, runs, seed);
		GD.Print("=== MATCHUP ANALYSIS ===");
		GD.Print(stats.ToString());
		return true;
	}
}

// Demo armies (same as before, positional args to avoid name issues)
internal static class DemoArmies
{
	public static ArmyDef LeftBasic()
	{
		var a = new ArmyDef("Left Basic");
		a.Units.Add(new UnitDef("infantry", 1, 6,  60, 12,  25, 90, false));
		a.Units.Add(new UnitDef("spearman", 2, 12, 100, 18, 30, 80, false));
		a.Units.Add(new UnitDef("archer",  3, 22, 80,  22, 140, 75, false));
		a.Units.Add(new UnitDef("ogre",    4, 40, 380, 40, 35, 55, false));
		return a;
	}

	public static ArmyDef RightBasic()
	{
		var a = new ArmyDef("Right Basic");
		a.Units.Add(new UnitDef("raider", 1, 6,  55, 13, 25, 95, false));
		a.Units.Add(new UnitDef("brute",  2, 13, 130, 15, 25, 70, false));
		a.Units.Add(new UnitDef("caster", 3, 24, 70, 28, 150, 70, false));
		a.Units.Add(new UnitDef("dragon", 4, 45, 260, 46, 110, 80, true));
		return a;
	}
}
