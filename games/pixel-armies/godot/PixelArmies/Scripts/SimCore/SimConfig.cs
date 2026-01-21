namespace PixelArmies.SimCore;

public sealed class SimConfig
{
    public const float FixedDt = 1f / 60f;

    // Battlefield
    public float BattlefieldLength = 3000f; // ~ "three screens" in abstract units
    public float BaseAttackRange = 40f;

    // Bases
    public float BaseMaxHp = 5000f;

    // Unit spacing
    public float UnitRadiusBase = 12f;
    public float UnitRadiusPerTier = 4f;

    // Spawning / escalation
    public float StartingPower = 10f;
    public float PowerGainPerSecond = 6f;     // escalation pacing knob
    public float SpawnTryInterval = 0.35f;    // how often we *attempt* to spawn
    public float Tier2Time = 20f;
    public float Tier3Time = 45f;
    public float Tier4Time = 80f;

    public float UnitRadiusForTier(int tier)
    {
        if (tier < 1) tier = 1;
        return UnitRadiusBase + (tier - 1) * UnitRadiusPerTier;
    }

    public int UnlockedTierForTime(float timeSeconds)
    {
        if (timeSeconds >= Tier4Time) return 4;
        if (timeSeconds >= Tier3Time) return 3;
        if (timeSeconds >= Tier2Time) return 2;
        return 1;
    }
}
