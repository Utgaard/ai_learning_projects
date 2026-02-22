#nullable enable

using Godot;
using PixelArmies.SimCore;
using SimSide = PixelArmies.SimCore.Side;

namespace PixelArmies.Presentation;

public static class StickUnitRenderer
{
	private const float BaseLegSpread = 0.35f;

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
		float weaponLength,
		StickVisualProfile style)
	{
		float phaseOffset = (unitId % 17) * 0.37f;
		float phase = walkPhase + phaseOffset;

		float bobY = moving ? Mathf.Sin(phase * 2f) * style.BobAmp : 0f;
		float lean = attackPhase > 0f ? Smoothstep(attackPhase) * style.AttackLean : 0f;
		float dir = side == SimSide.Left ? 1f : -1f;
		var hip = feetPos + new Vector2(dir * lean, -style.LegLen - bobY);

		DrawTorso(canvas, hip, color, style);
		DrawHead(canvas, hip, color, style);
		DrawLegs(canvas, hip, color, phase, style);
		DrawArmAndWeapon(canvas, hip, color, side, phase, attackPhase, weaponLength, style);
	}

	private static void DrawTorso(CanvasItem canvas, Vector2 root, Color color, StickVisualProfile style)
	{
		var top = root + new Vector2(0f, -style.TorsoLen);
		canvas.DrawLine(root, top, color, style.LineWidth);
	}

	private static void DrawHead(CanvasItem canvas, Vector2 root, Color color, StickVisualProfile style)
	{
		var headCenter = root + new Vector2(0f, -style.TorsoLen - style.HeadRadius);
		canvas.DrawCircle(headCenter, style.HeadRadius, color);
	}

	private static void DrawLegs(CanvasItem canvas, Vector2 root, Color color, float phase, StickVisualProfile style)
	{
		float swing = Mathf.Sin(phase) * style.LegSwingAmp;
		float angleA = BaseLegSpread + swing;
		float angleB = -BaseLegSpread - swing;

		DrawLeg(canvas, root, color, angleA, style);
		DrawLeg(canvas, root, color, angleB, style);
	}

	private static void DrawLeg(CanvasItem canvas, Vector2 root, Color color, float angle, StickVisualProfile style)
	{
		var dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
		var end = root + dir * style.LegLen;
		canvas.DrawLine(root, end, color, style.LineWidth);
	}

	private static void DrawArmAndWeapon(
		CanvasItem canvas,
		Vector2 root,
		Color color,
		SimSide side,
		float phase,
		float attackPhase,
		float weaponLength,
		StickVisualProfile style)
	{
		float weaponLen = weaponLength > 0f ? weaponLength : style.ArmLen;
		float dir = side == SimSide.Left ? 1f : -1f;
		float swing = Mathf.Sin(phase + Mathf.Pi * 0.5f) * style.ArmSwingAmp;

		float attackAngle = style.WeaponUprightAngle;
		if (attackPhase > 0f)
		{
			float eased = Smoothstep(attackPhase);
			attackAngle = Mathf.Lerp(style.AttackStartAngle, style.AttackEndAngle, eased);
		}

		var basePos = root + new Vector2(0f, -style.TorsoLen * 0.7f);
		Vector2 armDir;
		Vector2 weaponDir;

		if (style.WeaponMotion == StickWeaponMotion.Thrust)
		{
			if (attackPhase > 0f)
			{
				float t = Smoothstep(attackPhase);
				float spearAngle;
				float extend = 0f;

				// Spear-driven phases: rotate down -> thrust -> retract -> rotate up
				if (t < 0.25f)
				{
					float subT = Mathf.Clamp(t / 0.25f, 0f, 1f);
					spearAngle = Mathf.Lerp(style.CarryAngle, style.ThrustLowerAngle, subT);
				}
				else if (t < 0.55f)
				{
					float subT = Mathf.Clamp((t - 0.25f) / 0.3f, 0f, 1f);
					spearAngle = Mathf.Lerp(style.ThrustLowerAngle, style.ThrustForwardAngle, subT);
					extend = style.ThrustExtra * Smoothstep(subT);
				}
				else if (t < 0.75f)
				{
					float subT = Mathf.Clamp((t - 0.55f) / 0.2f, 0f, 1f);
					spearAngle = style.ThrustForwardAngle;
					extend = style.ThrustExtra * (1f - subT);
				}
				else
				{
					float subT = Mathf.Clamp((t - 0.75f) / 0.25f, 0f, 1f);
					spearAngle = Mathf.Lerp(style.ThrustForwardAngle, style.CarryAngle, subT);
				}

				weaponDir = new Vector2(dir * Mathf.Cos(spearAngle), Mathf.Sin(spearAngle)).Normalized();
				weaponLen += extend;

				// Arm follows spear base (hand anchored to spear axis).
				float armAngle = Mathf.Lerp(style.ArmForwardAngle, spearAngle, 0.6f);
				armDir = new Vector2(dir * Mathf.Cos(armAngle), Mathf.Sin(armAngle)).Normalized();
			}
			else
			{
				float sway = swing * 0.15f;
				float armAngle = style.ArmForwardAngle + sway;
				float carryAngle = style.CarryAngle + sway;
				armDir = new Vector2(dir * Mathf.Cos(armAngle), Mathf.Sin(armAngle)).Normalized();
				weaponDir = new Vector2(dir * Mathf.Cos(carryAngle), Mathf.Sin(carryAngle));
			}
		}
		else if (attackPhase > 0f)
		{
			float finalAngle = swing + attackAngle;
			armDir = new Vector2(Mathf.Cos(finalAngle) * dir, Mathf.Sin(finalAngle)).Normalized();
			weaponDir = armDir;
		}
		else
		{
			float armAngle = style.ArmForwardAngle + swing;
			armDir = new Vector2(dir * Mathf.Cos(armAngle), Mathf.Sin(armAngle)).Normalized();
			weaponDir = new Vector2(dir * Mathf.Cos(style.CarryAngle), Mathf.Sin(style.CarryAngle));
		}

		var hand = basePos + armDir * style.ArmLen;

		var weaponTip = hand + weaponDir * weaponLen;

		canvas.DrawLine(basePos, hand, color, style.LineWidth);
		canvas.DrawLine(hand, weaponTip, style.WeaponShaftColor, style.WeaponThickness);
		canvas.DrawCircle(weaponTip, style.WeaponTipRadius, style.WeaponTipColor);

		if (style.HasShield)
		{
			var shieldPos = basePos + new Vector2(dir * style.ShieldOffset.X, style.ShieldOffset.Y);
			var shieldColor = new Color(color.R * 0.7f, color.G * 0.7f, color.B * 0.7f, 1f);
			if (style.ShieldWidth > 0f && style.ShieldHeight > 0f)
			{
				var rect = new Rect2(
					shieldPos.X - style.ShieldWidth * 0.5f,
					shieldPos.Y - style.ShieldHeight * 0.5f,
					style.ShieldWidth,
					style.ShieldHeight);
				canvas.DrawRect(rect, shieldColor);
				if (style.ShieldOutlineWidth > 0f)
				{
					canvas.DrawRect(rect, style.ShieldOutlineColor, false, style.ShieldOutlineWidth);
				}
			}
			else
			{
				canvas.DrawCircle(shieldPos, style.ShieldRadius, shieldColor);
				if (style.ShieldOutlineWidth > 0f)
				{
					canvas.DrawArc(shieldPos, style.ShieldRadius, 0f, Mathf.Tau, 24, style.ShieldOutlineColor, style.ShieldOutlineWidth);
				}
			}
		}
	}

	private static float Smoothstep(float t)
	{
		t = Mathf.Clamp(t, 0f, 1f);
		return t * t * (3f - 2f * t);
	}
}
