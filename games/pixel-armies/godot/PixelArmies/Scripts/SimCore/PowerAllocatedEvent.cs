#nullable enable

namespace PixelArmies.SimCore;

public readonly record struct PowerAllocatedEvent(
	Side Side,
	int Tier,
	float Amount
);
