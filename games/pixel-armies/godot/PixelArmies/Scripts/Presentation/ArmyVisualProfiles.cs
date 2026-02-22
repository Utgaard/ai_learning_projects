#nullable enable

using System.Collections.Generic;
using Godot;
using PixelArmies.SimCore;

namespace PixelArmies.Presentation;

public readonly struct StickVisualProfile
{
	public readonly float TorsoLen;
	public readonly float LegLen;
	public readonly float ArmLen;
	public readonly float HeadRadius;
	public readonly float LineWidth;
	public readonly float LegSwingAmp;
	public readonly float BobAmp;
	public readonly float ArmSwingAmp;
	public readonly float ArmForwardAngle;
	public readonly float WeaponUprightAngle;
	public readonly float AttackStartAngle;
	public readonly float AttackEndAngle;
	public readonly float AttackLean;
	public readonly float WeaponThickness;
	public readonly float WeaponTipRadius;
	public readonly Color WeaponShaftColor;
	public readonly Color WeaponTipColor;
	public readonly StickWeaponMotion WeaponMotion;
	public readonly float ThrustLowerAngle;
	public readonly float ThrustForwardAngle;
	public readonly float ThrustExtra;
	public readonly float CarryAngle;
	public readonly bool HasShield;
	public readonly float ShieldRadius;
	public readonly Vector2 ShieldOffset;
	public readonly float ShieldWidth;
	public readonly float ShieldHeight;
	public readonly float ShieldOutlineWidth;
	public readonly Color ShieldOutlineColor;

	public StickVisualProfile(
		float torsoLen,
		float legLen,
		float armLen,
		float headRadius,
		float lineWidth,
		float legSwingAmp,
		float bobAmp,
		float armSwingAmp,
		float armForwardAngle,
		float weaponUprightAngle,
		float attackStartAngle,
		float attackEndAngle,
		float attackLean,
		float weaponThickness,
		float weaponTipRadius,
		Color weaponShaftColor,
		Color weaponTipColor,
		StickWeaponMotion weaponMotion,
		float thrustLowerAngle,
		float thrustForwardAngle,
		float thrustExtra,
		float carryAngle,
		bool hasShield,
		float shieldRadius,
		Vector2 shieldOffset,
		float shieldWidth,
		float shieldHeight,
		float shieldOutlineWidth,
		Color shieldOutlineColor)
	{
		TorsoLen = torsoLen;
		LegLen = legLen;
		ArmLen = armLen;
		HeadRadius = headRadius;
		LineWidth = lineWidth;
		LegSwingAmp = legSwingAmp;
		BobAmp = bobAmp;
		ArmSwingAmp = armSwingAmp;
		ArmForwardAngle = armForwardAngle;
		WeaponUprightAngle = weaponUprightAngle;
		AttackStartAngle = attackStartAngle;
		AttackEndAngle = attackEndAngle;
		AttackLean = attackLean;
		WeaponThickness = weaponThickness;
		WeaponTipRadius = weaponTipRadius;
		WeaponShaftColor = weaponShaftColor;
		WeaponTipColor = weaponTipColor;
		WeaponMotion = weaponMotion;
		ThrustLowerAngle = thrustLowerAngle;
		ThrustForwardAngle = thrustForwardAngle;
		ThrustExtra = thrustExtra;
		CarryAngle = carryAngle;
		HasShield = hasShield;
		ShieldRadius = shieldRadius;
		ShieldOffset = shieldOffset;
		ShieldWidth = shieldWidth;
		ShieldHeight = shieldHeight;
		ShieldOutlineWidth = shieldOutlineWidth;
		ShieldOutlineColor = shieldOutlineColor;
	}
}

public enum StickWeaponMotion
{
	Swing,
	Thrust
}

