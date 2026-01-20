#nullable enable

using Godot;
using PixelArmies.SimCore;
using SimSide = PixelArmies.SimCore.Side;

namespace PixelArmies.Presentation;

public partial class BattleView : Node2D
{
	private BattleSimulator? _sim;
	private SimConfig? _cfg;

	public float GroundY { get; private set; } = 120f;

	public void Configure(BattleSimulator sim, SimConfig cfg, float groundY)
	{
		_sim = sim;
		_cfg = cfg;
		GroundY = groundY;
	}

	public override void _Draw()
	{
		if (_sim == null || _cfg == null) return;

		// Simple coordinate system:
		// X = sim X
		// Y = 0 is ground line; units drawn above it

		// Bases
		DrawRect(new Rect2(-30, GroundY - 60, 30, 60), Colors.White);
		DrawRect(new Rect2(_cfg.BattlefieldLength, GroundY - 60, 30, 60), Colors.White);

		// Base HP bars (simple)
		float barW = 120f;
		float barH = 10f;
		float lPct = Mathf.Clamp(_sim.State.LeftBaseHp / _cfg.BaseMaxHp, 0f, 1f);
		float rPct = Mathf.Clamp(_sim.State.RightBaseHp / _cfg.BaseMaxHp, 0f, 1f);

		DrawRect(new Rect2(10, GroundY + 20, barW, barH), Colors.Black);
		DrawRect(new Rect2(10, GroundY + 20, barW * lPct, barH), Colors.White);

		DrawRect(new Rect2(_cfg.BattlefieldLength - barW - 10, GroundY + 20, barW, barH), Colors.Black);
		DrawRect(new Rect2(_cfg.BattlefieldLength - barW - 10, GroundY + 20, barW * rPct, barH), Colors.White);

		// Units as rectangles
		var font = ThemeDB.FallbackFont;
		const int tierFontSize = 10;

		foreach (var u in _sim.State.Units)
		{
			float y = GroundY - 12;
			float h = 12;
			float w = 10;

			// Make big units bigger (tier proxy)
			w += (u.Def.Tier - 1) * 4;
			h += (u.Def.Tier - 1) * 4;

			if (u.Def.IsAir)
				y -= 40f; // air height

			// Left units slightly different than right
			var c = u.Side == SimSide.Left ? Colors.Cyan : Colors.Orange;

			var rect = new Rect2(u.X - w * 0.5f, y - h, w, h);
			float outline = 1f + (u.Def.Tier - 1) * 1.2f;

			DrawRect(rect, c);
			DrawRect(rect, Colors.Black, false, outline);

			string label = u.Def.Tier.ToString();
			var labelSize = font.GetStringSize(label, fontSize: tierFontSize);
			var labelPos = new Vector2(u.X - labelSize.X * 0.5f, y - h - 4f);
			DrawString(font, labelPos, label, fontSize: tierFontSize, modulate: c);
		}

		// End text (minimal)
		if (_sim.State.IsOver)
		{
			var winner = _sim.State.Winner == SimSide.Left ? "LEFT WINS" : "RIGHT WINS";
			DrawString(font, new Vector2(20, 40), winner, fontSize: 32, modulate: Colors.White);
		}
	}
}
