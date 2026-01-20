#nullable enable

using PixelArmies.SimCore;

namespace PixelArmies.Content;

// Demo armies (same as before, positional args to avoid name issues)
internal static class DemoArmies
{
	public static ArmyDef LeftBasic()
	{
		var a = new ArmyDef("Left Basic");
		a.Units.Add(new UnitDef("infantry", 1, 6, 60, 12, 25, 90, false, 0.75f, 9, 0.6f));
		a.Units.Add(new UnitDef("spearman", 2, 12, 100, 18, 30, 80, false, 0.9f, 4, 0.8f));
		a.Units.Add(new UnitDef("archer", 3, 22, 80, 22, 140, 75, false, 1.0f, 0, 0f));
		a.Units.Add(new UnitDef("ogre", 4, 40, 380, 40, 35, 55, false, 1.3f, 0, 0f));
		return a;
	}

	public static ArmyDef RightBasic()
	{
		var a = new ArmyDef("Right Basic");
		a.Units.Add(new UnitDef("raider", 1, 6, 55, 13, 25, 95, false, 0.9f, 0, 0f));
		a.Units.Add(new UnitDef("brute", 2, 13, 130, 15, 25, 70, false, 1.05f, 0, 0f));
		a.Units.Add(new UnitDef("caster", 3, 24, 70, 28, 150, 70, false, 1.15f, 0, 0f));
		a.Units.Add(new UnitDef("dragon", 4, 45, 260, 46, 110, 80, true, 1.45f, 0, 0f));
		return a;
	}
}
