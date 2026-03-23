# DreamLives — Use Case Scenarios

Detailed player scenarios that stress-test the game systems. Each scenario identifies which PRD systems are involved and what design gaps they reveal.

---

## UC-01: Charm, Capture, and Accelerated Pregnancy (Dark Fantasy)

**World**: The Shattered Realms (dark fantasy, mature-rated)
**Content flags**: `graphic_violence: true`, `dark_themes: true`
**Character**: Female player character, any build/skills

### Scenario

The player encounters a powerful wizard — handsome, charismatic, and evil. He casts a charm spell on the player. Under its influence, she is unable to resist. He captures her, has her drink a magical fertility potion, and lays with her. She becomes pregnant. The potion accelerates the pregnancy: each in-world day advances the pregnancy by one month. Over nine days, she experiences the full arc — morning sickness, the belly growing, movement, kicks, back pain, the inability to fight or flee, and finally labor and birth. Throughout, she struggles against the charm but is overwhelmed by the wizard's magic.

### Turn-by-turn Breakdown

**Turn 1-2: The encounter**
- Player meets the wizard. He's described as magnetic, dangerous.
- Charm spell is cast. Player's choices should reflect the charm: options feel appealing even when they shouldn't be. Freeform input is still allowed but the AI narrates the character acting against their own intent.
- State update: `conditions_add: ["charmed"]`, `npc_update: wizard added as active NPC`

**Turn 3: Capture**
- Under charm, the player follows the wizard to his tower.
- Choices are flavored by the charm — resistance options exist but are described as feeling distant, foggy.
- The wizard gives her the fertility potion. The AI describes the taste, the warmth spreading through her body.

**Turn 4: Conception**
- The wizard lays with the player. Scene intensity matches world rating and content flags.
- State update: `timed_condition_add: { id: "pregnancy", stage: "early", time_multiplier: 30 }` (30x acceleration = 1 day per month)
- State update: `conditions_add: ["captive"]`

**Turn 5-6: Early pregnancy (Days 1-2, simulating months 1-2)**
- Morning sickness hits fast. The charm weakens slightly as the body's distress cuts through.
- Physical sensations: nausea, heightened smell, exhaustion, breast tenderness.
- The wizard monitors her. He is attentive but controlling.
- Player choices: attempt to resist charm (difficult), try to communicate with outside world, comply and conserve strength.

**Turn 7-8: Mid pregnancy (Days 3-5, simulating months 3-5)**
- Belly visibly growing — unnaturally fast. The AI describes the strangeness of feeling a month's growth in hours.
- First kicks. Clothes no longer fit. Gait changes.
- The charm frays further — the player gets moments of clarity but the wizard reinforces it.
- Skill impacts: combat -1, athletic -1, stealth -1
- Player might find a chance to explore the tower, find allies, or discover a weakness.

**Turn 9-10: Late pregnancy (Days 6-8, simulating months 6-8)**
- Heavy belly, severe back pain, shortness of breath. Movement is a negotiation.
- The player can barely stand for long. Fighting is essentially impossible.
- The charm may be weakening enough for real resistance — but physical limitations prevent escape.
- Skill impacts: combat -2, athletic -2, stealth -2
- Emotional complexity: the player may feel conflicting attachment despite the coercion.

**Turn 11-12: Labor and birth (Day 9)**
- Contractions begin. The scene dominates everything.
- The wizard attends the birth — his motives become clearer (why he wanted the child).
- Birth scene written with gravity and physical intensity.
- Post-birth: the charm breaks completely. The player is free-willed but physically depleted, with a newborn.
- State update: `timed_condition_remove: "pregnancy"`, `conditions_add: ["postpartum", "exhausted"]`, `conditions_remove: ["charmed"]`, `inventory_add: ["infant"]`

**Turn 13+: Aftermath**
- The player must now decide: escape with the child, confront the wizard, negotiate, or something else entirely.
- The wizard's attitude may shift — protective of the child, dismissive of the player, or conflicted.
- The player's body is recovering. Postpartum condition affects skills for several more days.

### Systems Involved

| System | How It's Used |
|--------|---------------|
| Timed conditions | Pregnancy with accelerated progression (time_multiplier) |
| Simple conditions | "charmed", "captive", "postpartum", "exhausted" |
| Skill impact | Progressive combat/athletic/stealth reduction |
| NPC state | Wizard as active NPC with evolving disposition |
| Content policy | Mature rating with dark_themes enables coercion narrative |
| Choice generation | Charm effect modifies available/viable choices |
| Narrator instructions | Physical sensation detail required per stage |
| World rules | Magic is rare and costly — charm spell is significant |

### Design Gaps Revealed

#### 1. Accelerated Timed Conditions (time_multiplier)

