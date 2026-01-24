#nullable enable

using Godot;
using PixelArmies.Analyzer;
using PixelArmies.GameHost;
using PixelArmies.Presentation;

namespace PixelArmies;

public partial class Main : Control
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

		var worldViewport = GetNodeOrNull<SubViewport>("WorldViewportContainer/WorldViewport");
		if (worldViewport != null)
		{
			worldViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
			worldViewport.AddChild(host);
		}
		else
		{
			AddChild(host);
		}

		var hud = GetNodeOrNull<HudRoot>("Hud");
		if (hud != null)
		{
			host.AttachHud(hud);
		}
	}
}
