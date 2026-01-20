using System;
using System.Collections.Generic;

namespace PixelArmies.SimCore;

public enum Side { Left, Right }

public readonly record struct UnitDef(
	string Id,
	int Tier,            // 1..4
	float Cost,          // "energy" cost
	float MaxHp,
	float Damage,
	float AttackRate,
	float Range,
	float Speed,
	bool IsAir,
	float FormationSpacingMul = 1f,
	int VanguardDepth = 0,
	float VanguardSpacingMul = 0f
);

public sealed class ArmyDef
{
	public string Name { get; }
	public List<UnitDef> Units { get; } = new();

	public ArmyDef(string name) => Name = name;

	public float MinCost()
	{
		float m = float.MaxValue;
		foreach (var u in Units) m = Math.Min(m, u.Cost);
		return m == float.MaxValue ? 999999f : m;
	}
}

public sealed class UnitState
{
	public int Id;
	public Side Side;
	public UnitDef Def;

	public float X;          // 1D lane position
	public float Hp;
	public float AttackCooldown;

	public bool Alive => Hp > 0f;

	public UnitState(int id, Side side, UnitDef def, float x)
	{
		Id = id;
		Side = side;
		Def = def;
		X = x;
		Hp = def.MaxHp;
		AttackCooldown = 0f;
	}
}
