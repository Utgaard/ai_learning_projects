#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using PixelArmies.Content;
using PixelArmies.Presentation;
using PixelArmies.SimCore;
using SimSide = PixelArmies.SimCore.Side;

namespace PixelArmies.GameHost;

public partial class BattleGameHost : Node2D
{
	private const float GroundY = 120f;
	private const float PaddingFactor = 1.18f;
	private const float MinSpan = 200f;
	private const float MinZoom = 0.10f;
	private const float MaxZoom = 1.0f;
	private const float PosSpeed = 6f;
	private const float ZoomSpeed = 5f;
	private const float ShakeDecay = 9f;
	private static readonly float[] TimeScales = { 1f, 2f, 4f };

	private BattleSimulator? _sim;
	private SimConfig? _cfg;
	private DebugSettings _debugSettings = DebugSettings.Disabled;
	private ArmyDef? _leftArmy;
	private ArmyDef? _rightArmy;

	private float _accum;
	private float _timeScale = 1f;
	private int _timeScaleIndex;
	private int _battleSeed = 12345;

	// Screen shake
	private float _shakeAmplitude;

	// Camera
	private Camera2D? _cam;

	// Nodes
	private BattlefieldBackdrop? _backdrop;
	private BattleView? _view;
	private BattleAudio? _audio;
	private HudRoot? _hud;
	private CanvasLayer? _overlayLayer;
	private Label? _speedLabel;
	private Control? _resultOverlay;
	private Label? _escalationBanner;
	private float _escalationBannerTimer;
	private int _prevUnlockedTier;
	private bool _resultShown;

	// Base HP tracking for shake/audio
	private float _prevLeftBaseHp;
	private float _prevRightBaseHp;

	// Match history — injected by Main
	private List<MatchRecord>? _matchHistory;

	// Callback when the player wants to return to army select
	public Action<MatchRecord>? OnBattleEnded;

	private readonly HudSnapshot _hudSnapshot = new();
	private readonly List<DamageEvent> _frameDamageEvents = new();
	private readonly List<UnitDiedEvent> _frameDeathEvents = new();
	private readonly List<PowerAllocatedEvent> _framePowerEvents = new();
	private readonly List<UnitSpawnedEvent> _frameSpawnEvents = new();
	private static readonly IReadOnlyList<PowerAllocatedEvent> EmptyPowerEvents = Array.Empty<PowerAllocatedEvent>();
	private static readonly IReadOnlyList<UnitSpawnedEvent> EmptySpawnEvents = Array.Empty<UnitSpawnedEvent>();

	public override void _Ready()
	{
		_cfg = new SimConfig();

		var left  = _leftArmy  ?? DemoArmies.LeftBasic();
		var right = _rightArmy ?? DemoArmies.RightBasic();
		StartBattle(left, right);
	}

	public void SetArmies(ArmyDef left, ArmyDef right)
	{
		_leftArmy  = left;
		_rightArmy = right;
	}

	public void SetMatchHistory(List<MatchRecord> history) => _matchHistory = history;

	public void AttachHud(HudRoot hud) => _hud = hud;

	public void ConfigureDebug(DebugSettings settings) => _debugSettings = settings;

