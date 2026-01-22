#nullable enable

namespace PixelArmies.SimCore;

public readonly record struct DebugSettings(bool Enabled, float IntervalSeconds, string Prefix)
{
	public static DebugSettings Disabled => new(false, 0f, "");

	public DebugSettings WithPrefix(string prefix)
	{
		return new DebugSettings(Enabled, IntervalSeconds, prefix ?? "");
	}
}
