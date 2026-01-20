#nullable enable

using Godot;

namespace PixelArmies.Presentation;

public partial class Battlefield : Node2D
{
	[Export] public float BattlefieldLength { get; set; } = 2000f;
	[Export] public float GroundY { get; set; } = 120f;
	[Export] public float SkyHeight { get; set; } = 300f;
	[Export] public float GroundDepth { get; set; } = 200f;

	public override void _Ready()
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		DrawSky();
		DrawGround();
		DrawMarkers();
	}

	private void DrawSky()
	{
		float pad = 300f;
		float width = BattlefieldLength + pad * 2f;
		float top = GroundY - SkyHeight;

		var skyTop = new Color(0.20f, 0.42f, 0.70f);
		var skyBottom = new Color(0.62f, 0.78f, 0.92f);

		const int bands = 8;
		float bandH = SkyHeight / bands;

		for (int i = 0; i < bands; i++)
		{
			float t = bands == 1 ? 0f : i / (float)(bands - 1);
			var c = skyTop.Lerp(skyBottom, t);
			float y = top + i * bandH;
			DrawRect(new Rect2(-pad, y, width, bandH + 1f), c);
		}

		// Soft horizon glow
		DrawRect(new Rect2(-pad, GroundY - 6f, width, 12f), new Color(0.85f, 0.90f, 0.95f, 0.6f));
	}

	private void DrawGround()
	{
		float pad = 300f;
		float width = BattlefieldLength + pad * 2f;

		var groundTop = new Color(0.30f, 0.55f, 0.25f);
		var groundBottom = new Color(0.18f, 0.32f, 0.18f);

		const int bands = 5;
		float bandH = GroundDepth / bands;

		for (int i = 0; i < bands; i++)
		{
			float t = bands == 1 ? 0f : i / (float)(bands - 1);
			var c = groundTop.Lerp(groundBottom, t);
			float y = GroundY + i * bandH;
			DrawRect(new Rect2(-pad, y, width, bandH + 1f), c);
		}

		DrawLine(new Vector2(-pad, GroundY), new Vector2(BattlefieldLength + pad, GroundY), Colors.White, 2f);
	}

	private void DrawMarkers()
	{
		var postColor = new Color(0.95f, 0.95f, 0.95f, 0.5f);
		var stripeColor = new Color(0.10f, 0.20f, 0.10f, 0.35f);

		for (float x = 0f; x <= BattlefieldLength; x += 160f)
		{
			DrawLine(new Vector2(x, GroundY - 18f), new Vector2(x, GroundY + 18f), postColor, 2f);
		}

		for (float x = 0f; x <= BattlefieldLength; x += 80f)
		{
			DrawRect(new Rect2(x - 8f, GroundY + 6f, 16f, 6f), stripeColor);
		}
	}
}
