#nullable enable

namespace PixelArmies.SimCore;

public readonly record struct UnitDiedEvent(
	int UnitId,
	int? KillerId
);
