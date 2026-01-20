#nullable enable

namespace PixelArmies.SimCore;

public readonly record struct DamageEvent(
	int AttackerId,
	int TargetId,
	float Damage,
	bool IsRanged
);
