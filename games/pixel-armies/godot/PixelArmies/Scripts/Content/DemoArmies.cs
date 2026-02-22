#nullable enable

using System.Collections.Generic;
using PixelArmies.SimCore;

namespace PixelArmies.Content;

internal static class DemoArmies
{
	public static IReadOnlyList<ArmyDef> All() => new[]
	{
		LeftBasic(),
		RightBasic(),
		Skirmishers(),
	};

	public static ArmyDef LeftBasic()
	{
		// Balanced escalation, slightly favors lower tiers
		var a = new ArmyDef("Legion", "legion") { TierWeights = new[] { 5f, 4f, 2.5f, 1f } };
		a.Units.Add(new UnitDef(
			Id: "infantry", Tier: 1, Cost: 6, MaxHp: 20, Damage: 6,
			AttackRate: 2.0f, Range: 14, Speed: 90,
			MovementClass: MovementClass.Ground, TargetingPolicy: TargetingPolicy.Frontmost,
			FormationSpacingMul: 0.75f, VanguardDepth: 9, VanguardSpacingMul: 0.6f,
			WeaponLength: 14f, AttackDuration: 0.22f));
		a.Units.Add(new UnitDef(
			Id: "spearman", Tier: 2, Cost: 12, MaxHp: 100, Damage: 9,
			AttackRate: 2.0f, Range: 60, Speed: 80,
			MovementClass: MovementClass.Ground, TargetingPolicy: TargetingPolicy.Frontmost,
			FormationSpacingMul: 0.9f, VanguardDepth: 4, VanguardSpacingMul: 0.8f,
			WeaponLength: 60f, AttackDuration: 1.0f));
		a.Units.Add(new UnitDef(
			Id: "archer", Tier: 3, Cost: 22, MaxHp: 80, Damage: 11,
			AttackRate: 2.0f, Range: 140, Speed: 75,
			MovementClass: MovementClass.Air, TargetingPolicy: TargetingPolicy.Frontmost,
			FormationSpacingMul: 1.0f, AttackDuration: 0.22f));
		a.Units.Add(new UnitDef(
			Id: "ogre", Tier: 4, Cost: 40, MaxHp: 380, Damage: 24,
			AttackRate: 1.6f, Range: 70, Speed: 55,
			MovementClass: MovementClass.Ground, TargetingPolicy: TargetingPolicy.Frontmost,
			FormationSpacingMul: 1.3f, AttackDuration: 0.22f,
			Ability: AbilityType.Cleave, AbilityParam: 50f));
		return a;
	}

	public static ArmyDef RightBasic()
	{
		// Elite army: fewer T1, ramps toward heavy units faster
		var a = new ArmyDef("Brutes", "brutes") { TierWeights = new[] { 3f, 5f, 4f, 3f } };
		a.Units.Add(new UnitDef(
			Id: "raider", Tier: 1, Cost: 6, MaxHp: 15, Damage: 6.5f,
			AttackRate: 2.0f, Range: 14, Speed: 95,
			MovementClass: MovementClass.Ground, TargetingPolicy: TargetingPolicy.Frontmost,
			FormationSpacingMul: 0.9f, WeaponLength: 14f, AttackDuration: 0.22f));
		a.Units.Add(new UnitDef(
			Id: "brute", Tier: 2, Cost: 13, MaxHp: 130, Damage: 7.5f,
			AttackRate: 2.0f, Range: 55, Speed: 70,
			MovementClass: MovementClass.Ground, TargetingPolicy: TargetingPolicy.Frontmost,
			FormationSpacingMul: 1.05f, AttackDuration: 0.22f,
			Ability: AbilityType.Stun));
		a.Units.Add(new UnitDef(
			Id: "caster", Tier: 3, Cost: 24, MaxHp: 70, Damage: 14,
			AttackRate: 2.0f, Range: 150, Speed: 70,
			MovementClass: MovementClass.Ground, TargetingPolicy: TargetingPolicy.ClosestInRange,
			FormationSpacingMul: 1.15f, AttackDuration: 0.22f,
			Ability: AbilityType.OnDeathExplode, AbilityParam: 80f));
		a.Units.Add(new UnitDef(
			Id: "dragon", Tier: 4, Cost: 45, MaxHp: 260, Damage: 30,
			AttackRate: 1.5f, Range: 110, Speed: 80,
			MovementClass: MovementClass.Air, TargetingPolicy: TargetingPolicy.Frontmost,
			FormationSpacingMul: 1.45f, AttackDuration: 0.22f,
			Ability: AbilityType.Cleave, AbilityParam: 60f));
		return a;
	}

	public static ArmyDef Skirmishers()
	{
		// Swarm army: heavy T1 bias, but saves up for big T4 hits
		var a = new ArmyDef("Skirmishers", "skirmishers") { TierWeights = new[] { 9f, 3f, 2f, 1.5f } };
		a.Units.Add(new UnitDef(
			Id: "runner", Tier: 1, Cost: 6, MaxHp: 16, Damage: 5.5f,
			AttackRate: 2.2f, Range: 14, Speed: 105,
			MovementClass: MovementClass.Ground, TargetingPolicy: TargetingPolicy.Closest,
			FormationSpacingMul: 0.7f, WeaponLength: 14f, AttackDuration: 0.22f));
		a.Units.Add(new UnitDef(
			Id: "piker", Tier: 2, Cost: 12, MaxHp: 95, Damage: 8,
			AttackRate: 2.0f, Range: 55, Speed: 85,
			MovementClass: MovementClass.Ground, TargetingPolicy: TargetingPolicy.Frontmost,
			FormationSpacingMul: 0.85f, AttackDuration: 0.22f));
		a.Units.Add(new UnitDef(
			Id: "slinger", Tier: 3, Cost: 20, MaxHp: 70, Damage: 10,
			AttackRate: 2.1f, Range: 120, Speed: 80,
			MovementClass: MovementClass.Ground, TargetingPolicy: TargetingPolicy.ClosestInRange,
			FormationSpacingMul: 1.0f, AttackDuration: 0.22f));
		a.Units.Add(new UnitDef(
			Id: "beast", Tier: 4, Cost: 38, MaxHp: 260, Damage: 22,
			AttackRate: 1.7f, Range: 80, Speed: 70,
			MovementClass: MovementClass.Ground, TargetingPolicy: TargetingPolicy.Frontmost,
			FormationSpacingMul: 1.2f, AttackDuration: 0.22f,
			Ability: AbilityType.Cleave, AbilityParam: 45f));
		return a;
	}
}
