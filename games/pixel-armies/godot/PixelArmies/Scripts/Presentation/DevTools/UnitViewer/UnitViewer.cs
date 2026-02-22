#nullable enable

using System.Collections.Generic;
using Godot;
using PixelArmies.Content;
using PixelArmies.SimCore;
using SimSide = PixelArmies.SimCore.Side;

namespace PixelArmies.Presentation.DevTools.UnitViewer;

public partial class UnitViewer : Node2D
{
	private const float GroundY = 120f;
	private const float DefaultAttackDuration = 0.22f;
	private const float HitFlashDuration = 0.12f;

	private const float AutoIdleTime = 1.2f;
	private const float AutoWalkTime = 1.6f;
	private const float AutoAttackTime = 0.4f;
	private const float AutoHitTime = 0.35f;
	private const float AutoDeathTime = 0.9f;
	private const bool ShowAttackDebug = true;

	private readonly List<UnitEntry> _units = new();
	private int _index;
	private bool _walking;
	private bool _autoLoop = true;
	private float _autoTimer;
	private int _autoStage;
	private bool _stageTriggered;
	private SimSide _side = SimSide.Left;

	private float _walkPhase;
	private float _deadTimer;
	private Vector2 _currentCenter;
	private float _currentWidth;
	private float _currentHeight;

	private Label? _infoLabel;
	private Label? _helpLabel;
	private Vector2 _lastViewportSize;

	private readonly IUnitAnimationProfile[] _profiles =
	{
		new StickTier1Profile(),
		new DefaultRectProfile()
	};

	private readonly HitReactionSystem _hitReactions = new();
	private readonly Dictionary<int, Vector2> _unitPositions = new();
	private readonly Dictionary<int, float> _attackTimers = new();
	private readonly Dictionary<int, float> _attackDurations = new();
	private readonly Dictionary<int, float> _hitFlashTimers = new();
	private readonly List<DamageEvent> _damageEvents = new();

	private ArmyVisualProfile _leftProfile = ArmyVisualProfiles.GetProfile("legion");
	private ArmyVisualProfile _rightProfile = ArmyVisualProfiles.GetProfile("brutes");

	public override void _Ready()
	{
		SetProcess(true);
		BuildUnitList();
		SetupCamera();
		SetupUi();
		UpdateInfoLabel();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (UpdateViewportSize()) PositionHud();
		AdvanceAutoLoop(dt);
		UpdateUnitPosition();
		UpdateAnimationState(dt);
		_hitReactions.Advance(dt, _damageEvents, _unitPositions);
		UpdateProfiles(dt);
		QueueRedraw();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

		switch (key.Keycode)
		{
			case Key.N:
				NextUnit(1);
				break;
			case Key.P:
				NextUnit(-1);
				break;
			case Key.A:
				TriggerAttack();
				break;
			case Key.H:
				TriggerHit();
				break;
			case Key.D:
				TriggerDeath();
				break;
			case Key.W:
				_walking = !_walking;
				break;
			case Key.Space:
				_autoLoop = !_autoLoop;
				_autoTimer = 0f;
				_autoStage = 0;
				break;
			case Key.L:
				_side = SimSide.Left;
				break;
			case Key.R:
				_side = SimSide.Right;
				break;
		}

	}

	public override void _Draw()
	{
		DrawBackdrop();

		if (_units.Count == 0) return;
		if (_deadTimer > 0f)
		{
			DrawProfileOverlays();
			return;
		}

		var entry = _units[_index];
		var def = entry.Def;

		float w = _currentWidth;
		float h = _currentHeight;
		var center = _currentCenter;

		var color = _side == SimSide.Left ? Colors.Cyan : Colors.Orange;
		var armyProfile = _side == SimSide.Left ? _leftProfile : _rightProfile;
		var stickStyle = armyProfile.ResolveStickProfile(def.Id);
		var profile = GetProfile(def, armyProfile);

		float attackPhase = _attackTimers.TryGetValue(entry.UnitId, out var atkTimer)
			? 1f - Mathf.Clamp(atkTimer / GetAttackDuration(entry.Def), 0f, 1f)
			: 0f;
		float flashAlpha = _hitFlashTimers.TryGetValue(entry.UnitId, out var flash)
			? Mathf.Clamp(flash / HitFlashDuration, 0f, 1f)
			: 0f;

		var data = new UnitDrawData(
			center,
			w,
			h,
			color,
			_side,
			_walking,
			_walkPhase,
			attackPhase,
			def.WeaponLength,
			flashAlpha,
			stickStyle);

		var font = ThemeDB.FallbackFont;
		var context = new UnitDrawContext(this, font, 10);
		profile.DrawUnit(context, entry.UnitState, data);

		if (ShowAttackDebug && attackPhase > 0f)
		{
			var labelPos = new Vector2(center.X - 18f, center.Y - h - 18f);
			DrawString(font, labelPos, "ATTACK", fontSize: 14, modulate: Colors.White);
		}

		DrawProfileOverlays();
	}

