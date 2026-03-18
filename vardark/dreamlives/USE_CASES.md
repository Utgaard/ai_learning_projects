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

## UC-02: (Template for future scenarios)

**World**:
**Content flags**:
**Character**:

### Scenario
_To be filled in._

### Systems Involved
_To be filled in._

### Design Gaps Revealed
_To be filled in._
