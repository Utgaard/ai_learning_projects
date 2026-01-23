#nullable enable

using PixelArmies.SimCore;

namespace PixelArmies.Content;

// Demo armies (same as before, positional args to avoid name issues)
internal static class DemoArmies
{
	public static ArmyDef LeftBasic()
	{
		var a = new ArmyDef("Left Basic");
		a.Units.Add(new UnitDef("infantry", 1, 6, 20, 6, 2.0f, 25, 90, MovementClass.Ground, TargetingPolicy.Frontmost, 0.75f, 9, 0.6f));
		a.Units.Add(new UnitDef("spearman", 2, 12, 100, 9, 2.0f, 55, 80, MovementClass.Ground, TargetingPolicy.Frontmost, 0.9f, 4, 0.8f));
		a.Units.Add(new UnitDef("archer", 3, 22, 80, 11, 2.0f, 140, 75, MovementClass.Air, TargetingPolicy.Frontmost, 1.0f, 0, 0f));
		a.Units.Add(new UnitDef("ogre", 4, 40, 380, 24, 1.6f, 70, 55, MovementClass.Ground, TargetingPolicy.Frontmost, 1.3f, 0, 0f));
		return a;
	}

	public static ArmyDef RightBasic()
	{
		var a = new ArmyDef("Right Basic");
		a.Units.Add(new UnitDef("raider", 1, 6, 15, 6.5f, 2.0f, 25, 95, MovementClass.Ground, TargetingPolicy.Frontmost, 0.9f, 0, 0f));
		a.Units.Add(new UnitDef("brute", 2, 13, 130, 7.5f, 2.0f, 55, 70, MovementClass.Ground, TargetingPolicy.Frontmost, 1.05f, 0, 0f));
		a.Units.Add(new UnitDef("caster", 3, 24, 70, 14, 2.0f, 150, 70, MovementClass.Ground, TargetingPolicy.ClosestInRange, 1.15f, 0, 0f));
		a.Units.Add(new UnitDef("dragon", 4, 45, 260, 30, 1.5f, 110, 80, MovementClass.Air, TargetingPolicy.Frontmost, 1.45f, 0, 0f));
		return a;
	}
}