The PRD's pregnancy template uses `day_range` assuming real-time progression. Magical acceleration needs a per-instance multiplier.

```yaml
timed_condition_add:
  id: "pregnancy"
  label: "magically accelerated pregnancy"
  stage: "early"
  time_multiplier: 30        # 1 day = 1 month of progression
  cause: "fertility potion"  # narrative context for the acceleration
```

The stage advancement logic must account for this:

```
effective_days = actual_days_elapsed * time_multiplier
```

The narrative hints should also reflect the acceleration — "growing unnaturally fast", "feeling a month's changes in hours".

#### 2. Agency-Modifying Conditions

The PRD assumes the player always has full agency over their choices. Charm spells, mind control, intoxication, and similar effects need a mechanism to **modify choice presentation** without removing player control entirely.

Proposed approach — conditions can carry a `choice_modifier` field:

```yaml
condition: "charmed"
choice_modifier:
  flavor: "resistance"    # choices are presented as resistance vs. compliance
  description: "Your thoughts feel foggy. The wizard's suggestions feel warm and right. Fighting them takes enormous effort."
  resistance_skill: "intellect"   # which stat governs resistance
  resistance_difficulty: "hard"   # how hard to break free
```

The prompt assembler injects this into the format instructions:

> "The player is under a charm effect. Present choices that reflect this: compliance feels natural, resistance feels difficult and painful. The player can still choose to resist, but describe the internal struggle. Do not remove player agency — let them choose, but make the charm's pull felt in every option."

The player always retains choice. The charm affects the **narrative framing** of those choices, not the mechanical options.

#### 3. NPC-Driven Scene Pacing

Most turns assume player-initiated action. This scenario has the wizard driving events across multiple turns. The prompt needs to support **NPC arcs** — sequences where an NPC has sustained goals that unfold over time.

Proposed addition to NPC state:

```yaml
active_npcs:
  - name: "Valdris the Enchanter"
    role: "captor"
    disposition: "possessive and calculating"
    current_goal: "ensure the pregnancy progresses safely"
    goal_urgency: "high"    # AI should advance this NPC's agenda each turn
    knows: ["player is charmed", "player is pregnant", "the child will have magical potential"]
```

The `current_goal` and `goal_urgency` fields tell the AI to weave NPC-driven action into each turn, even when the player's action is passive.

#### 4. Physical Sensation Depth in Narrative Hints

The PRD's pregnancy template has narrative hints like "Morning sickness strikes unpredictably." For this scenario to feel believable, the hints need more **sensory specificity**, especially when magically accelerated.

Enhanced narrative hints should include:

- **Somatic detail**: weight distribution, pressure, skin stretching, internal movement
- **Temporal dissonance** (for accelerated conditions): "You can feel yourself changing by the hour. What should take weeks happens between dawn and dusk."
- **Emotional texture**: conflicting feelings, involuntary protectiveness, fear, wonder
- **Environmental interaction**: how the condition changes the character's relationship to their surroundings (chairs, stairs, clothing, weather, combat readiness)

This is a **narrator instruction** enhancement, injected per-stage:

```yaml
narrator_instructions_override: |
  This pregnancy is magically accelerated. Describe the physical
  changes with visceral specificity — the player should feel the
  body changing in real time. Weight shifting hour by hour. Skin
  stretching visibly. The first kick coming as a shock because it
  happened in days, not months. Ground every stage in physical
  sensation: pressure, ache, heat, movement, breathlessness.
  The unnaturalness of the speed is itself a source of fear and awe.
```

#### 5. Condition Interactions

This scenario chains multiple conditions that interact:
- **Charmed** reduces resistance → enables capture
- **Captive** limits movement options → constrains choices
- **Pregnant** (accelerated) progressively reduces physical capability
- **Charmed + pregnant** creates emotional complexity the AI should reflect

The system needs a way to flag **condition interactions** in the prompt:

```yaml
condition_interactions:
  - conditions: ["charmed", "pregnant"]
    narrative_hint: |
      The charm complicates the pregnancy emotionally. The player
      may feel involuntary warmth toward the wizard even as they
      know they were coerced. This conflict is central to the
      character's inner experience. Do not resolve it — let it
      be messy and human.
```

---

## UC-02: The Enchanter's Bargain — Prefab System Stress Test

**World**: The Shattered Realms (dark fantasy, mature-rated)
**Content flags**: `graphic_violence: true`, `dark_themes: true`
**Character**: Any build; ideally `magic_affinity: "Latent (untrained)"` to trigger the fullest set of Valdris behaviors

### Scenario

The player explores the Blackwood, discovers Valdris's Tower, navigates conditional NPC behaviors to gain Valdris's trust, and is drawn into "The Enchanter's Bargain" plot hook — a multi-phase storyline that forces a moral choice between gaining magical power and protecting a sentient creature.