public readonly struct StickDeathProfile
{
	public readonly float FallDuration;
	public readonly float DeathDuration;
	public readonly float FallAngle;
	public readonly float FragmentLifetimeMin;
	public readonly float FragmentLifetimeMax;
	public readonly float FragmentSpeed;
	public readonly int FragmentCountMin;
	public readonly int FragmentCountMax;
	public readonly float FragmentLengthMin;
	public readonly float FragmentLengthMax;
	public readonly float SparkLifetimeMin;
	public readonly float SparkLifetimeMax;
	public readonly float SparkSpeed;
	public readonly int SparkCountMin;
	public readonly int SparkCountMax;
	public readonly float LineWidth;
	public readonly float TorsoLen;
	public readonly float LegLen;
	public readonly float HeadRadius;
	public readonly Color SparkColor;

	public StickDeathProfile(
		float fallDuration,
		float deathDuration,
		float fallAngle,
		float fragmentLifetimeMin,
		float fragmentLifetimeMax,
		float fragmentSpeed,
		int fragmentCountMin,
		int fragmentCountMax,
		float fragmentLengthMin,
		float fragmentLengthMax,
		float sparkLifetimeMin,
		float sparkLifetimeMax,
		float sparkSpeed,
		int sparkCountMin,
		int sparkCountMax,
		float lineWidth,
		float torsoLen,
		float legLen,
		float headRadius,
		Color sparkColor)
	{
		FallDuration = fallDuration;
		DeathDuration = deathDuration;
		FallAngle = fallAngle;
		FragmentLifetimeMin = fragmentLifetimeMin;
		FragmentLifetimeMax = fragmentLifetimeMax;
		FragmentSpeed = fragmentSpeed;
		FragmentCountMin = fragmentCountMin;
		FragmentCountMax = fragmentCountMax;
		FragmentLengthMin = fragmentLengthMin;
		FragmentLengthMax = fragmentLengthMax;
		SparkLifetimeMin = sparkLifetimeMin;
		SparkLifetimeMax = sparkLifetimeMax;
		SparkSpeed = sparkSpeed;
		SparkCountMin = sparkCountMin;
		SparkCountMax = sparkCountMax;
		LineWidth = lineWidth;
		TorsoLen = torsoLen;
		LegLen = legLen;
		HeadRadius = headRadius;
		SparkColor = sparkColor;
	}
}

public sealed class ArmyVisualProfile
{
	public string Id { get; }
	public Color PrimaryColor { get; }
	public StickVisualProfile StickProfile { get; }
	public StickDeathProfile StickDeathProfile { get; }

	private readonly Dictionary<string, StickVisualProfile> _stickOverrides = new();

	public ArmyVisualProfile(string id, Color primaryColor, StickVisualProfile stickProfile, StickDeathProfile stickDeathProfile)
	{
		Id = id;
		PrimaryColor = primaryColor;
		StickProfile = stickProfile;
		StickDeathProfile = stickDeathProfile;
	}

	public void AddStickOverride(string unitDefId, StickVisualProfile profile)
	{
		_stickOverrides[unitDefId] = profile;
	}

	public StickVisualProfile ResolveStickProfile(string unitDefId)
	{
		return _stickOverrides.TryGetValue(unitDefId, out var profile) ? profile : StickProfile;
	}

	public bool UseStickFor(UnitDef def)
	{
		return def.Tier == 1 || _stickOverrides.ContainsKey(def.Id);
	}
}

