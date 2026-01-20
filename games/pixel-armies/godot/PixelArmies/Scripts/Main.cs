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

		if (AnalyzerRunner.TryRunFromArgs())
		{
			GetTree().Quit();
			return;
		}

		var host = new BattleGameHost();
		AddChild(host);
	}
}
