#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using PixelArmies.SimCore;

namespace PixelArmies.Presentation;

// Procedural sound effects for battle events.
// Generates raw PCM waveforms and plays them via AudioStreamWAV.
public partial class BattleAudio : Node
{
	private const int SampleRate = 22050;
	private const float MasterVolume = 0.55f;

	// Pre-baked streams for each sound type — generated once, reused per play
	private AudioStreamWAV? _meleeHit;
	private AudioStreamWAV? _rangedHit;
	private AudioStreamWAV? _cleaveSweep;
	private AudioStreamWAV? _deathT1;
	private AudioStreamWAV? _deathT2;
	private AudioStreamWAV? _deathHeavy;
	private AudioStreamWAV? _baseDamage;
	private AudioStreamWAV? _tierUnlock;
	private AudioStreamWAV? _victory;

	// Throttle: don't play the same category more than once every N ms per frame
	private float _meleeHitCooldown;
	private float _rangedHitCooldown;
	private const float MeleeThrottle  = 0.05f;
	private const float RangedThrottle = 0.04f;

	public override void _Ready()
	{
		_meleeHit    = MakeStream(GenerateTone(280f, 140f, 0.07f, 0.50f, decayRate: 7f));
		_rangedHit   = MakeStream(GenerateTone(440f, 200f, 0.09f, 0.40f, decayRate: 6f));
		_cleaveSweep = MakeStream(GenerateSweep(130f, 60f,  0.20f, 0.65f, decayRate: 3.5f));
		_deathT1     = MakeStream(GenerateTone(380f, 90f,  0.10f, 0.38f, decayRate: 5f));
		_deathT2     = MakeStream(GenerateTone(260f, 70f,  0.15f, 0.50f, decayRate: 4f));
		_deathHeavy  = MakeStream(GenerateTone(160f, 45f,  0.28f, 0.72f, decayRate: 3f));
		_baseDamage  = MakeStream(GenerateBaseBoom());
		_tierUnlock  = MakeStream(GenerateFanfare());
		_victory     = MakeStream(GenerateVictory());
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_meleeHitCooldown  = Math.Max(0f, _meleeHitCooldown  - dt);
		_rangedHitCooldown = Math.Max(0f, _rangedHitCooldown - dt);
	}

	public void OnDamageEvents(IReadOnlyList<DamageEvent> events, Dictionary<int, UnitDef> unitDefs)
	{
		bool playedMelee = false;
		bool playedRanged = false;
		bool hasCleave = false;

		for (int i = 0; i < events.Count; i++)
		{
			var ev = events[i];
			if (ev.Damage <= 0f) continue;

			if (ev.IsAoe)
			{
				hasCleave = true;
			}
			else if (ev.IsRanged)
			{
				if (!playedRanged && _rangedHitCooldown <= 0f)
				{
					Play(_rangedHit);
					_rangedHitCooldown = RangedThrottle;
					playedRanged = true;
				}
			}
			else
			{
				if (!playedMelee && _meleeHitCooldown <= 0f)
				{
					Play(_meleeHit);
					_meleeHitCooldown = MeleeThrottle;
					playedMelee = true;
				}
			}
		}

		if (hasCleave) Play(_cleaveSweep);
	}

	public void OnDeathEvents(IReadOnlyList<UnitDiedEvent> events, Dictionary<int, UnitDef> unitDefs)
	{
		int maxTier = 0;
		for (int i = 0; i < events.Count; i++)
		{
			if (unitDefs.TryGetValue(events[i].UnitId, out var def))
				maxTier = Math.Max(maxTier, def.Tier);
		}

		if (maxTier >= 3) Play(_deathHeavy);
		else if (maxTier == 2) Play(_deathT2);
		else if (maxTier == 1) Play(_deathT1);
	}

	public void OnBaseDamage() => Play(_baseDamage);
	public void OnTierUnlock() => Play(_tierUnlock);
	public void OnVictory()    => Play(_victory);

	// --- Playback ---

	private void Play(AudioStreamWAV? stream)
	{
		if (stream == null) return;
		var player = new AudioStreamPlayer { Stream = stream, VolumeDb = Mathf.LinearToDb(MasterVolume) };
		AddChild(player);
		player.Play();
		player.Finished += () => player.QueueFree();
	}

	// --- PCM generators ---

	private static AudioStreamWAV MakeStream(byte[] pcm)
	{
		return new AudioStreamWAV
		{
			Data = pcm,
			Format = AudioStreamWAV.FormatEnum.Format16Bits,
			MixRate = SampleRate,
			Stereo = false,
		};
	}

