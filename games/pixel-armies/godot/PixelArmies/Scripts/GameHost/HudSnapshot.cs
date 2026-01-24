#nullable enable

namespace PixelArmies.GameHost;

public sealed class HudSnapshot
{
	public HudSideSnapshot Left { get; } = new();
	public HudSideSnapshot Right { get; } = new();
}

public sealed class HudSideSnapshot
{
	public float BaseHp;
	public int UnlockedTier;
	public int Kills;
	public float DamageDealt;
	public float PowerPool;
	public int CurrentBucketTier;
	public float CurrentBucketProgress;
	public float CurrentTargetCost;
}
