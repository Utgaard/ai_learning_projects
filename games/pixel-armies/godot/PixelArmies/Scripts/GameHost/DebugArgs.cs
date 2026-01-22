#nullable enable

using System.Globalization;
using PixelArmies.SimCore;

namespace PixelArmies;

public readonly record struct DebugArgs(bool Enabled, float IntervalSeconds)
{
	public static DebugArgs Parse(string[] args)
	{
		bool enabled = false;
		float interval = 3f;

		for (int i = 0; i < args.Length; i++)
		{
			var a = args[i];
			if (a == "debug" || a == "--debug")
			{
				enabled = true;
			}
			else if (a.StartsWith("--debug-interval=") &&
				float.TryParse(a["--debug-interval=".Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
			{
				interval = v;
			}
		}

		if (interval <= 0f) interval = 3f;
		return new DebugArgs(enabled, interval);
	}

	public DebugSettings ToDebugSettings()
	{
		if (!Enabled) return DebugSettings.Disabled;
		return new DebugSettings(true, IntervalSeconds, "");
	}
}
