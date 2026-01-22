#nullable enable

namespace PixelArmies.SimCore;

public readonly record struct DebugSnapshot(
    float LeftBaseHp,
    float RightBaseHp,
    int LeftTotal,
    int RightTotal,
    int L1,
    int L2,
    int L3,
    int L4,
    int R1,
    int R2,
    int R3,
    int R4
);
