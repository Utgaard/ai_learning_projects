#nullable enable

namespace PixelArmies.SimCore;

public readonly record struct UnitSpawnedEvent(
	Side Side,
	int Tier,
	int UnitId,
	string UnitDefId
);
