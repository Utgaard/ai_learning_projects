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

    public float UnitRadiusForTier(int tier)
    {
        if (tier < 1) tier = 1;
        return UnitRadiusBase + (tier - 1) * UnitRadiusPerTier;
    }
}
