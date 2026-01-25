#nullable enable

using Godot;
using PixelArmies.SimCore;
using SimSide = PixelArmies.SimCore.Side;

namespace PixelArmies.Presentation;

public static class StickUnitRenderer
{
	private const float TorsoLen = 14f;
	private const float LegLen = 10f;
	private const float ArmLen = 10f;
	private const float HeadRadius = 3f;
	private const float LineWidth = 2f;

	private const float LegSwingAmp = 0.6f;
	private const float BobAmp = 1.2f;
	private const float ArmSwingAmp = 0.25f;
	private const float BaseLegSpread = 0.35f;
	private static readonly Color ClubColor = new(0.35f, 0.22f, 0.10f);

	public static void Draw(
		CanvasItem canvas,
		Vector2 feetPos,
		int unitId,
		float moveSpeed,
		SimSide side,
		Color color,
		bool moving,
		float walkPhase,
		float attackPhase,
		float weaponLength)
	{
		float phaseOffset = (unitId % 17) * 0.37f;
		float phase = walkPhase + phaseOffset;

		float bobY = moving ? Mathf.Sin(phase * 2f) * BobAmp : 0f;
		float lean = attackPhase > 0f ? Smoothstep(attackPhase) * 1.5f : 0f;
		float dir = side == SimSide.Left ? 1f : -1f;
		var hip = feetPos + new Vector2(dir * lean, -LegLen - bobY);

		DrawTorso(canvas, hip, color);
		DrawHead(canvas, hip, color);
		DrawLegs(canvas, hip, color, phase);
		DrawArmAndWeapon(canvas, hip, color, side, phase, attackPhase, weaponLength);
	}

	private static void DrawTorso(CanvasItem canvas, Vector2 root, Color color)
	{
		var top = root + new Vector2(0f, -TorsoLen);
		canvas.DrawLine(root, top, color, LineWidth);
	}

	private static void DrawHead(CanvasItem canvas, Vector2 root, Color color)
	{
		var headCenter = root + new Vector2(0f, -TorsoLen - HeadRadius);
		canvas.DrawCircle(headCenter, HeadRadius, color);
	}

	private static void DrawLegs(CanvasItem canvas, Vector2 root, Color color, float phase)
	{
		float swing = Mathf.Sin(phase) * LegSwingAmp;
		float angleA = BaseLegSpread + swing;
		float angleB = -BaseLegSpread - swing;

		DrawLeg(canvas, root, color, angleA);
		DrawLeg(canvas, root, color, angleB);
	}

	private static void DrawLeg(CanvasItem canvas, Vector2 root, Color color, float angle)
	{
		var dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
		var end = root + dir * LegLen;
		canvas.DrawLine(root, end, color, LineWidth);
	}

	private static void DrawArmAndWeapon(
		CanvasItem canvas,
		Vector2 root,
		Color color,
		SimSide side,
		float phase,
		float attackPhase,
		float weaponLength)
	{
		float dir = side == SimSide.Left ? 1f : -1f;
		float swing = Mathf.Sin(phase + Mathf.Pi * 0.5f) * ArmSwingAmp;

		float restAngle = -Mathf.Pi * 0.5f;
		float startAngle = -0.6f;
		float endAngle = 0.95f;
		float attackAngle = restAngle;
		if (attackPhase > 0f)
		{
			float eased = Smoothstep(attackPhase);
			attackAngle = Mathf.Lerp(startAngle, endAngle, eased);
		}

		var basePos = root + new Vector2(0f, -TorsoLen * 0.7f);
		Vector2 armDir;
		Vector2 weaponDir;

		if (attackPhase > 0f)
		{
			float finalAngle = swing + attackAngle;
			armDir = new Vector2(Mathf.Cos(finalAngle) * dir, Mathf.Sin(finalAngle)).Normalized();
			weaponDir = armDir;
		}
		else
		{
			armDir = new Vector2(dir * Mathf.Cos(0.2f + swing), Mathf.Sin(0.2f + swing)).Normalized();
			weaponDir = new Vector2(0f, -1f);
		}

		var hand = basePos + armDir * ArmLen;

		float weaponLen = weaponLength > 0f ? weaponLength : ArmLen;
		var weaponTip = hand + weaponDir * weaponLen;

		canvas.DrawLine(basePos, hand, color, LineWidth);
		canvas.DrawLine(hand, weaponTip, ClubColor, LineWidth + 0.5f);
		canvas.DrawCircle(weaponTip, 1.6f, ClubColor);
	}

	private static float Smoothstep(float t)
	{
		t = Mathf.Clamp(t, 0f, 1f);
		return t * t * (3f - 2f * t);
	}
}
