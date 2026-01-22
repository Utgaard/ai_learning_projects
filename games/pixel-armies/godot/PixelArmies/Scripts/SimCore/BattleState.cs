using System;
using System.Collections.Generic;

namespace PixelArmies.SimCore;

public sealed class BattleState
{
    public float Time;

    public float LeftBaseHp;
    public float RightBaseHp;

    public readonly List<UnitState> Units = new();
    public int NextUnitId = 1;

    public BattleState(float baseHp)
    {
        LeftBaseHp = baseHp;
        RightBaseHp = baseHp;
    }

    public bool IsOver => LeftBaseHp <= 0f || RightBaseHp <= 0f;

    public Side Winner
    {
        get
        {
            if (LeftBaseHp <= 0f && RightBaseHp <= 0f) return Side.Left; // tie-break arbitrary
            if (RightBaseHp <= 0f) return Side.Left;
            return Side.Right;
        }
    }

    public DebugSnapshot BuildDebugSnapshot()
    {
        int l1 = 0, l2 = 0, l3 = 0, l4 = 0;
        int r1 = 0, r2 = 0, r3 = 0, r4 = 0;
        int lTotal = 0;
        int rTotal = 0;

        for (int i = 0; i < Units.Count; i++)
        {
            var u = Units[i];
            if (!u.Alive) continue;

            if (u.Side == Side.Left)
            {
                lTotal++;
                switch (u.Def.Tier)
                {
                    case 1: l1++; break;
                    case 2: l2++; break;
                    case 3: l3++; break;
                    case 4: l4++; break;
                }
            }
            else
            {
                rTotal++;
                switch (u.Def.Tier)
                {
                    case 1: r1++; break;
                    case 2: r2++; break;
                    case 3: r3++; break;
                    case 4: r4++; break;
                }
            }
        }

        return new DebugSnapshot(
            LeftBaseHp,
            RightBaseHp,
            lTotal,
            rTotal,
            l1, l2, l3, l4,
            r1, r2, r3, r4);
    }
}
