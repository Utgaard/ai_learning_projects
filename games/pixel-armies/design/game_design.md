# Pixel Armies — Game Design Document

Version: 0.1  
Status: Early Concept  
Audience: Internal (Developer / Family Project)

---

## 1. Core Pillars (LOCKED)

### Game Fantasy
The player feels like a god, selecting opposing armies and watching fate, physics, the features of the armies and randomness decide the outcome.

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
- Primary target: Web (HTML5)
- Secondary target: Windows
- Design bias toward simple, fast-to-implement features

### Player Count
- Single-player only
- The player acts as a spectator / arranger

---

## 2. Match Definition (DRAFT)

> Defines what happens from “Start Battle” to “Battle End”.

### Battlefield
- 2D landscape, viewed from the side
- Size: about three screens, a standard unit should be able to traverse from one end to another in about one minute
- horizontal, left vs right
- One stationary "base" at either end, where units spawn
- One lane, ground and air combined. 

### Armies
- Each side fields an army defined by:
  - Unit pool
  - Randomized spawning 
  - Escalation tiers

### Escalation
- Armies unlock stronger unit tiers over time
- Expected number of tiers: 4
- Randomizer produce more "power" over time, increasing spawn rate

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

### Combat Resolution
- Simple physical interactions
- Emphasis on visual clarity and chaos

### Terrain
- Deformable terrain under consideration
- Must remain readable and performant

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

## 6. Open Questions & Risks

- Optimal number of simultaneous units
- Terrain deformation performance in web
- Escalation pacing
- Visual readability at scale

---

## 7. Change Log
- 2026-01-17: Added more detail
- 2026-01-16: Created initial GDD
- 2026-01-16: Locked Core Pillars
