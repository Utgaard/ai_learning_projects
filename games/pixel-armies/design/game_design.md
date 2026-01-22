# Pixel Armies — Game Design Document

Version: 0.1  
Status: Early Concept  
Audience: Internal (Developer / Family Project)

---

## 1. Core Pillars (LOCKED)

### Game Fantasy
The player feels like a god, selecting opposing armies and watching fate, physics, army traits, and randomness decide the outcome.

### Target Experience
- Exciting
- Physical
- Chaotic but readable
- Randomized
- Spectator-friendly

### Non-Goals
This game explicitly avoids:
- Player interaction once the battle has started
- Micromanagement of units
- Base building
- Fog of war
- Direct unit control
- Competitive or skill-based play

### Platform & Scope
- Primary target: Windows
- Secondary target: Web
- Design bias toward simple, fast-to-implement features

### Player Count
- Single-player only
- The player acts as a spectator / arranger

---

## 2. Match Definition (LOCKED)

> Defines what happens from “Start Battle” to “Battle End”.

### Battlefield
- 2D landscape, viewed from the side
- Size: about three screens, a standard unit should be able to traverse from one end to another in about one minute
- horizontal, left vs right
- One stationary "base" at either end, where units spawn
- One lane, ground and air combined. 
- Camera should show about one "screen" worth of action, scrolling to ensure the middle of the fight is in the centre of the screen at all times
- the middle of the screen is the point between the foremost units of both siden. 
- zoomed enough to see both basic small units and large monstrous units fight at the same time. including air units. they fly a few basic unit heights worth of distance above the ground. 
- zoom is stationary and doesnt change.

### Armies
- Each side fields an army defined by:
  - Unit pool
  - Randomized spawning 
  - Escalation tiers

### Escalation
- Armies unlock stronger unit tiers over time
- Expected number of tiers: 4
- - Over time, the randomizer gains more available "power", allowing stronger units to spawn more frequently and/or increasing overall spawn rate


### Battle Flow
1. Player selects two armies
2. Battle starts automatically
3. Units spawn at base and advance towards the enemies base
4. Units collide and fight
5. Escalation unlocks stronger units
6. One side eventually wins, by reaching the enemy base and reducing its hitpoints to 0

### End Condition
- When one of the two armies base hitpoints is reduced to 0

---

## 3. Systems (DRAFT)

### Spawning System
- Randomized unit selection from currently unlocked tiers
- Symmetric rules but could be changed later
- Each army has their own randomizer, but they are identical unless otherwise specified. 
- based on a cool visualization at the top of the screen for each randomizer, that for each determines which units spawn. more powerful units require more "energy" to spawn. 
- Randomizer visualization is informational only and not interactive


### Combat Resolution
- Simple physical interactions
- Emphasis on visual clarity and chaos
- Units attack when their cooldown reaches 0 and a target is in range.
- On attack, they apply Damage once and reset cooldown to (1 / AttackRate).

### Formation & Vanguard Mechanics
Goal: Improve readability by giving unit types distinct spacing patterns at the frontline without changing combat rules.

Rules:
- Each unit type defines `FormationSpacingMul` (baseline ally spacing multiplier).
  - < 1.0 = tighter formation
  - > 1.0 = looser spacing
- Each unit type may define a vanguard:
  - `VanguardDepth` = number of closest units of that type to the frontline that use vanguard spacing
  - `VanguardSpacingMul` = spacing multiplier for those vanguard units (defaults to FormationSpacingMul)
- Vanguard selection is positional and recomputed each sim step.
- Vanguard clusters are homogeneous per unit type (no mixing types into a single vanguard count).
- Vanguard only affects ally spacing; combat and targeting rules are unchanged.

### Terrain
- Deformable terrain under consideration
- Must remain readable and performant

### Matchup Analyzer (Headless Simulation)
Goal: Allow fast balancing by simulating many battles without visualization and summarizing outcomes.

- Runs N simulations of the current Army A vs Army B setup in "headless" mode (no rendering).
- Uses the same core simulation rules as the visual game where possible.
- Supports deterministic runs via RNG seed (reproducible results).
- Start with a CLI analyzer, integration into in game menu later

Outputs (per matchup):
- Win rate per side
- Avg / median time-to-win
- Avg winner base HP remaining (and distribution buckets)
- Stomp indicator (e.g., % wins with winner base HP > 70%)
- Variance / swinginess metrics (time-to-win, base HP remaining)

Notes:
- Headless mode may optionally enable a simplified "fast mode" for collisions/terrain if needed for performance.

---

## 4. Content (DRAFT)

### Units
- Pixel-art soldiers
- Clear silhouettes
- Increasing power per tier
- modular system that makes adding or adjusting armies easy

### Armies / Factions
- Player selects predefined armies
- Armies differ by:
  - Unit composition
  - Tier emphasis

---

## 5. Technical Notes (DRAFT)

- 2D rendering
- Deterministic simulation preferred
- No player input during simulation
- Performance must support many units on screen

---

## 5.5 Architecture (DRAFT)

Goal: Keep the project modular and scalable as content and complexity grows, with strict separation between simulation logic, content definitions, visualization, and analysis tooling.

### Layers

#### 1) SimCore (Deterministic Battle Simulation)
Responsibilities:
- Fixed-timestep simulation (stepper)
- Unit movement, blocking/frontline behavior
- Targeting and combat resolution
- Spawning and escalation
- Status effects and ability execution
- Deterministic RNG (seeded) and stable iteration order

