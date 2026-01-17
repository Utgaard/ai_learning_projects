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
}
