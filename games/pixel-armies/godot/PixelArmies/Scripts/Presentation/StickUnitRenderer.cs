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

	public static void Draw(
		CanvasItem canvas,
		Vector2 feetPos,
		int unitId,
		float moveSpeed,
		SimSide side,
		Color color,
		bool moving,
		float walkPhase)
	{
		float phaseOffset = (unitId % 17) * 0.37f;
		float phase = walkPhase + phaseOffset;

		float bobY = moving ? Mathf.Sin(phase * 2f) * BobAmp : 0f;
		var hip = feetPos + new Vector2(0f, -LegLen - bobY);

		DrawTorso(canvas, hip, color);
		DrawHead(canvas, hip, color);
		DrawLegs(canvas, hip, color, phase);
		DrawArm(canvas, hip, color, side, phase);
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

	private static void DrawArm(CanvasItem canvas, Vector2 root, Color color, SimSide side, float phase)
	{
		float dir = side == SimSide.Left ? 1f : -1f;
		float swing = Mathf.Sin(phase + Mathf.Pi * 0.5f) * ArmSwingAmp;
		var basePos = root + new Vector2(0f, -TorsoLen * 0.7f);
		var armDir = new Vector2(dir * Mathf.Cos(swing), -Mathf.Sin(swing));
		var end = basePos + armDir * ArmLen;
		canvas.DrawLine(basePos, end, color, LineWidth);
	}
}