public static class ArmyVisualProfiles
{
	private static readonly Dictionary<string, ArmyVisualProfile> Profiles = new()
	{
		{
			"legion",
			new ArmyVisualProfile(
				"legion",
				new Color(0.35f, 0.55f, 0.90f),
				new StickVisualProfile(
					torsoLen: 16f,
					legLen: 10f,
					armLen: 9f,
					headRadius: 3f,
					lineWidth: 2.2f,
					legSwingAmp: 0.45f,
					bobAmp: 0.9f,
					armSwingAmp: 0.16f,
					armForwardAngle: 0.35f,
					weaponUprightAngle: -Mathf.Pi * 0.5f,
					attackStartAngle: -0.55f,
					attackEndAngle: 0.95f,
					attackLean: 1.2f,
					weaponThickness: 2.2f,
					weaponTipRadius: 1.6f,
					weaponShaftColor: new Color(0.35f, 0.22f, 0.10f),
					weaponTipColor: new Color(0.35f, 0.22f, 0.10f),
					weaponMotion: StickWeaponMotion.Swing,
					thrustLowerAngle: 0.4f,
					thrustForwardAngle: 0.05f,
					thrustExtra: 10f,
					carryAngle: -Mathf.Pi * 0.5f,
					hasShield: false,
					shieldRadius: 0f,
					shieldOffset: Vector2.Zero,
					shieldWidth: 0f,
					shieldHeight: 0f,
					shieldOutlineWidth: 0f,
					shieldOutlineColor: Colors.Transparent),
				new StickDeathProfile(
					fallDuration: 0.2f,
					deathDuration: 0.6f,
					fallAngle: 1.4f,
					fragmentLifetimeMin: 0.35f,
					fragmentLifetimeMax: 0.8f,
					fragmentSpeed: 90f,
					fragmentCountMin: 8,
					fragmentCountMax: 12,
					fragmentLengthMin: 6f,
					fragmentLengthMax: 11f,
					sparkLifetimeMin: 0.25f,
					sparkLifetimeMax: 0.5f,
					sparkSpeed: 170f,
					sparkCountMin: 10,
					sparkCountMax: 14,
					lineWidth: 2.2f,
					torsoLen: 16f,
					legLen: 10f,
					headRadius: 3f,
					sparkColor: new Color(0.95f, 0.25f, 0.2f)))
		},
		{
			"brutes",
			new ArmyVisualProfile(
				"brutes",
				new Color(0.85f, 0.18f, 0.18f),
				new StickVisualProfile(
					torsoLen: 13f,
					legLen: 9f,
					armLen: 9f,
					headRadius: 3.4f,
					lineWidth: 2.8f,
					legSwingAmp: 0.35f,
					bobAmp: 1.4f,
					armSwingAmp: 0.12f,
					armForwardAngle: 0.2f,
					weaponUprightAngle: -Mathf.Pi * 0.5f,
					attackStartAngle: -0.7f,
					attackEndAngle: 1.05f,
					attackLean: 1.6f,
					weaponThickness: 3.0f,
					weaponTipRadius: 2.1f,
					weaponShaftColor: new Color(0.28f, 0.16f, 0.08f),
					weaponTipColor: new Color(0.28f, 0.16f, 0.08f),
					weaponMotion: StickWeaponMotion.Swing,
					thrustLowerAngle: 0.45f,
					thrustForwardAngle: 0.08f,
					thrustExtra: 12f,
					carryAngle: -Mathf.Pi * 0.5f,
					hasShield: false,
					shieldRadius: 0f,
					shieldOffset: Vector2.Zero,
					shieldWidth: 0f,
					shieldHeight: 0f,
					shieldOutlineWidth: 0f,
					shieldOutlineColor: Colors.Transparent),
				new StickDeathProfile(
					fallDuration: 0.24f,
					deathDuration: 0.75f,
					fallAngle: 1.7f,
					fragmentLifetimeMin: 0.45f,
					fragmentLifetimeMax: 0.95f,
					fragmentSpeed: 110f,
					fragmentCountMin: 10,
					fragmentCountMax: 16,
					fragmentLengthMin: 7f,
					fragmentLengthMax: 13f,
					sparkLifetimeMin: 0.3f,
					sparkLifetimeMax: 0.6f,
					sparkSpeed: 200f,
					sparkCountMin: 12,
					sparkCountMax: 18,
					lineWidth: 2.8f,
					torsoLen: 13f,
					legLen: 9f,
					headRadius: 3.4f,
					sparkColor: new Color(0.9f, 0.15f, 0.15f)))
		},
		{
			"skirmishers",
			new ArmyVisualProfile(
				"skirmishers",
				new Color(0.72f, 0.62f, 0.18f),
				new StickVisualProfile(
					torsoLen: 14f,
					legLen: 12f,
					armLen: 10f,
					headRadius: 2.6f,
					lineWidth: 1.8f,
					legSwingAmp: 0.7f,
					bobAmp: 1.1f,
					armSwingAmp: 0.22f,
					armForwardAngle: 0.45f,
					weaponUprightAngle: -Mathf.Pi * 0.5f,
					attackStartAngle: -0.5f,
					attackEndAngle: 0.9f,
					attackLean: 1.0f,
					weaponThickness: 1.8f,
					weaponTipRadius: 1.4f,
					weaponShaftColor: new Color(0.4f, 0.26f, 0.12f),
					weaponTipColor: new Color(0.4f, 0.26f, 0.12f),
					weaponMotion: StickWeaponMotion.Swing,
					thrustLowerAngle: 0.35f,
					thrustForwardAngle: 0.02f,
					thrustExtra: 8f,
					carryAngle: -Mathf.Pi * 0.5f,
					hasShield: false,
					shieldRadius: 0f,
					shieldOffset: Vector2.Zero,
					shieldWidth: 0f,
					shieldHeight: 0f,
					shieldOutlineWidth: 0f,
					shieldOutlineColor: Colors.Transparent),
				new StickDeathProfile(
					fallDuration: 0.18f,
					deathDuration: 0.55f,
					fallAngle: 1.2f,
					fragmentLifetimeMin: 0.3f,
					fragmentLifetimeMax: 0.7f,
					fragmentSpeed: 80f,
					fragmentCountMin: 7,
					fragmentCountMax: 11,
					fragmentLengthMin: 5f,
					fragmentLengthMax: 10f,
					sparkLifetimeMin: 0.22f,
					sparkLifetimeMax: 0.45f,
					sparkSpeed: 160f,
					sparkCountMin: 9,
					sparkCountMax: 13,
					lineWidth: 1.8f,
					torsoLen: 14f,
					legLen: 12f,
					headRadius: 2.6f,
					sparkColor: new Color(0.95f, 0.25f, 0.2f)))
		}
	};

