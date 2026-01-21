#nullable enable

using System.Collections.Generic;
using Godot;
using PixelArmies.SimCore;

namespace PixelArmies.Presentation;

public sealed class HitReactionSystem
{
	private const float BaseFreezeDuration = 0.07f;
	private const float MinFreezeDuration = 0.05f;
	private const float MaxFreezeDuration = 0.12f;
	private const float DamageToPixels = 0.25f;
	private const float MinKnockback = 2f;
	private const float MaxKnockback = 10f;
	private const float DecaySpeed = 14f;
	private const float MeleeBoost = 1.2f;

	private readonly Dictionary<int, ReactionState> _states = new();
	private readonly Dictionary<int, Vector2> _renderCenters = new();
	private readonly List<int> _scratchIds = new();

	public void Advance(float dt, IReadOnlyList<DamageEvent> events, Dictionary<int, Vector2> simCenters)
	{
		if (events.Count > 0)
		{
			ApplyEvents(events, simCenters);
		}

		UpdateStates(dt);
		UpdateRenderCenters(simCenters);
	}

	public bool TryGetRenderCenter(int unitId, out Vector2 center) =>
		_renderCenters.TryGetValue(unitId, out center);

	private void ApplyEvents(IReadOnlyList<DamageEvent> events, Dictionary<int, Vector2> simCenters)
	{
		for (int i = 0; i < events.Count; i++)
		{
			var ev = events[i];
			if (ev.Damage <= 0f) continue;

			if (!simCenters.TryGetValue(ev.TargetId, out var targetPos)) continue;
			if (!simCenters.TryGetValue(ev.AttackerId, out var attackerPos)) continue;

			var dir = targetPos - attackerPos;
			if (dir.LengthSquared() < 0.0001f)
			{
				dir = new Vector2(targetPos.X >= attackerPos.X ? 1f : -1f, 0f);
			}
			else
			{
				dir = dir.Normalized();
			}

			float mag = Mathf.Clamp(ev.Damage * DamageToPixels, MinKnockback, MaxKnockback);
			if (!ev.IsRanged) mag *= MeleeBoost;
			mag = Mathf.Clamp(mag, MinKnockback, MaxKnockback);

			float freeze = Mathf.Clamp(BaseFreezeDuration + ev.Damage * 0.002f, MinFreezeDuration, MaxFreezeDuration);

			if (!_states.TryGetValue(ev.TargetId, out var state))
			{
				state = new ReactionState();
			}

			state.Offset += dir * mag;
			if (state.Offset.Length() > MaxKnockback)
				state.Offset = state.Offset.Normalized() * MaxKnockback;

			state.FreezeTimer = Mathf.Max(state.FreezeTimer, freeze);
			state.HasFrozenCenter = false;

			_states[ev.TargetId] = state;
		}
	}

	private void UpdateStates(float dt)
	{
		_scratchIds.Clear();
		foreach (var kvp in _states) _scratchIds.Add(kvp.Key);

		for (int i = 0; i < _scratchIds.Count; i++)
		{
			int id = _scratchIds[i];
			var state = _states[id];

			state.FreezeTimer = Mathf.Max(0f, state.FreezeTimer - dt);
			float decay = 1f - Mathf.Exp(-DecaySpeed * dt);
			state.Offset = state.Offset.Lerp(Vector2.Zero, decay);

			if (state.FreezeTimer <= 0f && state.Offset.LengthSquared() < 0.01f)
			{
				_states.Remove(id);
			}
			else
			{
				_states[id] = state;
			}
		}
	}

	private void UpdateRenderCenters(Dictionary<int, Vector2> simCenters)
	{
		_renderCenters.Clear();

		foreach (var kvp in simCenters)
		{
			int id = kvp.Key;
			var simCenter = kvp.Value;

			if (_states.TryGetValue(id, out var state))
			{
				var adjusted = simCenter + state.Offset;
				if (state.FreezeTimer > 0f)
				{
					if (!state.HasFrozenCenter)
					{
						state.FrozenCenter = adjusted;
						state.HasFrozenCenter = true;
						_states[id] = state;
					}
					_renderCenters[id] = state.FrozenCenter;
				}
				else
				{
					_renderCenters[id] = adjusted;
				}
			}
			else
			{
				_renderCenters[id] = simCenter;
			}
		}
	}

	private struct ReactionState
	{
		public float FreezeTimer;
		public Vector2 Offset;
		public Vector2 FrozenCenter;
		public bool HasFrozenCenter;
	}
}
