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

		var debugArgs = DebugArgs.Parse(OS.GetCmdlineUserArgs());

		if (AnalyzerRunner.TryRunFromArgs())
		{
			GetTree().Quit();
			return;
		}

		GD.Print("Started visual mode (game host)");
		var host = new BattleGameHost();
		host.ConfigureDebug(debugArgs.ToDebugSettings());
		AddChild(host);
	}
}