	private void StartBattle(ArmyDef left, ArmyDef right)
	{
		_cfg ??= new SimConfig();
		_leftArmy  = left;
		_rightArmy = right;

		// Clean up previous battle
		_view?.QueueFree();
		_view = null;
		_resultOverlay?.QueueFree();
		_resultOverlay = null;
		_resultShown = false;
		_accum = 0f;
		_prevUnlockedTier = 1;
		_shakeAmplitude = 0f;

		_sim = new BattleSimulator(_cfg, left, right, _battleSeed, _debugSettings);
		_prevLeftBaseHp  = _cfg.BaseMaxHp;
		_prevRightBaseHp = _cfg.BaseMaxHp;

		if (_backdrop == null)
		{
			_backdrop = new BattlefieldBackdrop { GroundY = GroundY, ZIndex = -20, ZAsRelative = true };
			AddChild(_backdrop);
		}

		_view = new BattleView();
		_view.Configure(_sim, _cfg, GroundY);
		_view.BaseDamagedLeft  += () => { AddShake(22f); _audio?.OnBaseDamage(); };
		_view.BaseDamagedRight += () => { AddShake(22f); _audio?.OnBaseDamage(); };
		AddChild(_view);

		if (_audio == null)
		{
			_audio = new BattleAudio();
			AddChild(_audio);
		}

		if (_hud == null && GetViewport() is not SubViewport)
		{
			_hud = new HudRoot();
			AddChild(_hud);
		}

		if (_cam == null)
		{
			_cam = new Camera2D
			{
				Enabled = true,
				PositionSmoothingEnabled = false,
				Zoom = new Vector2(MaxZoom, MaxZoom),
			};
			AddChild(_cam);
			_backdrop?.AttachCamera(_cam);
		}

		if (_overlayLayer == null)
		{
			_overlayLayer = new CanvasLayer { Layer = 15 };
			AddChild(_overlayLayer);
		}

		BuildOverlayLabels();

		GD.Print($"Battle started: {left.Name} vs {right.Name} (seed {_battleSeed})");
	}

	private void BuildOverlayLabels()
	{
		if (_speedLabel == null)
		{
			_speedLabel = new Label
			{
				Text = "Speed: 1x  [Tab]",
				AnchorLeft = 0f, AnchorTop = 1f, AnchorRight = 0f, AnchorBottom = 1f,
				OffsetLeft = 8f, OffsetTop = -28f, OffsetRight = 200f, OffsetBottom = -4f,
			};
			_speedLabel.AddThemeFontSizeOverride("font_size", 14);
			_overlayLayer!.AddChild(_speedLabel);
		}

		if (_escalationBanner == null)
		{
			_escalationBanner = new Label
			{
				Text = "",
				AnchorLeft = 0.5f, AnchorTop = 0.35f, AnchorRight = 0.5f, AnchorBottom = 0.35f,
				OffsetLeft = -150f, OffsetRight = 150f, OffsetTop = 0f, OffsetBottom = 36f,
				HorizontalAlignment = HorizontalAlignment.Center,
				Visible = false,
			};
			_escalationBanner.AddThemeFontSizeOverride("font_size", 28);
			_overlayLayer!.AddChild(_escalationBanner);
		}
	}

	public override void _Process(double delta)
	{
		if (_sim == null || _cfg == null || _view == null) return;

		float dt = (float)delta;

		_frameDamageEvents.Clear();
		_frameDeathEvents.Clear();
		_framePowerEvents.Clear();
		_frameSpawnEvents.Clear();

		if (!_sim.State.IsOver)
		{
			_accum += dt * _timeScale;
			while (_accum >= SimConfig.FixedDt)
			{
				_sim.Step(SimConfig.FixedDt);
				var events  = _sim.ConsumeDamageEvents();
				if (events.Count  > 0) _frameDamageEvents.AddRange(events);
				var deaths  = _sim.ConsumeUnitDiedEvents();
				if (deaths.Count  > 0) _frameDeathEvents.AddRange(deaths);
				var power   = _sim.ConsumePowerAllocatedEvents();
				if (power.Count   > 0) _framePowerEvents.AddRange(power);
				var spawns  = _sim.ConsumeUnitSpawnedEvents();
				if (spawns.Count  > 0) _frameSpawnEvents.AddRange(spawns);
				_accum -= SimConfig.FixedDt;
			}

			CheckEscalation();
			ProcessAudio();
			ProcessShakeFromEvents();
		}
		else
		{
			var events  = _sim.ConsumeDamageEvents();
			if (events.Count  > 0) _frameDamageEvents.AddRange(events);
			var deaths  = _sim.ConsumeUnitDiedEvents();
			if (deaths.Count  > 0) _frameDeathEvents.AddRange(deaths);
			var power   = _sim.ConsumePowerAllocatedEvents();
			if (power.Count   > 0) _framePowerEvents.AddRange(power);
			var spawns  = _sim.ConsumeUnitSpawnedEvents();
			if (spawns.Count  > 0) _frameSpawnEvents.AddRange(spawns);

			if (!_resultShown)
			{
				_resultShown = true;
				_audio?.OnVictory();
				var record = BuildMatchRecord(_sim.State);
				ShowResultOverlay(_sim.State, record);
				OnBattleEnded?.Invoke(record);
			}
		}

		UpdateEscalationBanner(dt);
		UpdateShake(dt);

		_view.Advance(dt, _frameDamageEvents, _frameDeathEvents);
		UpdateHud();
		UpdateCamera(dt);
		_view.QueueRedraw();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

		switch (key.Keycode)
		{
			case Key.Tab:
				_timeScaleIndex = (_timeScaleIndex + 1) % TimeScales.Length;
				_timeScale = TimeScales[_timeScaleIndex];
				if (_speedLabel != null)
					_speedLabel.Text = $"Speed: {_timeScale:0}x  [Tab]";
				break;
			case Key.R when _resultShown:
				_battleSeed++;
				StartBattle(_leftArmy!, _rightArmy!);
				break;
			case Key.S when _resultShown:
				ReturnToSelect();
				break;
		}
	}