	// Simple sine tone with exponential decay and optional frequency glide
	private static byte[] GenerateTone(float freqStart, float freqEnd, float duration, float volume, float decayRate = 5f)
	{
		int n = (int)(SampleRate * duration);
		var data = new byte[n * 2];
		float phase = 0f;

		for (int i = 0; i < n; i++)
		{
			float normalized = (float)i / n;
			float freq = Mathf.Lerp(freqStart, freqEnd, normalized);
			float envelope = Mathf.Exp(-normalized * decayRate) * volume;
			phase += Mathf.Tau * freq / SampleRate;
			float sample = Mathf.Sin(phase) * envelope;
			WriteFrame(data, i, sample);
		}
		return data;
	}

	// Frequency sweep with additive harmonic for richer "whoosh" character
	private static byte[] GenerateSweep(float freqStart, float freqEnd, float duration, float volume, float decayRate = 3.5f)
	{
		int n = (int)(SampleRate * duration);
		var data = new byte[n * 2];
		float phase = 0f;
		float phase2 = 0f;

		for (int i = 0; i < n; i++)
		{
			float normalized = (float)i / n;
			float freq = Mathf.Lerp(freqStart, freqEnd, normalized * normalized);  // easing
			float envelope = Mathf.Exp(-normalized * decayRate) * volume;
			phase  += Mathf.Tau * freq / SampleRate;
			phase2 += Mathf.Tau * (freq * 1.5f) / SampleRate;
			float sample = (Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase2) * 0.3f) * envelope;
			WriteFrame(data, i, sample);
		}
		return data;
	}

	// Deep low boom for base damage
	private static byte[] GenerateBaseBoom()
	{
		const float Duration = 0.45f;
		int n = (int)(SampleRate * Duration);
		var data = new byte[n * 2];
		float phase = 0f;
		float noisePhase = 0f;

		for (int i = 0; i < n; i++)
		{
			float normalized = (float)i / n;
			float freq = Mathf.Lerp(80f, 28f, normalized * normalized);
			float envelope = normalized < 0.05f
				? normalized / 0.05f
				: Mathf.Exp(-(normalized - 0.05f) * 3.5f);
			envelope *= 0.85f;
			phase += Mathf.Tau * freq / SampleRate;
			noisePhase += Mathf.Tau * (freq * 2.1f) / SampleRate;
			float sample = (Mathf.Sin(phase) * 0.65f + Mathf.Sin(noisePhase) * 0.35f) * envelope;
			WriteFrame(data, i, sample);
		}
		return data;
	}

	// Three ascending notes: root, major third, fifth
	private static byte[] GenerateFanfare()
	{
		float[] freqs = { 330f, 415f, 495f, 660f };
		const float NoteLen = 0.12f;
		const float Volume = 0.55f;
		int samplesPerNote = (int)(SampleRate * NoteLen);
		int total = samplesPerNote * freqs.Length;
		var data = new byte[total * 2];

		for (int n = 0; n < freqs.Length; n++)
		{
			float phase = 0f;
			for (int i = 0; i < samplesPerNote; i++)
			{
				float normalized = (float)i / samplesPerNote;
				float env = normalized < 0.1f ? normalized / 0.1f : Mathf.Exp(-(normalized - 0.1f) * 5f);
				phase += Mathf.Tau * freqs[n] / SampleRate;
				float sample = Mathf.Sin(phase) * env * Volume;
				WriteFrame(data, n * samplesPerNote + i, sample);
			}
		}
		return data;
	}

	// Short ascending major chord arpeggio
	private static byte[] GenerateVictory()
	{
		float[] freqs = { 262f, 330f, 392f, 523f, 660f };
		const float NoteLen = 0.10f;
		const float Volume = 0.55f;
		int samplesPerNote = (int)(SampleRate * NoteLen);
		int total = samplesPerNote * freqs.Length;
		var data = new byte[total * 2];

		for (int n = 0; n < freqs.Length; n++)
		{
			float phase = 0f;
			for (int i = 0; i < samplesPerNote; i++)
			{
				float normalized = (float)i / samplesPerNote;
				float env = normalized < 0.08f ? normalized / 0.08f : Mathf.Exp(-(normalized - 0.08f) * 4f);
				phase += Mathf.Tau * freqs[n] / SampleRate;
				float sample = Mathf.Sin(phase) * env * Volume;
				WriteFrame(data, n * samplesPerNote + i, sample);
			}
		}
		return data;
	}

	private static void WriteFrame(byte[] data, int index, float sample)
	{
		short s = (short)Mathf.RoundToInt(Mathf.Clamp(sample * 32767f, -32767f, 32767f));
		data[index * 2]     = (byte)(s & 0xFF);
		data[index * 2 + 1] = (byte)((s >> 8) & 0xFF);
	}
}