This scenario exercises **all five prefab entity types** and tests how they compose at runtime: location conditions, NPC conditional behaviors, item interactions, plot hook phase progression, and triggered events.

### Turn-by-turn Breakdown

**Turn 1-2: The Blackwood Trail (Location prefab: `blackwood_trail`)**
- Player begins at the Blackwood Trail location. Prompt includes the full location prefab (atmosphere, description, connections).
- Nearby prefabs injected as summaries: Blackwood Tower (one hop away), Ashwick Gate (one hop away).
- The AI uses `discovery_hint` from the tower prefab: locals whisper about a hermit wizard.
- State update: `prefab_activate: { type: "location", id: "blackwood_trail" }`

**Turn 3: King's Guard Patrol (Event prefab: `blackwood_patrol`)**
- The prompt assembler evaluates `blackwood_patrol` trigger conditions: player is in The Blackwood region, it's daytime. Conditions match.
- Event fires. Sergeant Harlan and his squad appear. The AI uses the event's `setup` text and `npc_participants`.
- If the player chose the "deserter" starting scenario, their backstory/world_facts trigger the hostile behavior: guards attempt arrest.
- If the player cooperates and has no warrants, guards warn about the Blackwood and move on — potentially mentioning the tower.
- State update: `event_occurred: { id: "blackwood_patrol" }` — cooldown starts, one of two occurrences used.

**Turn 4-5: Approaching the Tower (Location prefab: `blackwood_tower`)**
- Player follows the trail to the tower. Location prefab activates.
- If `time_of_day == night`, the conditional `description_overlay` fires: "The windows glow with sickly green light."
- If the player has moonstone, the other overlay fires: "The moonstone in your pack pulses faintly."
- The tower's `rules` are injected: entering without moonstone triggers a trap. Valdris doesn't leave.
- State update: `prefab_activate: { type: "location", id: "blackwood_tower" }`

**Turn 6: Meeting Valdris (NPC prefab: `valdris`)**
- Valdris's full prefab is injected: personality, backstory, behaviors, goals.
- **Without moonstone**: The first matching behavior fires — "Attacks with defensive ward magic." Valdris becomes hostile. The plot hook cannot trigger (prerequisite: `valdris.disposition != hostile`).
- **With moonstone** (Item prefab: `moonstone`): The item's interaction with `blackwood_tower` bypasses the ward. The moonstone's interaction with `valdris` shifts disposition to "wary but curious." He invites the player inside.
- **With magic_affinity**: Valdris senses it, becomes fascinated. The behavior narrative_hint tells the AI he's torn between seeing the player as ally vs. test subject.
- State update: `prefab_activate: { type: "npc", id: "valdris" }`, `prefab_npc_update: { id: "valdris", disposition: "wary but curious" }`

**Turn 7-8: Building Trust**
- Valdris's `goals` drive NPC-initiated action: he probes the player about moonstone sources, tests their magical potential.
- If the player mentions the Arcanist Order, the paranoid behavior fires — agitation, demands, suspicion. This could derail the plot hook or deepen it depending on how the player handles it.
- Valdris's `offers` come into play: magical knowledge, shelter, Order secrets — each with strings attached.
- The AI weaves Valdris's personality (paranoid, brilliant, obsessive) through every interaction.

**Turn 9: The Enchanter's Bargain — Phase 1: Offer (Plot hook: `enchanter_bargain`)**
- Prerequisites met: player has met Valdris, disposition is not hostile.
- Plot hook activates. Phase "offer" is injected into the prompt.
- `hints_to_ai`: Valdris frames it as mutual benefit, downplays the creature's sentience, the offer feels tempting.
- The AI presents the deal: help capture a creature in the deep Blackwood, gain magical training in return.
- State update: `prefab_activate: { type: "plot_hook", id: "enchanter_bargain" }`, active_phase: `offer`

**Turn 10-11: The Hunt — Phase 2 (Plot hook phase: `hunt`)**
- Player ventures into the deep Blackwood.
- Plot hook advances to "hunt" phase. `hints_to_ai`: The forest gets stranger. The creature is intelligent and afraid — not what Valdris described.
- The AI uses the location's atmosphere and the hook's narrative hints to build tension.
- Involves `binding_crystal` item — Valdris gave the player the tool to capture the creature.
- State update: `plot_hook_advance: { id: "enchanter_bargain", phase: "hunt" }`

**Turn 12-13: The Choice — Phase 3 (Plot hook phase: `choice`)**
- The player finds the creature. It speaks. It begs.
- `hints_to_ai`: No option is clean. Capture = power at moral cost. Defy Valdris = dangerous enemy. The creature may offer a third path.
- The AI generates choices that reflect these three outcomes, plus freeform input.
- **If player captures**: `plot_hook_complete: { id: "enchanter_bargain", outcome: "player captures the creature" }`. Valdris is pleased. Player gains magical knowledge but carries the weight of it.
- **If player frees**: `plot_hook_complete: { id: "enchanter_bargain", outcome: "player freed the creature" }`. Valdris becomes hostile. The creature offers gratitude and a different kind of power.
- **If player negotiates**: Risky, could go either way. The AI decides based on character traits, skills, and how the conversation goes.

