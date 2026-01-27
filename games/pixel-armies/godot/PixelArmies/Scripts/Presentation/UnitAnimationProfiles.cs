#nullable enable

using Godot;
using PixelArmies.SimCore;
using SimSide = PixelArmies.SimCore.Side;

namespace PixelArmies.Presentation;

public readonly struct UnitDrawContext
{
	public CanvasItem Canvas { get; }
	public Font Font { get; }
	public int TierFontSize { get; }

	public UnitDrawContext(CanvasItem canvas, Font font, int tierFontSize)
	{
		Canvas = canvas;
		Font = font;
		TierFontSize = tierFontSize;
	}
}

public readonly struct UnitDrawData
{
	public Vector2 Center { get; }
	public float Width { get; }
	public float Height { get; }
	public Color Color { get; }
	public SimSide Side { get; }
	public bool Moving { get; }
	public float WalkPhase { get; }
	public float AttackPhase { get; }
	public float WeaponLength { get; }
	public float FlashAlpha { get; }
	public StickVisualProfile StickStyle { get; }

	public UnitDrawData(
		Vector2 center,
		float width,
		float height,
		Color color,
		SimSide side,
		bool moving,
		float walkPhase,
		float attackPhase,
		float weaponLength,
		float flashAlpha,
		StickVisualProfile stickStyle)
	{
		Center = center;
		Width = width;
		Height = height;
		Color = color;
		Side = side;
		Moving = moving;
		WalkPhase = walkPhase;
		AttackPhase = attackPhase;
		WeaponLength = weaponLength;
		FlashAlpha = flashAlpha;
		StickStyle = stickStyle;
	}
}

public readonly struct UnitDeathInfo
{
	public int UnitId { get; }
	public Vector2 Center { get; }
	public Vector2 Feet { get; }
	public BattleView.UnitVisual Visual { get; }
	public SimSide Side { get; }
	public float WeaponLength { get; }
	public StickDeathProfile StickDeathStyle { get; }

	public UnitDeathInfo(
		int unitId,
		Vector2 center,
		Vector2 feet,
		BattleView.UnitVisual visual,
		SimSide side,
		float weaponLength,
		StickDeathProfile stickDeathStyle)
	{
		UnitId = unitId;
		Center = center;
		Feet = feet;
		Visual = visual;
		Side = side;
		WeaponLength = weaponLength;
		StickDeathStyle = stickDeathStyle;
	}
}

public interface IUnitAnimationProfile
{
	bool Applies(UnitDef def);
	void DrawUnit(UnitDrawContext context, UnitState unit, UnitDrawData data);
	void OnDeath(UnitDeathInfo info);
	void Update(float dt);
	void DrawOverlay(CanvasItem canvas);
}

public sealed class DefaultRectProfile : IUnitAnimationProfile
{
	private readonly DeathEffectSystem _deathEffects = new();

	public bool Applies(UnitDef def) => def.Tier != 1;

	public void DrawUnit(UnitDrawContext context, UnitState unit, UnitDrawData data)
	{
		var rect = new Rect2(data.Center.X - data.Width * 0.5f, data.Center.Y - data.Height * 0.5f, data.Width, data.Height);
		float outline = 1f + (unit.Def.Tier - 1) * 1.2f;

		context.Canvas.DrawRect(rect, data.Color);
		context.Canvas.DrawRect(rect, Colors.Black, false, outline);

		if (data.FlashAlpha > 0f)
		{
			var flashColor = new Color(1f, 1f, 1f, data.FlashAlpha);
			context.Canvas.DrawRect(rect, flashColor);
		}

		string label = unit.Def.Tier.ToString();
		var labelSize = context.Font.GetStringSize(label, fontSize: context.TierFontSize);
		var labelPos = new Vector2(data.Center.X - labelSize.X * 0.5f, data.Center.Y - data.Height * 0.5f - 4f);
		context.Canvas.DrawString(context.Font, labelPos, label, fontSize: context.TierFontSize, modulate: data.Color);
	}

	public void OnDeath(UnitDeathInfo info)
	{
		_deathEffects.AddDeath(info.UnitId, info.Center, info.Visual);
	}

	public void Update(float dt)
	{
		_deathEffects.Update(dt);
	}

	public void DrawOverlay(CanvasItem canvas)
	{
		_deathEffects.Draw(canvas);
	}
}

public sealed class StickTier1Profile : IUnitAnimationProfile
{
	private readonly StickDeathEffectSystem _deathEffects = new();

	public bool Applies(UnitDef def) => def.Tier == 1;

	public void DrawUnit(UnitDrawContext context, UnitState unit, UnitDrawData data)
	{
		var feet = new Vector2(data.Center.X, data.Center.Y + data.Height * 0.5f);
		float weaponLength = data.WeaponLength > 0f ? data.WeaponLength : 14f;
		StickUnitRenderer.Draw(
			context.Canvas,
			feet,
			unit.Id,
			unit.Def.Speed,
			unit.Side,
			data.Color,
			data.Moving,
			data.WalkPhase,
			data.AttackPhase,
			weaponLength,
			data.StickStyle);
	}

	public void OnDeath(UnitDeathInfo info)
	{
		_deathEffects.AddDeath(info.Feet, info.Side, info.Visual.Color, info.StickDeathStyle);
	}

	public void Update(float dt)
	{
		_deathEffects.Update(dt);
	}

	public void DrawOverlay(CanvasItem canvas)
	{
		_deathEffects.Draw(canvas);
	}
}
