#nullable enable

using System.Collections.Generic;
using Godot;
using PixelArmies.Analyzer;
using PixelArmies.Content;
using PixelArmies.GameHost;
using PixelArmies.Presentation;
using PixelArmies.Presentation.DevTools.UnitViewer;
using PixelArmies.SimCore;

namespace PixelArmies;

public partial class Main : Control
{
	private const int MaxHistoryEntries = 5;
	private readonly List<MatchRecord> _history = new();

	public override void _Ready()
	{
		GD.Print("Pixel Armies booting...");

		var debugArgs = DebugArgs.Parse(OS.GetCmdlineUserArgs());

		if (AnalyzerRunner.TryRunFromArgs())
		{
			GetTree().Quit();
			return;
		}

		if (HasArg("--viewer"))
		{
			GD.Print("Started Unit Viewer mode.");
			var viewerHud = GetNodeOrNull<CanvasLayer>("Hud");
			if (viewerHud != null) viewerHud.QueueFree();
			AddChild(new UnitViewer());
			return;
		}

		ShowArmySelect(debugArgs.ToDebugSettings());
	}

	private void ShowArmySelect(DebugSettings debugSettings)
	{
		GD.Print("Showing army selection screen.");
		var selector = new ArmySelectScreen(DemoArmies.All(), _history);
		selector.OnConfirmed = (left, right) => LaunchBattle(left, right, debugSettings);
		AddChild(selector);
	}

	private void LaunchBattle(ArmyDef left, ArmyDef right, DebugSettings debugSettings)
	{
		GD.Print($"Starting battle: {left.Name} vs {right.Name}");

		var host = new BattleGameHost();
		host.SetArmies(left, right);
		host.SetMatchHistory(_history);
		host.ConfigureDebug(debugSettings);
		host.OnBattleEnded += RecordMatch;

		// When host frees itself (S key), rebuild the select screen
		host.TreeExited += () =>
		{
			// Guard: only show select if we're still in the tree
			if (!IsInsideTree()) return;
			ShowArmySelect(debugSettings);
		};

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
			host.AttachHud(hud);
	}

	private void RecordMatch(MatchRecord record)
	{
		_history.Add(record);
		if (_history.Count > MaxHistoryEntries)
			_history.RemoveAt(0);
	}

	private static bool HasArg(string arg)
	{
		var args = OS.GetCmdlineUserArgs();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i] == arg) return true;
		}
		return false;
	}
}