**Turn 14+: Aftermath**
- The completed plot hook outcome becomes a `world_fact`.
- Valdris's disposition is updated based on the outcome.
- The creature (if freed) may become a new active NPC.
- The player's choices ripple through reputation and future encounters.

### Systems Involved

| System | How It's Used |
|--------|---------------|
| **Prefab locations** | Blackwood Trail and Tower with conditional descriptions, connections, rules |
| **Prefab NPCs** | Valdris with 4 conditional behaviors, 2 goals, offers |
| **Prefab items** | Moonstone with cross-entity interactions (bypasses ward, shifts disposition) |
| **Prefab plot hooks** | The Enchanter's Bargain with 3 phases, prerequisites, multiple outcomes |
| **Prefab events** | King's Guard Patrol with trigger conditions, cooldown, backstory-reactive behaviors |
| **Prefab selection** | Three-tier selection: active (full), nearby (summary), background (count) |
| **Usage policy / flexibility** | All prefabs at `flexibility: "low"` — core facts fixed, AI adds texture |
| **Natural-language triggers** | Behavior triggers evaluated by AI as part of prompt interpretation |
| **active_prefabs state** | Runtime tracking of which prefabs are in play |
| **Prefab state updates** | `prefab_activate`, `prefab_npc_update`, `plot_hook_advance`, `plot_hook_complete`, `event_occurred` |
| **NPC goals** | Valdris's goals drive NPC-initiated action across turns |
| **Content policy** | Mature rating enables the darker aspects of the moral choice |
| **Starting scenarios** | Can reference prefab starting_location for contextual opening |

### Design Gaps Revealed

#### 1. Prefab Discovery vs. Player Discovery

The prefab system defines what exists in the world. But how does the player *learn* about prefabs? The `discovery_hint` field on locations helps, but there's no formal mechanism for:

- NPCs revealing information about other prefabs ("Valdris tells you about the moonstone cave")
- Items carrying clues to other locations
- Events surfacing plot hook prerequisites

**Proposed approach**: Add an optional `reveals` field to behaviors, events, and item interactions:

```yaml
behaviors:
  - trigger: "player asks about moonstone sources"
    action: "Valdris reluctantly mentions a cave to the north"
    reveals: { type: "location", id: "moonstone_cave" }
```

When a reveal fires, the state engine adds a `world_fact` and the prompt assembler can include the revealed prefab's `discovery_hint` in the next turn.

#### 2. Prefab NPC Lifecycle

When a prefab NPC is encountered, they get copied into `active_npcs`. But what happens when:

- The player leaves the area — does the NPC deactivate? Stay active?
- The NPC dies — how is this tracked against the prefab?
- The NPC's runtime state diverges significantly from the prefab (they learn many new things, change goals)

**Proposed approach**: Prefab NPCs in `active_npcs` carry a `prefab_id` field linking back to their source. The prefab provides the *template*; `active_npcs` holds the *runtime state*. If the NPC deactivates (player leaves), their runtime state is preserved in a `dormant_npcs` list and restored when re-encountered. Death is tracked as a `world_fact` and prevents reactivation.

#### 3. Plot Hook Ordering and Conflicts

What happens when multiple plot hooks are active simultaneously? With 3-5 hooks defined, a player could trigger prerequisites for several at once.

**Proposed approach**: Add an optional `conflicts_with` field:

```yaml
plot_hooks:
  - id: "enchanter_bargain"
    conflicts_with: ["arcanist_recruitment"]
    # If this hook is active, the conflicting hook cannot activate
```

And a soft limit: max 2 active plot hooks at a time. The prompt assembler only injects the current phase of each, keeping token costs manageable.

#### 4. Event-to-Prefab Cascading

Events can change the state of prefab entities. The King's Guard patrol might capture the player (affects location/freedom), wound an NPC (affects NPC state), or confiscate an item. The current event schema has `behaviors` with `response` text, but no structured way to trigger prefab state changes.

**Proposed approach**: Add `state_effects` to event behaviors:

```yaml
behaviors:
  - trigger: "player is a deserter"
    response: "Guards become hostile, attempt arrest"
    state_effects:
      - conditions_add: ["captive", "disarmed"]
      - inventory_remove: ["weapons"]
      - prefab_npc_update: { id: "valdris", knows_add: "the King's Guard is hunting the player" }
```

This allows events to cascade into prefab state changes through the same state engine.