	private void BuildUnitList()
	{
		_units.Clear();

		var armies = new List<ArmyDef>
		{
			DemoArmies.LeftBasic(),
			DemoArmies.RightBasic(),
			DemoArmies.Skirmishers()
		};

		int id = 1;
		for (int a = 0; a < armies.Count; a++)
		{
			var army = armies[a];
			for (int i = 0; i < army.Units.Count; i++)
			{
				var def = army.Units[i];
				var state = new UnitState(id, SimSide.Left, def, 0f);
				_units.Add(new UnitEntry(army, def, id, state));
				id++;
			}
		}

		UpdateInfoLabel();
	}

	private void SetupCamera()
	{
		var cam = new Camera2D
		{
			Enabled = true,
			Position = new Vector2(0f, 0f),
			Zoom = new Vector2(1f, 1f),
		};
		AddChild(cam);
	}

	private void SetupUi()
	{
		var layer = new CanvasLayer { Layer = 20 };
		AddChild(layer);

		_infoLabel = new Label
		{
			Text = "",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_infoLabel.AddThemeFontSizeOverride("font_size", 18);
		layer.AddChild(_infoLabel);

		_helpLabel = new Label
		{
			Text = "N/P next/prev  L/R side  W walk  A attack  H hit  D death  Space auto",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		_helpLabel.AddThemeFontSizeOverride("font_size", 14);
		layer.AddChild(_helpLabel);

		PositionHud();
	}

	private bool UpdateViewportSize()
	{
		var size = GetViewportRect().Size;
		if (size != _lastViewportSize)
		{
			_lastViewportSize = size;
			return true;
		}

		return false;
	}

	private void UpdateInfoLabel()
	{
		if (_infoLabel == null || _units.Count == 0) return;
		var entry = _units[_index];
		var profile = _side == SimSide.Left ? _leftProfile : _rightProfile;

		_infoLabel.Text =
			$"{entry.Def.Id} (Tier {entry.Def.Tier})  " +
			$"Army={entry.Army.Name}  Style={profile.Id}  " +
			$"WeaponLen={entry.Def.WeaponLength:0}";
		PositionHud();
	}

	private void PositionHud()
	{
		if (_infoLabel == null || _helpLabel == null) return;
		var size = GetViewportRect().Size;
		float centerX = size.X * 0.5f;
		_infoLabel.Position = new Vector2(centerX - _infoLabel.Size.X * 0.5f, 12f);
		_helpLabel.Position = new Vector2(centerX - _helpLabel.Size.X * 0.5f, 36f);
	}


	private void NextUnit(int dir)
	{
		if (_units.Count == 0) return;
		_index = (_index + dir + _units.Count) % _units.Count;
		ResetState();
		UpdateInfoLabel();
	}

	private void ResetState()
	{
		_walking = false;
		_walkPhase = 0f;
		_attackTimers.Clear();
		_attackDurations.Clear();
		_hitFlashTimers.Clear();
		_deadTimer = 0f;
		_damageEvents.Clear();
	}

	private void AdvanceAutoLoop(float dt)
	{
		if (!_autoLoop) return;

		_autoTimer += dt;
		switch (_autoStage)
		{
			case 0:
				if (_autoTimer >= AutoIdleTime) NextStage(1);
				break;
			case 1:
				_walking = true;
				if (_autoTimer >= AutoWalkTime) NextStage(2);
				break;
			case 2:
				if (!_stageTriggered)
				{
					TriggerAttack();
					_stageTriggered = true;
				}
				_walking = false;
				if (_autoTimer >= AutoAttackTime) NextStage(3);
				break;
			case 3:
				if (!_stageTriggered)
				{
					TriggerHit();
					_stageTriggered = true;
				}
				if (_autoTimer >= AutoHitTime) NextStage(4);
				break;
			case 4:
				if (!_stageTriggered)
				{
					TriggerDeath();
					_stageTriggered = true;
				}
				if (_autoTimer >= AutoDeathTime) NextStage(0);
				break;
		}
	}

	private void NextStage(int stage)
	{
		_autoStage = stage;
		_autoTimer = 0f;
		_stageTriggered = false;
		if (stage == 0) ResetState();
	}

	private void UpdateAnimationState(float dt)
	{
		if (_attackTimers.Count > 0)
		{
			_walking = false;
		}

		if (_walking)
		{
			float freq = Mathf.Clamp(6f + CurrentDef().Speed * 0.08f, 6f, 10f);
			_walkPhase += dt * freq;
		}

		UpdateTimers(_attackTimers, _attackDurations, dt);
		UpdateTimers(_hitFlashTimers, dt);

		if (_deadTimer > 0f) _deadTimer = Mathf.Max(0f, _deadTimer - dt);
		_damageEvents.Clear();
	}

	private void UpdateUnitPosition()
	{
		if (_units.Count == 0) return;
		var def = _units[_index].Def;
		_currentWidth = 10 + (def.Tier - 1) * 4;
		_currentHeight = 12 + (def.Tier - 1) * 4;
		_currentCenter = new Vector2(0f, GroundY - _currentHeight * 0.5f);
		_unitPositions[_units[_index].UnitId] = _currentCenter;
	}

	private void UpdateProfiles(float dt)
	{
		for (int i = 0; i < _profiles.Length; i++)
		{
			_profiles[i].Update(dt);
		}
	}

	private void DrawProfileOverlays()
	{
		for (int i = 0; i < _profiles.Length; i++)
		{
			_profiles[i].DrawOverlay(this);
		}
	}

	private void TriggerAttack()
	{
		var entry = _units[_index];
		float duration = GetAttackDuration(entry.Def);
		_attackTimers[entry.UnitId] = duration;
		_attackDurations[entry.UnitId] = duration;
	}

	private void TriggerHit()
	{
		var entry = _units[_index];
		_hitFlashTimers[entry.UnitId] = HitFlashDuration;
		_damageEvents.Clear();
		_damageEvents.Add(new DamageEvent(entry.UnitId, entry.UnitId, 1f, false));
	}

	private void TriggerDeath()
	{
		var entry = _units[_index];
		var def = entry.Def;
		float w = 10 + (def.Tier - 1) * 4;
		float h = 12 + (def.Tier - 1) * 4;
		var center = new Vector2(0f, GroundY - h * 0.5f);
		var feet = new Vector2(0f, GroundY);
		var color = _side == SimSide.Left ? Colors.Cyan : Colors.Orange;

		var armyProfile = _side == SimSide.Left ? _leftProfile : _rightProfile;
		var info = new UnitDeathInfo(
			entry.UnitId,
			center,
			feet,
			new BattleView.UnitVisual { Width = w, Height = h, Color = color },
			_side,
			def.WeaponLength,
			armyProfile.StickDeathProfile);

		var profile = GetProfile(def, armyProfile);
		profile.OnDeath(info);
		_deadTimer = 0.6f;
	}

	private UnitDef CurrentDef() => _units[_index].Def;

	private IUnitAnimationProfile GetProfile(UnitDef def, ArmyVisualProfile armyProfile)
	{
		return armyProfile.UseStickFor(def) ? _profiles[0] : _profiles[1];
	}

	private static readonly List<int> TimerScratch = new();

	private static void UpdateTimers(Dictionary<int, float> timers, float dt)
	{
		TimerScratch.Clear();
		foreach (var key in timers.Keys) TimerScratch.Add(key);
		for (int i = 0; i < TimerScratch.Count; i++)
		{
			int key = TimerScratch[i];
			float remaining = timers[key] - dt;
			if (remaining <= 0f) timers.Remove(key);
			else timers[key] = remaining;
		}
	}

	private static void UpdateTimers(Dictionary<int, float> timers, Dictionary<int, float> durations, float dt)
	{
		TimerScratch.Clear();
		foreach (var key in timers.Keys) TimerScratch.Add(key);
		for (int i = 0; i < TimerScratch.Count; i++)
		{
			int key = TimerScratch[i];
			float remaining = timers[key] - dt;
			if (remaining <= 0f)
			{
				timers.Remove(key);
				durations.Remove(key);
			}
			else
			{
				timers[key] = remaining;
			}
		}
	}

	private static float GetAttackDuration(UnitDef def)
	{
		return def.AttackDuration > 0f ? def.AttackDuration : DefaultAttackDuration;
	}

	private void DrawBackdrop()
	{
		var viewport = GetViewportRect();
		DrawRect(new Rect2(-viewport.Size.X, -viewport.Size.Y, viewport.Size.X * 2f, viewport.Size.Y * 2f), new Color(0.18f, 0.34f, 0.55f));
		DrawRect(new Rect2(-viewport.Size.X, GroundY, viewport.Size.X * 2f, viewport.Size.Y), new Color(0.18f, 0.28f, 0.18f));
		DrawLine(new Vector2(-viewport.Size.X, GroundY), new Vector2(viewport.Size.X, GroundY), Colors.White, 2f);
	}

	private readonly record struct UnitEntry(ArmyDef Army, UnitDef Def, int UnitId, UnitState UnitState);
}
