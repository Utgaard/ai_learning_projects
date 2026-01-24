#nullable enable

using Godot;

namespace PixelArmies.Presentation;

public partial class GameplayBackdropUI : Control
{
	public enum BackdropMode
	{
		ProceduralGradient,
		Texture
	}

	[Export] public BackdropMode Mode { get; set; } = BackdropMode.ProceduralGradient;
	[Export] public Color SkyTop { get; set; } = new(0.20f, 0.42f, 0.70f);
	[Export] public Color SkyBottom { get; set; } = new(0.62f, 0.78f, 0.92f);
	[Export] public Color GroundBand { get; set; } = new(0.28f, 0.52f, 0.24f);
	[Export] public Color BelowGroundBand { get; set; } = new(0.14f, 0.26f, 0.14f);

	[Export(PropertyHint.Range, "0.05,0.5,0.01")]
	public float GroundBandHeightRatio { get; set; } = 0.22f;

	[Export(PropertyHint.Range, "0.05,0.6,0.01")]
	public float BelowBandHeightRatio { get; set; } = 0.18f;

	public override void _Ready()
	{
		ClipContents = true;
		QueueRedraw();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
		{
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if (Mode != BackdropMode.ProceduralGradient)
		{
			DrawRect(new Rect2(Vector2.Zero, Size), SkyBottom);
			return;
		}

		var size = Size;
		if (size.X <= 1f || size.Y <= 1f) return;

		DrawSkyGradient(size);
		DrawGroundBands(size);
	}

	private void DrawSkyGradient(Vector2 size)
	{
		const int bands = 4;
		float bandH = size.Y / bands;

		for (int i = 0; i < bands; i++)
		{
			float t = bands == 1 ? 0f : i / (float)(bands - 1);
			var c = SkyTop.Lerp(SkyBottom, t);
			float y = i * bandH;
			DrawRect(new Rect2(0f, y, size.X, bandH + 1f), c);
		}
	}

	private void DrawGroundBands(Vector2 size)
	{
		float groundBandH = size.Y * GroundBandHeightRatio;
		float belowBandH = size.Y * BelowBandHeightRatio;

		float belowY = size.Y - belowBandH;
		float groundY = belowY - groundBandH;

		if (groundBandH > 0f)
		{
			DrawRect(new Rect2(0f, groundY, size.X, groundBandH), GroundBand);
		}

		if (belowBandH > 0f)
		{
			DrawRect(new Rect2(0f, belowY, size.X, belowBandH), BelowGroundBand);
		}
	}
}
