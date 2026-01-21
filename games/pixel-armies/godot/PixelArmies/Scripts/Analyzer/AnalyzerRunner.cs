#nullable enable

using Godot;
using PixelArmies.Content;
using PixelArmies.SimCore;

namespace PixelArmies.Analyzer;

public static class AnalyzerRunner
{
	
	public static bool TryRunFromArgs()
	{
		var args = OS.GetCmdlineUserArgs();

		bool analyze = false;
		int runs = 1000;
		int seed = 42;

		foreach (var a in args)
		{
			if (a == "analyze") analyze = true;
			else if (a.StartsWith("--runs=") && int.TryParse(a["--runs=".Length..], out var r)) runs = r;
			else if (a.StartsWith("--seed=") && int.TryParse(a["--seed=".Length..], out var s)) seed = s;
		}

		if (!analyze) return false;

		var cfg = new SimConfig();
		var left = DemoArmies.LeftBasic();
		var right = DemoArmies.RightBasic();

		var stats = SimCore.Analyzer.RunMany(cfg, left, right, runs, seed);
		GD.Print("=== MATCHUP ANALYSIS ===");
		GD.Print(stats.ToString());
		return true;
	}
}