	private void ReturnToSelect()
	{
		// Signal to Main to rebuild the selection screen
		// We free ourselves from the tree so Main can recreate the selector
		QueueFree();
	}

	// --- Screen shake ---

	private void AddShake(float amount) => _shakeAmplitude = Math.Max(_shakeAmplitude, amount);

	private void UpdateShake(float dt)
	{
		if (_shakeAmplitude < 0.5f) { _shakeAmplitude = 0f; return; }
		_shakeAmplitude *= Mathf.Exp(-ShakeDecay * dt);
		if (_cam != null)
		{
			_cam.Offset = new Vector2(
				(float)GD.RandRange(-1.0, 1.0) * _shakeAmplitude,
				(float)GD.RandRange(-1.0, 1.0) * _shakeAmplitude);
		}
	}

	private void ProcessShakeFromEvents()
	{
		// Melee hits: small shake
		for (int i = 0; i < _frameDamageEvents.Count; i++)
		{
			var ev = _frameDamageEvents[i];
			if (!ev.IsRanged && !ev.IsAoe) AddShake(3.5f);
			if (ev.IsAoe) AddShake(10f);  // Cleave / death-explode
		}

		// Heavy unit deaths: medium shake
		for (int i = 0; i < _frameDeathEvents.Count; i++)
		{
			// Tier tracked via spawn events — just add a base shake per death
			AddShake(5f);
		}

		// Tier-4 spawns: large shake
		for (int i = 0; i < _frameSpawnEvents.Count; i++)
		{
			if (_frameSpawnEvents[i].Tier >= 4) AddShake(16f);
			else if (_frameSpawnEvents[i].Tier == 3) AddShake(7f);
		}
	}

	// --- Audio ---

	private void ProcessAudio()
	{
		if (_audio == null) return;

		// Re-use _lastKnownDefs from BattleView is not accessible here;
		// we pass an approximation: empty dict is fine — audio just needs tier for deaths
		if (_frameDamageEvents.Count > 0)
			_audio.OnDamageEvents(_frameDamageEvents, _audioDefs);

		if (_frameDeathEvents.Count > 0)
			_audio.OnDeathEvents(_frameDeathEvents, _audioDefs);

		// Track spawn defs so death audio can pick up tier
		for (int i = 0; i < _frameSpawnEvents.Count; i++)
		{
			var e = _frameSpawnEvents[i];
			if (_sim == null) continue;
			var army = e.Side == SimSide.Left ? _sim.LeftArmy : _sim.RightArmy;
			foreach (var u in army.Units)
			{
				if (u.Id == e.UnitDefId)
				{
					_audioDefs[e.UnitId] = u;
					break;
				}
			}
		}
	}

	// Lightweight def lookup for audio tier detection (unit id → UnitDef)
	private readonly Dictionary<int, UnitDef> _audioDefs = new();

	// --- Escalation ---

