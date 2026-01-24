#nullable enable

using Godot;

namespace PixelArmies.Presentation;

public partial class BattlefieldBackdrop : Node2D
{
	private const float SafetyMargin = 50f;
	[Export] public float GroundY { get; set; } = 120f;
	[Export] public float GroundBandHeight { get; set; } = 40f;
	[Export] public Color SkyTop { get; set; } = new(0.20f, 0.42f, 0.70f);
	[Export] public Color SkyBottom { get; set; } = new(0.62f, 0.78f, 0.92f);
	[Export] public Color GroundSurface { get; set; } = new(0.30f, 0.55f, 0.25f);
	[Export] public Color Subsurface { get; set; } = new(0.18f, 0.32f, 0.18f);
	[Export] public Color HorizonColor { get; set; } = new(0.95f, 0.95f, 0.95f, 0.4f);

	private Camera2D? _camera;

	public void AttachCamera(Camera2D camera)
	{
		_camera = camera;
	}

	public override void _Process(double delta)
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		var cam = _camera ?? GetViewport().GetCamera2D();
		if (cam == null) return;

		var viewport = GetViewport();
		var viewportSize = viewport.GetVisibleRect().Size;
		var zoom = cam.Zoom;
		float zoomX = Mathf.Abs(zoom.X) < 0.0001f ? 1f : zoom.X;
		float zoomY = Mathf.Abs(zoom.Y) < 0.0001f ? 1f : zoom.Y;

		// In Godot, smaller zoom values show more world (zoomed out),
		// so visible world size is viewport / zoom.
		float halfW = viewportSize.X * (1f / zoomX) * 0.5f;
		float halfH = viewportSize.Y * (1f / zoomY) * 0.5f;
		var center = cam.GlobalPosition;

		float left = center.X - halfW - SafetyMargin;
		float right = center.X + halfW + SafetyMargin;
		float top = center.Y - halfH - SafetyMargin;
		float bottom = center.Y + halfH + SafetyMargin;

		DrawSkyBands(left, right, top, GroundY);
		DrawGroundBands(left, right, bottom);
		DrawLine(new Vector2(left, GroundY), new Vector2(right, GroundY), HorizonColor, 2f);
	}

	private void DrawSkyBands(float left, float right, float top, float groundY)
	{
		float height = groundY - top;
		if (height <= 0f) return;

		const int bands = 8;
		float bandH = height / bands;
		float width = right - left;

		for (int i = 0; i < bands; i++)
		{
			float y = top + i * bandH;
			float t = height <= 0f ? 0f : (y - top) / height;
			var color = SkyTop.Lerp(SkyBottom, t);
			DrawRect(new Rect2(left, y, width, bandH + 1f), color);
		}
	}

	private void DrawGroundBands(float left, float right, float bottom)
	{
		float width = right - left;
		float surfaceH = GroundBandHeight;
		if (surfaceH > 0f)
		{
			DrawRect(new Rect2(left, GroundY, width, surfaceH), GroundSurface);
		}

		float subsurfaceTop = GroundY + surfaceH;
		float subsurfaceH = bottom - subsurfaceTop;
		if (subsurfaceH > 0f)
		{
			DrawRect(new Rect2(left, subsurfaceTop, width, subsurfaceH), Subsurface);
		}
	}
}
