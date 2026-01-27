#nullable enable

using System.Collections.Generic;
using Godot;

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
	public readonly Color WeaponColor;

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
		Color weaponColor)
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
		WeaponColor = weaponColor;
	}
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
	public StickVisualProfile StickProfile { get; }
	public StickDeathProfile StickDeathProfile { get; }

	private readonly Dictionary<string, StickVisualProfile> _stickOverrides = new();

	public ArmyVisualProfile(string id, StickVisualProfile stickProfile, StickDeathProfile stickDeathProfile)
	{
		Id = id;
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
}

public static class ArmyVisualProfiles
{
	private static readonly Dictionary<string, ArmyVisualProfile> Profiles = new()
	{
		{
			"legion",
			new ArmyVisualProfile(
				"legion",
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
					weaponColor: new Color(0.35f, 0.22f, 0.10f)),
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
					weaponColor: new Color(0.28f, 0.16f, 0.08f)),
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
					weaponColor: new Color(0.4f, 0.26f, 0.12f)),
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

	public static ArmyVisualProfile GetProfile(string? id)
	{
		if (id == null) return DefaultProfile;
		return Profiles.TryGetValue(id, out var profile) ? profile : DefaultProfile;
	}
}
