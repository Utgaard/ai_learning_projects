# Project MABS - Product Requirements Document (MVP)

**Version:** 1.0  
**Status:** Approved for Development  
**Target Platform:** PC (Windows/Mac/Linux)  
**Engine:** Unity 6 (LTS)

---

## 1. Executive Summary
**Project MABS (Modular Army Battle Simulator)** is a physics-driven auto-battler where players configure armies and watch them fight in a side-scrolling tug-of-war.

**The Goal of this MVP** is to build a functional **"Vertical Slice"** that proves the three hardest technical pillars of the project:
1.  **The Physics Economy:** Visualizing unit spawning via a "Pachinko" physics machine.
2.  **Modular Architecture:** Running two wildly different armies (e.g., "Swarm" vs. "Giants") using the same code.
3.  **Headless Simulation:** Running the exact same battle logic without graphics at high speed for automated balancing.

---

## 2. Success Metrics
The MVP will be considered successful if:
* **Functionality:** A full game loop (Spawn -> Fight -> Win) completes without user intervention.
* **Performance:** The simulation handles 100 simultaneous units at >60 FPS.
* **Validation:** The "Headless Mode" runs via command line and outputs a text log declaring a winner within 5 seconds of simulation time.
* **Modularity:** We can create a new unit type (e.g., a "Tank") solely by creating a new ScriptableObject, without writing C# code.

---

## 3. Feature Scope (MVP)

### 3.1 Core Gameplay Loop (Must-Have)
* **1v1 Linear Battle:** Units spawn at left/right bases and move toward the opposing base.
* **Automated Combat:** Units automatically detect enemies within range, stop, and attack until the target dies.
* **Win Condition:** The game ends when a Base's HP reaches 0.
* **Economy:** Resources ("Power Orbs") drop automatically over time. Buckets collect orbs to fund unit spawns.

### 3.2 Visuals & Physics (Must-Have)
* **The "Ragdoll Switch":** Units must transition from animated sprites to dynamic physics objects upon death.
* **The Visual Spawner:** Physical spheres must be seen falling through pegs into buckets to determine spawn timing.

### 3.3 The "Headless" System (Must-Have)
* **No-Graphics Mode:** A build flag that disables rendering, UI, and audio.
* **Math Spawner:** A fallback logic system that simulates the drop rates of the physics spawner using probability tables (to allow fast-forwarding).

### 3.4 Out of Scope (Do NOT Build)
* Tournament / Bracket Visualization.
* Complex Audio / Sound Effects.
* Main Menu / Lobby (Game boots directly into battle).
* Base "Pushback Wave" mechanics (bases are static for now).
* Advanced Abilities (AOE, Status Effects, Flying units).

---

## 4. User Stories

