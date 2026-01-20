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
