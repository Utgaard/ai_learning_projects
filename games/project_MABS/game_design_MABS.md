# Game Design Document: Project MABS (Modular Army Battle Simulator)

**Version:** 1.0  
**Genre:** 2D Side-Scrolling Auto-Battler / Simulation  
**Perspective:** 2D (Side View)  
**Engine:** Unity (Recommended for Physics/ScriptableObjects)

---

## 1. Executive Summary
**Project MABS** is a modular battle simulator where "wildly different" armies—ranging from mythological creatures to sci-fi robots—clash in physics-driven warfare. 

The core hook is the combination of strategic army selection with a **"Pachinko-style" physics economy**: units are spawned based on falling "Power Orbs" landing in randomized buckets. The game is designed primarily as a spectator experience, featuring a cinematic "Director AI" camera and an automated Tournament Mode.

---

## 2. Core Gameplay Loop

### 2.1 The Flow
1.  **Setup:** User selects two armies (e.g., "Greeks" vs. "Aliens") or initiates a Tournament.
2.  **Simulation:** The game loads the battle scene.
3.  **Economy:** "Power Orbs" drop from the top of the screen into physics buckets.
4.  **Spawning:** When a bucket fills (Energy > Unit Cost), a unit spawns.
5.  **Combat:** Units move linearly (Left <-> Right), engage enemies, and attempt to destroy the opposing base.
6.  **Resolution:** One base is destroyed. The winner is recorded.

### 2.2 Win Condition
* **Primary:** Reduce the Enemy Base HP to 0.
* **Base Defense Mechanism:** The Base has "Stages" (e.g., 66%, 33%). When a threshold is crossed, the Base emits a **Force Wave**, physically knocking back all nearby enemies to reset the frontline.

### 2.3 User Interaction
* **Primary:** Passive Observation.
* **Secondary:** Camera panning (optional override).
* **Meta-Game:** Configuring Army matchups and seeding Tournaments.

---

## 3. Systems Architecture

### 3.1 Data-Driven Armies (The "Modular" Core)
All armies are defined via Data Files (ScriptableObjects/JSON), separating logic from visuals.

**Structure: `ArmyDefinition`**
* **ID:** Unique Name (e.g., "Undead_Legion")
* **BasePrefab:** Visual model for the HQ.
* **BaseConfig:** HP totals and Pushback Force settings.
* **SpawnerConfig:** Defines the layout of the physics buckets (e.g., "Swarm armies have larger buckets for low-tier units").

**Structure: `UnitDefinition`**
* **Stats:** HP, Move Speed, Attack Damage, Range, Attack Rate.
* **Cost:** Energy required to spawn.
* **Tier:** 1 (Common) to 4 (Legendary).
* **Physics:** Mass, Drag, Collider Size (allows for Giants vs. Insects).
* **VisualAdapterID:** References the specific Prefab/Animator (see 4.2).

### 3.2 The Dual-Mode Spawner system
To allow for both "Visual Spectacle" and "Rapid Balancing," the spawning system has two operating modes.

**Mode A: Physics (Visual)**
* **Mechanism:** Physical spheres ("Power Orbs") spawn at the top and bounce through a pegged obstacle course.
* **Randomness:** Chaos is determined by the physics engine (bounces, collisions).
* **Logic:** Sphere lands in "Tier 2 Bucket" -> Add Energy. If Bucket Full -> Spawn Unit.

**Mode B: Headless (Simulation)**
* **Mechanism:** No rendering, no physics.
* **Calibration:** Uses a pre-calculated **Probability Table** derived from running Mode A 10,000 times.
* **Logic:** Every `DropInterval`, the engine rolls a random number against the Probability Table to instantly award Energy.
* **Purpose:** Allows simulating 1,000 battles in seconds to test win rates.

**The Resource Curve:**
* Drop rate increases over time (`BaseRate + (Time * Acceleration)`).
* Applies symmetrically to both players to ensure fairness.

### 3.3 Battle Logic & AI
* **Lane Logic:** Units exist on a 2D plane. Movement is restricted to the X-axis.
* **State Machine:**
    1.  `Move`: Walk forward.
    2.  `Scan`: Raycast for `TargetLayer` within `Range`.
    3.  `Attack`: Stop moving, play animation, deal damage.
    4.  `Die`: Trigger **Ragdoll Switch**.

### 3.4 The Ragdoll Switch
Transitioning from "Character" to "Debris."
* **Alive:** `Animator` is active. `Rigidbody` is Kinematic (controlled by code).
* **Dead:** `Animator` disabled. `Rigidbody` becomes Dynamic (controlled by physics).
* **Effect:** Corpses pile up, can be pushed by living units, and add weight to the battle.

---

## 4. Visuals & Presentation

### 4.1 Art Style
* **Environment:** 2D Parallax backgrounds.
* **Units:** 2D Skeletal Meshes (Spine/Puppet) or 2D Sprites.
* **View:** Orthographic Side-Scroller.

### 4.2 Visual Adapters
A code interface (`IVisualAdapter`) that creates a layer between the Game Logic and the Art.
* *Example:* The Logic calls `Attack()`.
    * *Adapter A (Knight):* Triggers "SwingSword" animation.
    * *Adapter B (Tank):* Triggers "FireCannon" particle effect and recoil tween.

### 4.3 The "Director" (Camera AI)
Automated camera framing.
* **Centroid Tracking:** Calculates the midpoint between the two frontlines.
* **Dynamic Zoom:** Zooms in when armies clash; Zooms out when distance increases.
* **Action Bias:** If a Base is taking damage, the camera locks to the Base.

---

## 5. Game Modes

### 5.0 Single Battle Mode
* **Input:** User selects 2 Armies.
* **Visualization:** Loads battle screen with the chosen armies, simulate battle, show victory screen with the victorious Army.
  
### 5.1 Tournament Mode
* **Input:** User selects 4, 8, or 16 Armies.
* **Visualization:** A Bracket Tree UI shows the hierarchy.
* **Loop:**
    1.  Auto-generates matches.
    2.  Loads Battle Scene for Match 1.
    3.  Simulates to Win/Loss.
    4.  Returns to Bracket UI, advances Winner.
    5.  Repeats until Champion is crowned.

### 5.2 Balance Sandbox (Headless)
* **Input:** Select Army A and Army B. Set "Iterations" (e.g., 100).
* **Process:** Runs simulation at max timescale (no rendering).
* **Output:** A CSV/Log showing Win Rate, Average Match Time, and Unit Spawn Counts.

---

## 6. Technical Requirements
* **Physics Engine:** 2D Physics (Box2D or equivalent).
* **Performance Target:** Support for 100+ active units (50 vs 50) + 50 active Ragdolls.
* **Optimization Strategy:**
    * Object Pooling for Units and Projectiles.
    * Ragdoll "Freezing" (turning kinematic) after static duration.
    * Sprite Atlasing to reduce Draw Calls.