Rules:
- Pure C# logic: no Godot types (`Node`, `Vector2`, etc.)
- No file IO, no rendering, no player input
- Same simulation must be usable for both visual gameplay and headless analyzer

Outputs:
- `BattleState` fully describing the battle at any point in time
- Optional event stream (spawn/hit/death/base damage) for visuals and analytics

#### 2) Content (Armies / Units / Abilities as Data)
Responsibilities:
- Define armies, units, tiers, stats, and abilities
- Allow large numbers of armies and quick iteration without recompiling SimCore (target state)

Rules:
- Content is declarative (data-driven), not hard-coded logic
- All content items have stable string IDs (units, abilities, armies)
- Abilities are chosen from a finite set of supported ability types

#### 3) Hosts (Ways to Run the Simulation)
Two primary hosts run the same SimCore:
- Game host (Godot runtime): runs simulation + renders the battle
- Analyzer host (CLI/headless): runs N simulations fast with no rendering, outputs statistics

#### 4) Presentation (Visualization / UI)
Responsibilities:
- Render units, terrain, bases, and effects from `BattleState`
- Camera behavior (follow midpoint of frontline)
- Non-interactive randomizer visualization (informational only)
- Menus and army selection (battle setup only)

Rules:
- Presentation never decides battle outcomes
- No gameplay logic in rendering/UI code

### Abilities Model (Stats + Limited Ability Types)
Approach:
- Units are primarily differentiated by stats and a limited set of ability types (e.g., Cleave, Projectile, Pierce/Reach, OnDeathExplode, Aura, Stun, etc.)
- Ability types have clear triggers (OnSpawn, OnAttack, OnHit, OnDeath, AuraTick) and deterministic behavior
- Adding new ability types is done in SimCore code; content selects from supported types

Rationale:
- Enables “wildly different units” while remaining deterministic, testable, and analyzable
- Supports the headless analyzer and balancing workflow

### Target folder structure (inside the Godot project)
- `Scripts/SimCore/` — deterministic battle logic only
- `Scripts/Content/` — data definitions and loaders (later: JSON/resources)
- `Scripts/GameHost/` — runtime glue, menus, camera, sim stepping
- `Scripts/Presentation/` — rendering code (units, battlefield, effects)
- `Scripts/Analyzer/` — CLI/headless analyzer entry and stats reporting


## 6. Open Questions & Risks

- Optimal number of simultaneous units
- Terrain deformation performance in web
- Escalation pacing
- Visual readability at scale
- Define what "exciting matchup" means in measurable terms (win-rate band, stomp-rate ceiling, target battle duration).
- Headless simulation fidelity vs performance tradeoffs (especially terrain deformation).

---
## Project Snapshot (Living Section)

### Current Architecture & Assumptions

**High-level structure**
- **SimCore**
  - Deterministic, headless-capable battle simulation
  - No Godot types or rendering logic
  - Owns: units, combat resolution, spawning, escalation, base HP
- **Presentation**
  - Purely visual interpretation of SimCore state and events
  - Owns: camera, hit reactions, death effects, damage numbers, tracers, particles
  - Visual randomness is allowed; simulation outcomes must not change
- **GameHost**
  - Orchestrates SimCore stepping and forwards events to Presentation
  - Bridges sim time ↔ frame time
- **Analyzer**
  - Runs SimCore headlessly for many iterations
  - Produces statistics (wins, draws, timeouts, stomp rate, etc.)
  - Must stay fast and avoid excessive logging

**Core assumptions**
- The player is a spectator (“god view”); no interaction during battles
- Battles are expected to resolve eventually via escalation and line-breakers
- Visual clarity and spectacle are prioritized over realism
- All gameplay logic must be testable in headless analyzer mode
- Presentation must never affect simulation results

---

### Non-Goals (Explicit)

- No micromanagement, unit control, or base building
- No fog of war
- No multiplayer
- No real physics simulation (ragdolls, rigid bodies, etc.)
- No attempt at historical realism or balance symmetry
- No requirement for competitive fairness (this is a simulator/spectacle)

---

### Current Known Issues / Risks

- **Stalemates**: Dense tier-1 melee formations can still stall indefinitely if higher tiers do not appear or are too weak
- **Escalation visibility**: Need reliable confirmation (via debug readout) that tiers 2–4 unlock and spawn as intended
- **Balance immaturity**: Current armies are placeholders; analyzer shows frequent draws under timeout
- **Performance sensitivity**: Excessive debug logging can significantly slow analyzer runs
- **Content coupling**: Some demo content assumptions are still hardcoded rather than data-driven

---

### Next 3 Planned Milestones

1. **Reliable Escalation & Debug Visibility**
   - Time-based tier unlocking (tiers 2–4)
   - Tier-weighted spawning
   - Periodic debug readout (units per tier, base HP, sim time)

2. **Line-Breaker Mechanics**
   - Ensure higher tiers actively break stalemates:
     - Reach units
     - Ranged units
     - Cleave / AoE / on-death effects
   - Strong, readable visuals for these effects

3. **Content Scalability**
   - Move armies and unit definitions to data (e.g. JSON or resources)
   - Enable rapid creation of many visually and mechanically distinct armies
   - Keep SimCore generic and content-agnostic

---


## 7. Change Log
- 2026-01-18: Aligned project folders with architecture layers
- 2026-01-17: Added headless matchup analyzer for balancing/statistics
- 2026-01-17: Added more detail
- 2026-01-16: Created initial GDD
- 2026-01-16: Locked Core Pillars

## 8. References
- game_mechanics.md - repository for more detailed game mechanics description
