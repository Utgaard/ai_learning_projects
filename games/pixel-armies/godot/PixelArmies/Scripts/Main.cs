#nullable enable

using Godot;
using PixelArmies.Analyzer;
using PixelArmies.GameHost;

namespace PixelArmies;

public partial class Main : Node2D
{
	public override void _Ready()
	{
		GD.Print("Pixel Armies booting...");
		GD.Print("ARGS: " + string.Join(" | ", OS.GetCmdlineArgs()));
		
		var userArgs = OS.GetCmdlineUserArgs();
		GD.Print("USER ARGS: " + string.Join(" | ", userArgs));

		if (AnalyzerRunner.TryRunFromArgs())
		{
			GD.Print("Started headless analyzer mode.");
			GetTree().Quit();
			return;
		}

		GD.Print("Started visual mode (game host)");
		var host = new BattleGameHost();
		AddChild(host);
	}
}
