using Godot;
using PixelArmies.SimCore;

public partial class Main : Node
{
	private BattleSimulator? _sim;
	private float _accum;
	private float _printTimer;

	public override void _Ready()
	{
		GD.Print("Pixel Armies booting...");

		// If launched with --analyze, run headless analyzer and quit.
		if (TryRunAnalyzerFromArgs())
		{
			GetTree().Quit();
			return;
		}

		// Otherwise, start a single battle sim (we'll render later)
		var cfg = new SimConfig();

		var left = DemoArmies.LeftBasic();
		var right = DemoArmies.RightBasic();

		_sim = new BattleSimulator(cfg, left, right, seed: 12345);
		GD.Print("Started demo battle sim.");
	}

	public override void _Process(double delta)
	{
		if (_sim == null || _sim.State.IsOver) return;

		_accum += (float)delta;
		while (_accum >= SimConfig.FixedDt)
		{
			_sim.Step(SimConfig.FixedDt);
			_accum -= SimConfig.FixedDt;
		}

		_printTimer += (float)delta;
		if (_printTimer >= 1.0f)
		{
			_printTimer = 0f;
			GD.Print($"t={_sim.State.Time:0.0}s units={_sim.State.Units.Count} LBase={_sim.State.LeftBaseHp:0} RBase={_sim.State.RightBaseHp:0}");
		}
	}

	private bool TryRunAnalyzerFromArgs()
	{
		// Godot: OS.GetCmdlineArgs() returns args after the executable name.
		// We'll use: --analyze --runs=1000 --seed=42
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

// Demo armies live here for now. Later we load these from JSON/data files.
internal static class DemoArmies
{
	public static ArmyDef LeftBasic()
	{
		var a = new ArmyDef("Left Basic");

		// Tier 1
		a.Units.Add(new UnitDef("infantry", Tier: 1, Cost: 6,  MaxHp: 60,  Dps: 12, Range: 25, Speed: 90,  IsAir: false));
		// Tier 2
		a.Units.Add(new UnitDef("spearman", Tier: 2, Cost: 12, MaxHp: 100, Dps: 18, Range: 30, Speed: 80,  IsAir: false));
		// Tier 3
		a.Units.Add(new UnitDef("archer",  Tier: 3, Cost: 22, MaxHp: 80,  Dps: 22, Range: 140, Speed: 75, IsAir: false));
		// Tier 4
		a.Units.Add(new UnitDef("ogre",    Tier: 4, Cost: 40, MaxHp: 380, Dps: 40, Range: 35, Speed: 55,  IsAir: false));

		return a;
	}

	public static ArmyDef RightBasic()
	{
		var a = new ArmyDef("Right Basic");

		// Tier 1
		a.Units.Add(new UnitDef("raider",  Tier: 1, Cost: 6,  MaxHp: 55,  Dps: 13, Range: 25, Speed: 95,  IsAir: false));
		// Tier 2
		a.Units.Add(new UnitDef("brute",   Tier: 2, Cost: 13, MaxHp: 130, Dps: 15, Range: 25, Speed: 70,  IsAir: false));
		// Tier 3
		a.Units.Add(new UnitDef("caster",  Tier: 3, Cost: 24, MaxHp: 70,  Dps: 28, Range: 150, Speed: 70, IsAir: false));
		// Tier 4
		a.Units.Add(new UnitDef("dragon",  Tier: 4, Cost: 45, MaxHp: 260, Dps: 46, Range: 110, Speed: 80, IsAir: true));

		return a;
	}
}
