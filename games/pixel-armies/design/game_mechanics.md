## Formation & Vanguard Mechanics

This section defines how units cluster and pack together on the battlefield.  
The intent is to support visually and mechanically distinct armies (disciplined blocks, swarms, elites) without player micromanagement.

---

### Formation (Baseline Spacing)

Each **unit type** defines how tightly it packs with allied units in general movement and combat.

**FormationSpacingMul** (float, default = `1.0`)  
Multiplier applied to the unit’s baseline spacing radius when calculating ally spacing.

- `< 1.0` → tighter packing (disciplined, swarm-like)
- `> 1.0` → looser spacing (large, chaotic, elite)

Formation spacing applies everywhere on the battlefield unless overridden by Vanguard rules.

---

### Vanguard (Frontline Clustering)

Some unit types are capable of forming a **vanguard**: a tightly packed, multi-unit-deep cluster at the very front of the formation.

Vanguard behavior is defined **per unit type**, not globally.

Each unit type may define:

- **VanguardDepth** (integer, default = `0`)  
  Maximum number of units of this type that may participate in the vanguard simultaneously.
  - `0` → unit type cannot form a vanguard
  - Typical values: `3–10`

- **VanguardSpacingMul** (float, default = `FormationSpacingMul`)  
  Additional spacing multiplier applied **only while the unit is part of the vanguard**.
  - Usually `< FormationSpacingMul`
  - Enables very tight “shield wall” or “wedge” effects

---

### Vanguard Eligibility Rules

To maintain clarity, determinism, and visual coherence:

1. **Vanguards are homogeneous**
   - Only units of the **same unit type** may form a vanguard together
   - Mixed-type vanguards are not allowed

2. **Vanguard membership is positional**
   - For each side and each unit type:
     - Units closest to that side’s frontline are considered first
     - Up to `VanguardDepth` units may be marked as vanguard

3. **Vanguard is dynamic**
   - Units may enter or leave vanguard status as the frontline shifts
   - No permanent state or memory is required beyond current positions

Other unit types may advance alongside or behind a vanguard, but do not contribute to its depth.

---

### Interaction with Combat & Spacing

- Vanguard affects **ally spacing only**
- Enemy blocking, contact, reach, cleave, and other combat mechanics apply normally
- Vanguard does **not** imply invulnerability or special combat rules by itself

This ensures:
- Vanguard increases density and pressure, not guaranteed breakthrough
- Line-breaking still relies on reach, ranged units, AoE, or abilities

---

### Design Rationale

This model enables:

- Large disciplined blocks (phalanx, shield wall)
- Swarms with no structured front
- Mixed armies where:
  - One unit type forms a dense vanguard
  - Other unit types flow around or behind it

The system remains:
- Deterministic
- Data-driven
- Compatible with headless simulation and analyzer tooling

---

### Explicit Non-Goals

- No player control over formation or vanguard during battle
- No dynamic formation commands
- No mixed-type vanguard clustering

---

### Sanity Examples

- **Disciplined Infantry**
  - FormationSpacingMul = `0.9`
  - VanguardDepth = `8`
  - VanguardSpacingMul = `0.6`

- **Swarm Creature**
  - FormationSpacingMul = `0.7`
  - VanguardDepth = `0`

- **Elite Monster**
  - FormationSpacingMul = `1.4`
  - VanguardDepth = `0`

---

### Balance Note

Increasing vanguard depth increases frontline density and may raise stalemate risk.  
Battle resolution is expected to rely on:
- Reach weapons
- Ranged units
- Cleave / AoE abilities
- Escalation pressure

## Unit Movement & Targeting (Extension)

### Movement Class

Each unit has a **MovementClass** that defines how it interacts with allies and enemies during movement.

**MovementClass values:**
- **Ground**
  - Respects ally formation, spacing, and vanguard rules
  - Forms and maintains frontlines
  - Can be blocked by allied units
- **Air**
  - Ignores ally blocking and formation
  - Flies past allied ground units freely
  - Still respects enemy engagement distance (stops to attack when in contact)
  - Visually represents aerial or highly mobile units

**Design intent:**
- Air units act as interceptors, raiders, or line-bypass units
- Ground units define the main battle line and density
- Air units increase vertical and tactical variety without adding complex physics

MovementClass affects **movement and spacing only**; combat resolution rules remain unchanged.

---

### Targeting Policy

Each unit has a **TargetingPolicy** that determines how it selects which enemy to attack.

Targeting policies are deterministic and evaluated every time a unit selects or updates its target.

**Initial TargetingPolicy values:**

- **Frontmost**
  - Targets the first enemy encountered in the unit’s advance direction
  - Left-side units target the enemy with the smallest X position
  - Right-side units target the enemy with the largest X position
  - Default for air units and frontline attackers

- **ClosestInRange**
  - Targets the closest enemy that is currently within attack range
  - If no enemies are in range, movement continues toward the enemy side

- **Closest**
  - Targets the nearest enemy by distance
  - Used primarily to decide movement direction
  - May be combined with other policies in future extensions

**Design intent:**
- TargetingPolicy defines *preference*, not randomness
- Different unit types can feel tactically distinct without micromanagement
- The system is intentionally simple now but extensible later

---

### Future Targeting Extensions (Planned, Not Implemented)

The targeting system is designed to support future extensions such as:
- Prefer low-HP targets
- Prefer ranged or air units
- Prefer highest tier units
- Prefer base over units (siege or suicide units)

These extensions may use scoring-based selection but are explicitly **out of scope** for the current phase.

---

### Interaction Between Movement and Targeting

- Air units typically use:
  - `MovementClass = Air`
  - `TargetingPolicy = Frontmost`
- Ground ranged units may use:
  - `MovementClass = Ground`
  - `TargetingPolicy = ClosestInRange`

This separation allows:
- Clean mental models
- Clear visual storytelling
- Independent tuning of movement and combat behavior

---

### Non-Goals (Reiterated)

- No manual target selection by the player
- No per-unit AI scripting
- No physics-based collision for air units
- No pathfinding beyond lane-based advance

All behavior must remain deterministic and compatible with headless simulation.