	private void CheckEscalation()
	{
		if (_sim == null || _cfg == null) return;
		int current = _cfg.UnlockedTierForTime(_sim.State.Time);
		if (current > _prevUnlockedTier)
		{
			_prevUnlockedTier = current;
			ShowEscalationBanner(current);
			_audio?.OnTierUnlock();
			AddShake(12f);
		}
	}

	private void ShowEscalationBanner(int tier)
	{
		if (_escalationBanner == null) return;
		_escalationBanner.Text = $"--- TIER {tier} UNLOCKED ---";
		_escalationBanner.Visible = true;
		_escalationBannerTimer = 2.5f;
	}

	private void UpdateEscalationBanner(float dt)
	{
		if (_escalationBanner == null || !_escalationBanner.Visible) return;
		_escalationBannerTimer -= dt;
		float alpha = Mathf.Clamp(_escalationBannerTimer / 0.6f, 0f, 1f);
		_escalationBanner.Modulate = new Color(1f, 0.9f, 0.3f, alpha);
		if (_escalationBannerTimer <= 0f)
			_escalationBanner.Visible = false;
	}

	// --- Result overlay ---

	private MatchRecord BuildMatchRecord(BattleState state)
	{
		string winner = state.Winner switch
		{
			SimSide.Left  => _leftArmy?.Name  ?? "Left",
			SimSide.Right => _rightArmy?.Name ?? "Right",
			_             => "Draw",
		};
		bool stomp = state.Winner == SimSide.Left
			? state.RightBaseHp <= 0 && state.LeftBaseHp  > _cfg!.BaseMaxHp * 0.70f
			: state.LeftBaseHp  <= 0 && state.RightBaseHp > _cfg!.BaseMaxHp * 0.70f;

		return new MatchRecord(
			_leftArmy?.Name  ?? "Left",
			_rightArmy?.Name ?? "Right",
			winner,
			state.Time,
			state.LeftKills,
			state.RightKills,
			stomp);
	}

	private void ShowResultOverlay(BattleState state, MatchRecord record)
	{
		string title  = record.Stomp ? $"{record.Winner} wins!  STOMP!" : $"{record.Winner} wins!";
		string time   = $"Battle time: {record.BattleTime:0.0}s";
		string kills  = $"Kills  L {record.LeftKills}  vs  R {record.RightKills}";
		string hint   = "R — rematch   S — select armies";

		_resultOverlay = new Control
		{
			AnchorLeft = 0.5f, AnchorTop = 0.5f, AnchorRight = 0.5f, AnchorBottom = 0.5f,
			OffsetLeft = -200f, OffsetRight = 200f, OffsetTop = -90f, OffsetBottom = 90f,
			ZIndex = 60,
		};

		var bg = new ColorRect
		{
			AnchorLeft = 0f, AnchorTop = 0f, AnchorRight = 1f, AnchorBottom = 1f,
			Color = new Color(0.05f, 0.05f, 0.10f, 0.92f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};

		var box = new VBoxContainer
		{
			AnchorLeft = 0f, AnchorTop = 0f, AnchorRight = 1f, AnchorBottom = 1f,
			OffsetLeft = 16f, OffsetRight = -16f, OffsetTop = 12f, OffsetBottom = -12f,
		};
		box.AddThemeConstantOverride("separation", 6);

		var titleLabel = new Label { Text = title, HorizontalAlignment = HorizontalAlignment.Center };
		titleLabel.AddThemeFontSizeOverride("font_size", 28);
		titleLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));

