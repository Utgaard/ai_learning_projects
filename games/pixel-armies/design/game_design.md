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
- `BattleState` that fully describes the battle at any point in time
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

### Target Folder Structure (within Godot project)
- `Scripts/SimCore/` — deterministic battle logic only
- `Scripts/Content/` — data definitions and loaders (later: JSON/resources)
- `Scripts/GameHost/` — Godot runtime glue, menus, camera, render controllers
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

## 7. Change Log
- 2026-01-17: Added headless matchup analyzer for balancing/statistics
- 2026-01-17: Added more detail
- 2026-01-16: Created initial GDD
- 2026-01-16: Locked Core Pillars
