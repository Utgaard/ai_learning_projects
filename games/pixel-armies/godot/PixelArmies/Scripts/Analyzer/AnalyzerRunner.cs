#nullable enable

using System;
using System.Diagnostics;
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
		float maxSeconds = 240f;
		int progressInterval = 0;

		foreach (var a in args)
		{
			if (a == "analyze") analyze = true;
			else if (a.StartsWith("--runs=") && int.TryParse(a["--runs=".Length..], out var r)) runs = r;
			else if (a.StartsWith("--seed=") && int.TryParse(a["--seed=".Length..], out var s)) seed = s;
			else if (a.StartsWith("--max-seconds=") && float.TryParse(a["--max-seconds=".Length..], out var m)) maxSeconds = m;
			else if (a.StartsWith("--progress=") && int.TryParse(a["--progress=".Length..], out var p)) progressInterval = p;
		}

		if (!analyze) return false;

		GD.Print("USER ARGS: " + string.Join(" | ", args));
		GD.Print("=== MATCHUP ANALYSIS ===");

		if (runs < 1) runs = 1;
		if (maxSeconds <= 0f) maxSeconds = 240f;

		if (progressInterval <= 0)
		{
			int approx = Math.Max(1, runs / 20);
			progressInterval = Math.Min(approx, 250);
		}

		var stopwatch = Stopwatch.StartNew();

		var cfg = new SimConfig();
		var left = DemoArmies.LeftBasic();
		var right = DemoArmies.RightBasic();

		var stats = SimCore.Analyzer.RunMany(
			cfg,
			left,
			right,
			runs,
			seed,
			maxSeconds,
			progressInterval,
			(current, total) =>
			{
				double elapsed = stopwatch.Elapsed.TotalSeconds;
				float pct = total > 0 ? (100f * current / total) : 100f;
				GD.Print($"Progress: {current}/{total} ({pct:0.0}%) elapsed={elapsed:0.0}s");
			});

		GD.Print(stats.ToString());
		return true;
	}
}
