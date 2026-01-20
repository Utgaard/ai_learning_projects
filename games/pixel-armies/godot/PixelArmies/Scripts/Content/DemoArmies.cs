#nullable enable

using PixelArmies.SimCore;

namespace PixelArmies.Content;

// Demo armies (same as before, positional args to avoid name issues)
internal static class DemoArmies
{
	public static ArmyDef LeftBasic()
	{
		var a = new ArmyDef("Left Basic");
		a.Units.Add(new UnitDef("infantry", 1, 6, 60, 12, 25, 90, false));
		a.Units.Add(new UnitDef("spearman", 2, 12, 100, 18, 30, 80, false));
		a.Units.Add(new UnitDef("archer", 3, 22, 80, 22, 140, 75, false));
		a.Units.Add(new UnitDef("ogre", 4, 40, 380, 40, 35, 55, false));
		return a;
	}

	public static ArmyDef RightBasic()
	{
		var a = new ArmyDef("Right Basic");
		a.Units.Add(new UnitDef("raider", 1, 6, 55, 13, 25, 95, false));
		a.Units.Add(new UnitDef("brute", 2, 13, 130, 15, 25, 70, false));
		a.Units.Add(new UnitDef("caster", 3, 24, 70, 28, 150, 70, false));
		a.Units.Add(new UnitDef("dragon", 4, 45, 260, 46, 110, 80, true));
		return a;
	}
}