		var hintLabel = MakeResultLine(hint);
		hintLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1f));

		box.AddChild(titleLabel);
		box.AddChild(MakeResultLine(time));
		box.AddChild(MakeResultLine(kills));
		box.AddChild(new HSeparator());
		box.AddChild(hintLabel);

		_resultOverlay.AddChild(bg);
		_resultOverlay.AddChild(box);
		_overlayLayer!.AddChild(_resultOverlay);
	}

	private static Label MakeResultLine(string text)
	{
		var l = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
		l.AddThemeFontSizeOverride("font_size", 16);
		return l;
	}

	// --- Camera ---

	private void UpdateCamera(float dt)
	{
		if (_sim == null || _cfg == null || _cam == null) return;

		float leftBaseX  = 0f;
		float rightBaseX = _cfg.BattlefieldLength;
		float leftFrontX  = leftBaseX;
		float rightFrontX = rightBaseX;

		foreach (var u in _sim.State.Units)
		{
			if (!u.Alive) continue;
			if (u.Side == SimSide.Left) leftFrontX  = Mathf.Max(leftFrontX,  u.X);
			else                        rightFrontX = Mathf.Min(rightFrontX, u.X);
		}

		float baseSpan  = rightBaseX - leftBaseX;
		float frontSpan = Mathf.Max(MinSpan, rightFrontX - leftFrontX);
		float t         = 1f - Mathf.Clamp(frontSpan / Mathf.Max(1f, baseSpan), 0f, 1f);

		float baseCenter   = (leftBaseX  + rightBaseX)  * 0.5f;
		float frontCenter  = (leftFrontX + rightFrontX) * 0.5f;
		float targetCenterX = Mathf.Lerp(baseCenter, frontCenter, t);
		float targetSpan    = Mathf.Lerp(baseSpan, frontSpan, t);

		float viewportWidth     = GetViewportRect().Size.X;
		float desiredVisibleWidth = Mathf.Max(1f, targetSpan * PaddingFactor);
		float zoomX = Mathf.Clamp(viewportWidth / desiredVisibleWidth, MinZoom, MaxZoom);
		var targetZoom = new Vector2(zoomX, zoomX);
		var targetPos  = new Vector2(targetCenterX, 0f);

		float posAlpha  = 1f - Mathf.Exp(-PosSpeed  * dt);
		float zoomAlpha = 1f - Mathf.Exp(-ZoomSpeed * dt);

		_cam.Position = _cam.Position.Lerp(targetPos, posAlpha);
		_cam.Zoom     = _cam.Zoom.Lerp(targetZoom, zoomAlpha);
	}

	// --- HUD ---

	private void UpdateHud()
	{
		if (_sim == null || _cfg == null || _hud == null) return;

		_hudSnapshot.Left.BaseHp  = _sim.State.LeftBaseHp;
		_hudSnapshot.Right.BaseHp = _sim.State.RightBaseHp;
		_hudSnapshot.Left.UnlockedTier  = _cfg.UnlockedTierForTime(_sim.State.Time);
		_hudSnapshot.Right.UnlockedTier = _hudSnapshot.Left.UnlockedTier;
		_hudSnapshot.Left.Kills  = _sim.State.LeftKills;
		_hudSnapshot.Right.Kills = _sim.State.RightKills;
		_hudSnapshot.Left.DamageDealt  = _sim.State.LeftDamageDealt;
		_hudSnapshot.Right.DamageDealt = _sim.State.RightDamageDealt;

		var leftSpawner = _sim.LeftSpawner;
		_hudSnapshot.Left.PowerPool             = leftSpawner.PowerPool;
		_hudSnapshot.Left.CurrentBucketTier     = leftSpawner.CurrentBucketTier;
		_hudSnapshot.Left.CurrentBucketProgress = leftSpawner.CurrentBucketProgress;
		_hudSnapshot.Left.CurrentTargetCost     = leftSpawner.CurrentTargetCost;

		var rightSpawner = _sim.RightSpawner;
		_hudSnapshot.Right.PowerPool             = rightSpawner.PowerPool;
		_hudSnapshot.Right.CurrentBucketTier     = rightSpawner.CurrentBucketTier;
		_hudSnapshot.Right.CurrentBucketProgress = rightSpawner.CurrentBucketProgress;
		_hudSnapshot.Right.CurrentTargetCost     = rightSpawner.CurrentTargetCost;

		var powerEvents = _framePowerEvents.Count > 0
			? (IReadOnlyList<PowerAllocatedEvent>)_framePowerEvents
			: EmptyPowerEvents;
		var spawnEvents = _frameSpawnEvents.Count > 0
			? (IReadOnlyList<UnitSpawnedEvent>)_frameSpawnEvents
			: EmptySpawnEvents;
		_hud.UpdateHud(_hudSnapshot, powerEvents, spawnEvents, _frameDeathEvents, _frameDamageEvents);
	}
}