	private static readonly ArmyVisualProfile DefaultProfile = Profiles["legion"];

	static ArmyVisualProfiles()
	{
		var legion      = Profiles["legion"];
		var brutes      = Profiles["brutes"];
		var skirmishers = Profiles["skirmishers"];

		// ── Legion ──────────────────────────────────────────────────────────────

		legion.AddStickOverride("spearman",
			new StickVisualProfile(
				torsoLen: 19f,
				legLen: 12f,
				armLen: 11f,
				headRadius: 3f,
				lineWidth: 2.2f,
				legSwingAmp: 0.2f,
				bobAmp: 0.6f,
				armSwingAmp: 0.04f,
				armForwardAngle: 0.2f,
				weaponUprightAngle: -Mathf.Pi * 0.5f,
				attackStartAngle: -0.4f,
				attackEndAngle: 0.2f,
				attackLean: 1.4f,
				weaponThickness: 2.2f,
				weaponTipRadius: 1.8f,
				weaponShaftColor: new Color(0.34f, 0.20f, 0.08f),
				weaponTipColor: new Color(0.88f, 0.90f, 0.94f),
				weaponMotion: StickWeaponMotion.Thrust,
				thrustLowerAngle: 0.15f,
				thrustForwardAngle: 0.35f,
				thrustExtra: 24f,
				carryAngle: -1.05f,
				hasShield: true,
				shieldRadius: 0f,
				shieldOffset: new Vector2(12f, -6f),
				shieldWidth: 4.5f,
				shieldHeight: 12f,
				shieldOutlineWidth: 1.8f,
				shieldOutlineColor: new Color(0.05f, 0.05f, 0.05f, 1f)));

		// Archer — slender, athletic, bow draw & release (Air unit)
		legion.AddStickOverride("archer",
			new StickVisualProfile(
				torsoLen: 14f,
				legLen: 13f,
				armLen: 11f,
				headRadius: 2.5f,
				lineWidth: 1.8f,
				legSwingAmp: 0.55f,
				bobAmp: 1.0f,
				armSwingAmp: 0.20f,
				armForwardAngle: 0.30f,
				weaponUprightAngle: -Mathf.Pi * 0.5f,
				attackStartAngle: -0.9f,
				attackEndAngle: 0.5f,
				attackLean: 1.0f,
				weaponThickness: 1.8f,
				weaponTipRadius: 1.2f,
				weaponShaftColor: new Color(0.36f, 0.22f, 0.09f),
				weaponTipColor: new Color(0.85f, 0.85f, 0.80f),
				weaponMotion: StickWeaponMotion.Swing,
				thrustLowerAngle: 0.35f,
				thrustForwardAngle: 0.05f,
				thrustExtra: 8f,
				carryAngle: -0.8f,
				hasShield: false,
				shieldRadius: 0f,
				shieldOffset: Vector2.Zero,
				shieldWidth: 0f,
				shieldHeight: 0f,
				shieldOutlineWidth: 0f,
				shieldOutlineColor: Colors.Transparent));

		// Ogre — hulking brute, massive overhead club swing (T4)
		legion.AddStickOverride("ogre",
			new StickVisualProfile(
				torsoLen: 24f,
				legLen: 14f,
				armLen: 14f,
				headRadius: 5.5f,
				lineWidth: 4.5f,
				legSwingAmp: 0.25f,
				bobAmp: 2.0f,
				armSwingAmp: 0.10f,
				armForwardAngle: 0.15f,
				weaponUprightAngle: -Mathf.Pi * 0.5f,
				attackStartAngle: -1.0f,
				attackEndAngle: 1.4f,
				attackLean: 2.8f,
				weaponThickness: 5.0f,
				weaponTipRadius: 4.5f,
				weaponShaftColor: new Color(0.22f, 0.13f, 0.05f),
				weaponTipColor: new Color(0.28f, 0.18f, 0.08f),
				weaponMotion: StickWeaponMotion.Swing,
				thrustLowerAngle: 0.5f,
				thrustForwardAngle: 0.1f,
				thrustExtra: 10f,
				carryAngle: -Mathf.Pi * 0.5f,
				hasShield: false,
				shieldRadius: 0f,
				shieldOffset: Vector2.Zero,
				shieldWidth: 0f,
				shieldHeight: 0f,
				shieldOutlineWidth: 0f,
				shieldOutlineColor: Colors.Transparent));

		// ── Brutes ──────────────────────────────────────────────────────────────

		// Brute — squat, powerful axe chop (T2)
		brutes.AddStickOverride("brute",
			new StickVisualProfile(
				torsoLen: 11f,
				legLen: 8f,
				armLen: 11f,
				headRadius: 4.2f,
				lineWidth: 3.2f,
				legSwingAmp: 0.40f,
				bobAmp: 1.4f,
				armSwingAmp: 0.14f,
				armForwardAngle: 0.25f,
				weaponUprightAngle: -Mathf.Pi * 0.5f,
				attackStartAngle: -0.8f,
				attackEndAngle: 1.3f,
				attackLean: 2.0f,
				weaponThickness: 3.2f,
				weaponTipRadius: 3.5f,
				weaponShaftColor: new Color(0.22f, 0.13f, 0.05f),
				weaponTipColor: new Color(0.72f, 0.72f, 0.74f),
				weaponMotion: StickWeaponMotion.Swing,
				thrustLowerAngle: 0.45f,
				thrustForwardAngle: 0.08f,
				thrustExtra: 10f,
				carryAngle: -Mathf.Pi * 0.5f,
				hasShield: false,
				shieldRadius: 0f,
				shieldOffset: Vector2.Zero,
				shieldWidth: 0f,
				shieldHeight: 0f,
				shieldOutlineWidth: 0f,
				shieldOutlineColor: Colors.Transparent));

		// Caster — tall, spindly, gliding walk, wide staff sweep + glowing orb (T3)
		brutes.AddStickOverride("caster",
			new StickVisualProfile(
				torsoLen: 20f,
				legLen: 11f,
				armLen: 13f,
				headRadius: 3.0f,
				lineWidth: 1.6f,
				legSwingAmp: 0.15f,
				bobAmp: 0.35f,
				armSwingAmp: 0.28f,
				armForwardAngle: 0.40f,
				weaponUprightAngle: -Mathf.Pi * 0.5f,
				attackStartAngle: -1.1f,
				attackEndAngle: 0.6f,
				attackLean: 0.7f,
				weaponThickness: 1.8f,
				weaponTipRadius: 4.0f,
				weaponShaftColor: new Color(0.28f, 0.10f, 0.36f),
				weaponTipColor: new Color(0.78f, 0.30f, 1.00f),
				weaponMotion: StickWeaponMotion.Swing,
				thrustLowerAngle: 0.35f,
				thrustForwardAngle: 0.05f,
				thrustExtra: 6f,
				carryAngle: -0.5f,
				hasShield: false,
				shieldRadius: 0f,
				shieldOffset: Vector2.Zero,
				shieldWidth: 0f,
				shieldHeight: 0f,
				shieldOutlineWidth: 0f,
				shieldOutlineColor: Colors.Transparent));

		// Dragon — enormous flying beast, arm-thrust = fire-breath lunge (T4, Air)
		brutes.AddStickOverride("dragon",
			new StickVisualProfile(
				torsoLen: 22f,
				legLen: 10f,
				armLen: 18f,
				headRadius: 7.0f,
				lineWidth: 5.5f,
				legSwingAmp: 0.20f,
				bobAmp: 1.6f,
				armSwingAmp: 0.12f,
				armForwardAngle: 0.15f,
				weaponUprightAngle: -Mathf.Pi * 0.5f,
				attackStartAngle: -0.5f,
				attackEndAngle: 0.3f,
				attackLean: 1.6f,
				weaponThickness: 5.5f,
				weaponTipRadius: 5.0f,
				weaponShaftColor: new Color(0.55f, 0.08f, 0.08f),
				weaponTipColor: new Color(1.00f, 0.52f, 0.05f),
				weaponMotion: StickWeaponMotion.Thrust,
				thrustLowerAngle: 0.10f,
				thrustForwardAngle: 0.25f,
				thrustExtra: 20f,
				carryAngle: -0.6f,
				hasShield: false,
				shieldRadius: 0f,
				shieldOffset: Vector2.Zero,
				shieldWidth: 0f,
				shieldHeight: 0f,
				shieldOutlineWidth: 0f,
				shieldOutlineColor: Colors.Transparent));

		// ── Skirmishers ─────────────────────────────────────────────────────────

		// Piker — light, long-legged, pike thrust like spearman but slimmer (T2)
		skirmishers.AddStickOverride("piker",
			new StickVisualProfile(
				torsoLen: 16f,
				legLen: 13f,
				armLen: 10f,
				headRadius: 2.8f,
				lineWidth: 2.0f,
				legSwingAmp: 0.60f,
				bobAmp: 1.1f,
				armSwingAmp: 0.08f,
				armForwardAngle: 0.25f,
				weaponUprightAngle: -Mathf.Pi * 0.5f,
				attackStartAngle: -0.4f,
				attackEndAngle: 0.25f,
				attackLean: 1.2f,
				weaponThickness: 2.0f,
				weaponTipRadius: 1.6f,
				weaponShaftColor: new Color(0.42f, 0.28f, 0.12f),
				weaponTipColor: new Color(0.82f, 0.84f, 0.88f),
				weaponMotion: StickWeaponMotion.Thrust,
				thrustLowerAngle: 0.15f,
				thrustForwardAngle: 0.30f,
				thrustExtra: 18f,
				carryAngle: -1.05f,
				hasShield: false,
				shieldRadius: 0f,
				shieldOffset: Vector2.Zero,
				shieldWidth: 0f,
				shieldHeight: 0f,
				shieldOutlineWidth: 0f,
				shieldOutlineColor: Colors.Transparent));

		// Slinger — fast overhead-windmill stone throw (T3)
		skirmishers.AddStickOverride("slinger",
			new StickVisualProfile(
				torsoLen: 14f,
				legLen: 12f,
				armLen: 10f,
				headRadius: 2.6f,
				lineWidth: 1.8f,
				legSwingAmp: 0.60f,
				bobAmp: 1.0f,
				armSwingAmp: 0.30f,
				armForwardAngle: 0.45f,
				weaponUprightAngle: -Mathf.Pi * 0.5f,
				attackStartAngle: -1.3f,
				attackEndAngle: 1.0f,
				attackLean: 1.2f,
				weaponThickness: 1.6f,
				weaponTipRadius: 3.0f,
				weaponShaftColor: new Color(0.52f, 0.38f, 0.18f),
				weaponTipColor: new Color(0.58f, 0.58f, 0.60f),
				weaponMotion: StickWeaponMotion.Swing,
				thrustLowerAngle: 0.35f,
				thrustForwardAngle: 0.05f,
				thrustExtra: 6f,
				carryAngle: -0.5f,
				hasShield: false,
				shieldRadius: 0f,
				shieldOffset: Vector2.Zero,
				shieldWidth: 0f,
				shieldHeight: 0f,
				shieldOutlineWidth: 0f,
				shieldOutlineColor: Colors.Transparent));

		// Beast — huge predatory animal, bounding gait, claw swipe (T4)
		skirmishers.AddStickOverride("beast",
			new StickVisualProfile(
				torsoLen: 22f,
				legLen: 15f,
				armLen: 13f,
				headRadius: 5.0f,
				lineWidth: 3.8f,
				legSwingAmp: 0.45f,
				bobAmp: 2.2f,
				armSwingAmp: 0.18f,
				armForwardAngle: 0.30f,
				weaponUprightAngle: -Mathf.Pi * 0.5f,
				attackStartAngle: -0.7f,
				attackEndAngle: 1.3f,
				attackLean: 2.2f,
				weaponThickness: 3.8f,
				weaponTipRadius: 3.5f,
				weaponShaftColor: new Color(0.72f, 0.62f, 0.18f),
				weaponTipColor: new Color(0.92f, 0.90f, 0.82f),
				weaponMotion: StickWeaponMotion.Swing,
				thrustLowerAngle: 0.40f,
				thrustForwardAngle: 0.08f,
				thrustExtra: 8f,
				carryAngle: -Mathf.Pi * 0.5f,
				hasShield: false,
				shieldRadius: 0f,
				shieldOffset: Vector2.Zero,
				shieldWidth: 0f,
				shieldHeight: 0f,
				shieldOutlineWidth: 0f,
				shieldOutlineColor: Colors.Transparent));
	}

	public static ArmyVisualProfile GetProfile(string? id)
	{
		if (id == null) return DefaultProfile;
		return Profiles.TryGetValue(id, out var profile) ? profile : DefaultProfile;
	}
}