| ID | As A... | I Want To... | So That... |
| :--- | :--- | :--- | :--- |
| **US-1** | **Designer** | Create a new Army by editing a file | I can test "Aliens vs. Knights" without asking a coder to help. |
| **US-2** | **Player** | See balls falling into buckets | I understand why my units are (or aren't) spawning. |
| **US-3** | **Player** | See bodies pile up on the battlefield | The battle feels chaotic and the physics affect the frontline. |
| **US-4** | **Developer** | Run 100 battles in 1 minute (Headless) | I can mathematically prove if Army A is stronger than Army B. |

---

## 5. Technical Constraints & Risks
* **Deterministic Physics:** Unity's physics engine is not 100% deterministic across different devices. *Mitigation:* The "Headless Mode" will use a statistical approximation (Probability Tables) rather than trying to force physics determinism.
* **Batchmode Support:** Ensure all "View" logic (Sprites, Audio) is wrapped in null checks or `#if !UNITY_SERVER` guards to prevent crashes in Headless mode.

# Project MABS - Development Backlog (MVP)

**Version:** 1.0  
**Status:** Ready for Development  
**Tech Stack:** Unity 6 (LTS), C#

---

## Phase 1: The "Grey Box" Foundation
**Focus:** Data Architecture and Core Simulation Logic (No Graphics).

### [T-1.1] Project Initialization & Repository Setup
* **Goal:** As a developer, I want a clean Unity project environment with proper version control so that the team can collaborate without merge conflicts or lost data.
* **Constraints:**
    * Unity Version: **6 (LTS)**.
    * Template: **2D Core**.
    * Must use a standard `.gitignore` specifically for Unity.
* **Definition of Done:**
    * [ ] Project opens in Unity 6 without errors.
    * [ ] Folder structure established (`_Scripts`, `_Data`, `_Prefabs`, `_Scenes`, `_Art`).
    * [ ] `.gitignore` is active (ignoring `/Library`, `/Temp`, `.csproj` files).
    * [ ] Project initialized as a Git repository and pushed to remote.

### [T-1.2] Implement Data Architecture (ScriptableObjects)
* **Goal:** As a designer, I want to define `Armies` and `Units` in the Inspector so that I can create "wildly different" factions without writing new code.
* **Constraints:**
    * Must use `ScriptableObject` inheritance.
    * Fields must be serialized and editable in the Editor.
* **Definition of Done:**
    * [ ] `UnitDefinition.cs` created with fields: `ID`, `Health`, `MoveSpeed`, `AttackDamage`, `AttackRange`, `Cost`.
    * [ ] `ArmyDefinition.cs` created with fields: `ArmyName`, `BaseHealth`, `List<UnitDefinition>`.
    * [ ] Two dummy data assets created (e.g., "Soldier_Data" and "Human_Army_Data") to verify they save/load correctly.

### [T-1.3] The Unit Controller (Logic Layer)
* **Goal:** As a developer, I want a unit that can move and detect enemies using logic only (no visuals) so that the simulation can run in Headless mode.
* **Constraints:**
    * **Strict Separation:** This class must NOT reference `SpriteRenderer`, `Animator`, or `AudioSource`.
    * Must use a simple State Machine pattern (States: `Idle`, `Move`, `Attack`, `Die`).
    * Must use `Physics2D.Raycast` (or LayerMask checks) for detection.
* **Definition of Done:**
    * [ ] Unit moves along the X-axis at `MoveSpeed` (using `transform.Translate` or `Rigidbody.velocity`).
    * [ ] Unit stops moving when it detects a collider on the `TargetLayer`.
    * [ ] Unit resumes moving if the target disappears (is destroyed).
    * [ ] Unit destroys itself (Despawn) if its internal `Health <= 0`.

### [T-1.4] The Lane Manager (Optimization)
* **Goal:** As a system, I want to track all active units efficiently so that units don't have to scan the entire world to find a target (Performance).
* **Constraints:**
    * Must use a Singleton or Dependency Injection pattern to be accessible globally.
    * Must maintain separate lists for `TeamA` and `TeamB`.
* **Definition of Done:**
    * [ ] `LaneManager` script maintains two `List<UnitController>`.
    * [ ] Units register themselves to the list `OnEnable`.
    * [ ] Units remove themselves from the list `OnDisable`.
    * [ ] Debug Log accurately prints the count of units on the field as they spawn and die.

---

## Phase 2: The "Feel" (Visuals & Physics)
**Focus:** Connecting the Simulation to the Player Experience.

### [T-2.1] The Visual Adapter Pattern
* **Goal:** As a developer, I want to attach a visual representation to the logic **only if** the game is not running in Headless mode.
* **Constraints:**
    * Script `UnitView.cs`.
    * Must subscribe to C# Actions from `UnitController` (`OnAttack`, `OnMove`, `OnDeath`).
    * Must handle missing components gracefully (e.g., if Animator is null).
* **Definition of Done:**
    * [ ] `UnitView` successfully plays an animation when `UnitController` enters the Attack state.
    * [ ] `UnitView` successfully flips the sprite (Scale X -1) based on team direction.
    * [ ] If `UnitView` is removed from the GameObject, the `UnitController` still functions (logic continues).

### [T-2.2] Ragdoll Implementation (The "Switch")
* **Goal:** As a player, I want units to crumble into physics objects upon death so that battles feel impactful and chaotic.
* **Constraints:**
    * **Mechanism:** Transition from Kinematic (Script-driven) to Dynamic (Physics-driven).
    * Must allow "Force" to be applied on death (e.g., knockback).
* **Definition of Done:**
    * [ ] Unit functions normally while alive (Animator driving visuals).
    * [ ] Upon `Health <= 0`, `Animator` is disabled.
    * [ ] `Rigidbody2D` switches from Kinematic to Dynamic.
    * [ ] Dead unit falls to the ground and reacts to gravity/collision.

### [T-2.3] Physics Spawner (The "Pachinko" Machine)
* **Goal:** As a player, I want to see resources falling into buckets so that the random nature of the army generation is visualized.
* **Constraints:**
    * Uses Unity 2D Physics (Circle Colliders).
    * Buckets must be Trigger Colliders.
* **Definition of Done:**
    * [ ] "Power Orb" prefab created with Bouncy Physics Material.
    * [ ] `BucketTrigger` script detects Orb entry and adds value to a local variable `CurrentEnergy`.
    * [ ] When `CurrentEnergy >= UnitCost`, the Bucket fires a `SpawnUnit` event and resets/deducts energy.

---

## Phase 3: The Headless Simulation
**Focus:** Speed and Data Validation.

### [T-3.1] The Probability Spawner (Math Logic)
* **Goal:** As a developer, I want a non-physics spawner that mimics the statistical outcome of the Pachinko machine so that I can run simulations instantly.
* **Constraints:**
    * Must **not** instantiate any "Orb" GameObjects.
    * Must use `Random.Range` and a configurable `ProbabilityTable`.
* **Definition of Done:**
    * [ ] `ProbabilitySpawner` script created.
    * [ ] Script triggers `SpawnUnit` events at the correct `Interval`.
    * [ ] The frequency of Tier 1 vs Tier 4 spawns roughly matches the input percentages (e.g., 50/30/15/5) over 100 log trials.

### [T-3.2] The Headless Bootstrapper
* **Goal:** As an automated system, I want to detect if the game is running in batchmode and configure it for speed.
* **Constraints:**
    * Check `Application.isBatchMode`.
* **Definition of Done:**
    * [ ] `GameManager` script created.
    * [ ] If Batchmode detected: `PhysicsSpawner` is disabled, `ProbabilitySpawner` is enabled.
    * [ ] If Batchmode detected: Main Camera is disabled (optimization).
    * [ ] If Batchmode detected: `Time.timeScale` is set to 50.0f (or max stable value).

---

## Phase 4: Game Loop & MVP Polish
**Focus:** Making the game winnable.

### [T-4.1] Base Logic & Win Condition
* **Goal:** As a player, I want to destroy an enemy base to win the match.
* **Constraints:**
    * Base is a static object with High HP.
    * Must detect when HP reaches 0.
* **Definition of Done:**
    * [ ] Base Prefab created (Placeholder art acceptable).
    * [ ] Base takes damage from Units.
    * [ ] When Base HP <= 0, Game pauses and logs "Winner: [Team Name]".

### [T-4.2] First Content Pass (2 Armies)
* **Goal:** As a user, I want to see two different strategies play out to verify the balance tools.
* **Constraints:**
    * Create data for "Army Red" (Swarm/Fast/Weak).
    * Create data for "Army Blue" (Tank/Slow/Strong).
* **Definition of Done:**
    * [ ] Both armies are playable.
    * [ ] Matches last between 45-90 seconds on average (tuning spawn rates).
