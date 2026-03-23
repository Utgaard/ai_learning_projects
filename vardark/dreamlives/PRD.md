# Infinite Adventure (DreamLives) — Product Requirements Document

## Overview

A character-driven AI adventure platform where players create a detailed persona — body, skills, personality, history — and inhabit them in a reactive world. The world responds realistically to who you are, not just what you do. Steal something and people get angry. Be physically imposing and some NPCs get nervous. Build a reputation and it precedes you.

The platform supports many worlds created by developers or the community, from high fantasy to modern-day teen drama. Each world is defined by a structured World Config. Players bring their own characters into any compatible world.

Target: SaaS product serving many concurrent players across many worlds.

---

## Core Experience

### The Loop

1. **Create a character** — hybrid structured + freeform. Pick stats, body, skills; then write backstory, appearance, mannerisms.
2. **Enter a world** — browse available worlds, each with its own genre, tone, rules, and cast.
3. **Play** — second-person narrative. Each turn: the AI describes what happens, the world reacts to your character, you choose from suggested actions or type your own.
4. **Persist** — your character accumulates history, relationships, scars, reputation. The world remembers.

### What Makes It Different

The character isn't a cursor. It's a body with a history. The world doesn't treat every player the same — it treats your character as a specific person with specific traits, and that specificity is what creates immersion.

---

## Character System

### Creation Flow

**Phase 1: Structured choices** — define mechanical capabilities

- **Name**
- **Body Build**: Small | Lean | Average | Sturdy | Large | Imposing
- **Age Range**: Child | Teen | Young Adult | Adult | Middle-aged | Elder
- **Appearance**: freeform short ("scarred face, red hair, missing two fingers")
- **Skills** (pick 2 primary, 1 secondary): Combat | Stealth | Social | Survival | Scholarly | Craft | Athletic | Medical
- **Traits** (pick 2-3): Brave | Cunning | Hot-tempered | Gentle | Paranoid | Charming | Stubborn | Quiet | Reckless | Methodical | Compassionate | Cold | Curious | Loyal

**Phase 2: Freeform backstory** — gives AI texture, shapes world response

Backstory: 1-3 paragraphs. Example:
> "Former soldier who deserted after refusing an order. Carries guilt. Speaks softly but watches everything. Has a habit of carving small wooden animals when nervous."

**Phase 3: World-specific additions** — each World Config can define extra character fields (e.g., "Race" and "Magic affinity" for fantasy, "Occupation" for modern).

### Character Sheet (Runtime)

Maintained by the app, injected into every prompt, updated after every turn:

```yaml
name: "Kael"
body_build: "lean"
age_range: "young_adult"
appearance: "scarred face, red hair, missing two fingers on left hand"
primary_skills: ["stealth", "survival"]
secondary_skill: "combat"
traits: ["cunning", "quiet", "paranoid"]
backstory: "Former soldier who deserted after refusing an order..."

# --- Mutable state (updated each turn) ---
conditions:
  simple: ["fatigued", "muddy and travel-worn"]
  timed:
    - id: "shoulder_wound"
      label: "minor wound on right shoulder"
      stage: "healing"
      started_turn: 10
      started_day: 4
      affects_skills: ["combat"]
      narrative_hint: "Shoulder stiffens during exertion. Healing but still tender."

time:
  current_day: 7
  time_of_day: "evening"
  turns_today: 3
  turn_count: 14

inventory: ["hunting knife", "waterskin", "stolen merchant's ring", "30 copper"]

skill_progress:
  stealth: "practiced"    # novice → practiced → skilled → expert
  survival: "practiced"
  combat: "novice"

reputation:
  - { faction: "town_guard", stance: "suspicious", reason: "seen near the theft" }
  - { faction: "merchant_guild", stance: "hostile", reason: "stole Aldric's ring" }
  - { npc: "Mira", stance: "friendly", reason: "helped her find her brother" }

world_facts:
  - "The bridge to Eastmoor was burned three days ago"
  - "You promised Mira you'd return by nightfall"
  - "The guard captain knows your face"

active_npcs:
  - { name: "Mira", role: "herbalist", disposition: "grateful and worried", knows: "your real name" }
  - { name: "Captain Aldric", role: "merchant", disposition: "furious", knows: "you stole his ring" }
```

---

## World Config System

Every world is defined by a structured config. World creators fill this out, and the platform does the rest.

### World Config Schema

```yaml
# === IDENTITY ===
id: "shattered-realms"
name: "The Shattered Realms"
tagline: "A crumbling empire where magic fades and ancient evils stir"
genre: "dark_fantasy"
tone: ["atmospheric", "dark", "gritty"]
rating: "mature"    # teen | mature | adult
cover_image: "url_or_asset_id"

# === CONTENT POLICY ===
content_flags:
  sexual_content: false
  graphic_violence: true
  drug_use: false
  dark_themes: true

# === WORLD DESCRIPTION ===
setting: |
  A medieval fantasy world where magic is fading. The Arcanist Order
  once held the realm together, but their power wanes. Kingdoms fracture.
  Ancient ruins hold forgotten power. Eldritch horrors stir in deep
  places. Common people are superstitious and desperate. Technology is
  pre-industrial. Travel is dangerous.

# === WORLD RULES ===
rules:
  - "Magic exists but is rare and costly. Only trained arcanists can use it."
  - "Death is permanent. If the player dies, the adventure ends."
  - "No modern technology or anachronisms."
  - "NPCs act in self-interest. They don't help strangers without reason."
  - "Combat is dangerous and realistic. Even skilled fighters can be killed by a lucky blow."

# === WORLD-SPECIFIC CHARACTER FIELDS ===
character_extensions:
  - field: "race"
    type: "select"
    options: ["Human", "Elf", "Dwarf", "Halfling", "Orc-blood"]
    prompt_hint: "Affects how NPCs perceive you."
  - field: "magic_affinity"
    type: "select"
    options: ["None", "Latent (untrained)", "Apprentice"]
    prompt_hint: "Determines if you can attempt magical actions."

# === STARTING SCENARIOS ===
starting_scenarios:
  - id: "deserter"
    name: "The Deserter"
    description: "You fled the king's army three days ago..."
    opening_context: "Player begins alone in a dense forest, 3 days after deserting."
  - id: "merchant_road"
    name: "The Road to Ashwick"
    description: "A routine merchant escort goes wrong when the caravan is ambushed."
    opening_context: "Player is guarding a merchant caravan. Bandits attack at a river crossing."

# === FACTIONS & KEY NPCS ===
factions:
  - name: "The Arcanist Order"
    description: "Dying magical institution. Desperate to preserve knowledge."
    default_stance_to_player: "indifferent"
  - name: "The King's Guard"
    description: "Loyal but overstretched. Enforce law through intimidation."
    default_stance_to_player: "neutral"

key_npcs:
  - name: "Sera Voss"
    role: "Arcanist recruiter"
    personality: "Measured, manipulative, genuinely believes magic must be saved"
    knows_initially: "nothing about the player"

# === NARRATOR INSTRUCTIONS ===
narrator_instructions: |
  Write in a terse, grounded style. Short sentences during action.
  Longer, atmospheric passages during exploration. Avoid flowery language.
  This world is hard and unforgiving — reflect that.

# === TONE MODIFIERS ===
tone_modifiers:
  violence_detail: "high"
  romance: "possible"
  humor: "rare"
  horror_elements: "moderate"
  political_intrigue: "moderate"
```

### Genre Examples

The PRD includes full World Config examples for:
- **Teen Drama** ("Westlake High") — modern high school, social stakes, no supernatural
- **Cyberpunk** ("Neon Frontier") — space station, megacorps, hackers, cybernetics
- **Historical Romance** ("The Gilded Court") — Renaissance court, intrigue, adult-rated

---

## Prefab Content System

World Configs can optionally include **prefabs** — pre-authored locations, NPCs, items, plot hooks, and events that give the AI structured content to work with. Prefabs solve the "empty world" problem: without them, every world is a blank canvas and the AI invents everything on the fly, leading to inconsistency and generic encounters. With prefabs, world creators ship curated bones that the AI builds on.

Prefabs serve a dual purpose:
1. **Curated experiences** — authored encounters, characters, and storylines that play out consistently
2. **Building blocks** — seed content the AI can remix, combine, and extend for emergent gameplay

A single data format handles both use cases. The `usage_policy` and per-entity `flexibility` fields control how much creative license the AI takes.

### Prefab Container

Added to the World Config alongside existing `factions`, `key_npcs`, `starting_scenarios`:

```yaml
prefabs:
  usage_policy: "adaptive"    # strict | adaptive | generative
  locations: [...]
  npcs: [...]
  items: [...]
  plot_hooks: [...]
  events: [...]
```

**Usage policies:**
- **strict** — Use prefabs exactly as described. Don't invent beyond them. For curated, authored worlds.
- **adaptive** — Core facts are fixed. AI may add minor details (clothing, current mood, specific dialogue) as needed. This is the default.
- **generative** — Prefabs are seeds. AI freely combines, adapts, and extends them. For sandbox worlds.

### Entity Type: Locations

Named places with atmosphere, contents, connections, and conditional descriptions.

```yaml
prefabs:
  locations:
    - id: "blackwood_tower"
      name: "Valdris's Tower"
      region: "The Blackwood"
      tags: ["dangerous", "magical", "isolated", "landmark"]
      flexibility: "low"           # none | low | high
      description: |
        A crooked stone tower rising above the Blackwood canopy.
        Strange lights flicker in the upper windows at night.
        The forest around it is unnaturally quiet.
      atmosphere: "foreboding, magical residue, silence"
      discovery_hint: |
        Locals in nearby settlements whisper about a tower in the
        deep Blackwood where a hermit wizard conducts dark experiments.
      contains_npcs: ["valdris"]
      contains_items: ["moonstone_ward"]
      connected_to: ["blackwood_trail", "hidden_cellar"]
      conditions:
        - when: "time_of_day == night"
          description_overlay: "The windows glow with sickly green light. You hear chanting."
        - when: "player.has_item('moonstone')"
          description_overlay: "The moonstone in your pack pulses faintly as you approach."
      rules:
        - "The tower is warded. Entering without moonstone triggers a magical trap."
        - "Valdris does not leave the tower voluntarily."
```

### Entity Type: NPCs

Characters with backstory, conditional behaviors, goals, and flexibility level. Extends the existing `key_npcs` pattern with richer structure.

```yaml
  npcs:
    - id: "valdris"
      name: "Valdris the Enchanter"
      role: "hermit wizard"
      flexibility: "low"
      personality: "paranoid, brilliant, obsessive about his research"
      appearance: "gaunt, tall, silver-streaked hair, eyes that glow faintly"
      backstory: |
        Former Arcanist Order member who was expelled for forbidden
        experiments. He fled to the Blackwood 20 years ago. He seeks
        to reverse the fading of magic through any means necessary.
      faction: "none"
      home_location: "blackwood_tower"
      tags: ["magic_user", "antagonist", "knowledgeable", "dangerous"]

      knows_initially:
        - "The Arcanist Order is dying"
        - "He was expelled for experimenting on living subjects"
        - "Moonstone can channel residual magical energy"

      behaviors:
        - trigger: "player approaches tower without moonstone"
          action: "Attacks with defensive ward magic"
          disposition_shift: "hostile"
          narrative_hint: |
            Valdris sees intruders as threats or test subjects.
            Without an offering, he assumes hostility.
        - trigger: "player offers moonstone"
          action: "Becomes cautiously interested, invites player inside"
          disposition_shift: "wary but curious"
          narrative_hint: |
            Moonstone is rare and valuable to his research. Anyone
            who brings it either knows his work or is very lucky.
            Either way, they're useful.
        - trigger: "player mentions the Arcanist Order"
          action: "Becomes agitated, demands to know what they want"
          disposition_shift: "paranoid"
          narrative_hint: |
            Old wounds. He expects the Order sent someone to finish him.
        - trigger: "player has magic_affinity != None"
          action: "Senses the affinity, becomes intensely interested"
          disposition_shift: "fascinated"
          narrative_hint: |
            A magically gifted visitor is both a potential ally and
            a potential test subject. Valdris is torn.

      goals:
        - goal: "Acquire moonstone for his experiments"
          urgency: "medium"
        - goal: "Find a magically gifted subject to test his theories"
          urgency: "high"

      offers:
        - "Magical knowledge (if trust is earned)"
        - "Shelter in the tower (with strings attached)"
        - "Information about the Arcanist Order's secrets"
```

### Entity Type: Items

Notable objects with properties, narrative significance, and cross-entity interactions.

```yaml
  items:
    - id: "moonstone"
      name: "Moonstone"
      tags: ["magical", "rare", "trade_goods", "quest_item"]
      flexibility: "low"
      description: "A smooth, luminous stone that pulses with faint inner light."
      properties:
        value: "high"              # low | medium | high | priceless
        weight: "light"            # light | medium | heavy
        magical: true
      narrative_hint: |
        Moonstone is rare in the Shattered Realms. Those who deal in
        magic covet it. Common folk consider it cursed.
      found_at: ["moonstone_cave", "merchant_rare_goods"]
      interactions:
        - with_npc: "valdris"
          effect: "Offering it shifts his disposition from hostile to curious"
        - with_location: "blackwood_tower"
          effect: "Bypasses the magical ward on the entrance"
```

### Entity Type: Plot Hooks

Multi-phase storylines connecting entities, with prerequisites and possible outcomes. Plot hooks cover the role of "quests" without implying a quest-log UI that would break immersion.

```yaml
  plot_hooks:
    - id: "enchanter_bargain"
      name: "The Enchanter's Bargain"
      tags: ["magic", "moral_choice", "dangerous", "multi_session"]
      flexibility: "low"
      synopsis: |
        Valdris offers to teach the player magic in exchange for
        helping him capture a magical creature in the deep Blackwood.
        The creature is sentient. The player must choose between
        power and conscience.
      prerequisites:
        - "player has met valdris"
        - "valdris.disposition != hostile"
      involves_npcs: ["valdris"]
      involves_locations: ["blackwood_tower", "deep_blackwood"]
      involves_items: ["binding_crystal"]
      phases:
        - id: "offer"
          summary: "Valdris proposes the deal after testing the player"
          hints_to_ai: |
            Valdris should frame it as mutual benefit. He downplays
            the creature's sentience. The offer should feel tempting.
        - id: "hunt"
          summary: "Player ventures into the deep Blackwood to find the creature"
          hints_to_ai: |
            The forest gets stranger the deeper you go. The creature
            is not what Valdris described — it's intelligent and afraid.
        - id: "choice"
          summary: "Player decides: capture the creature or defy Valdris"
          hints_to_ai: |
            No option is clean. Capturing it gives real power but at
            moral cost. Defying Valdris makes a dangerous enemy. The
            creature may offer a third path.
      possible_outcomes:
        - "Player captures the creature — gains magic, loses something human"
        - "Player frees the creature — earns its gratitude, Valdris becomes hostile"
        - "Player negotiates — risky, could go either way"
```

### Entity Type: Events

Triggered situational encounters with cooldowns and max occurrences.

```yaml
  events:
    - id: "blackwood_patrol"
      name: "King's Guard Patrol in the Blackwood"
      tags: ["encounter", "social", "potential_combat"]
      flexibility: "low"
      description: |
        A squad of King's Guard soldiers searching the Blackwood
        for deserters and outlaws.
      trigger_conditions:
        - "player.location.region == 'The Blackwood'"
        - "time_of_day in ['morning', 'midday', 'afternoon']"
      cooldown: "3_days"
      max_occurrences: 2
      setup: |
        The player hears boots and clinking armor on the trail ahead.
        A squad of four King's Guard soldiers rounds the bend.
      npc_participants:
        - name: "Sergeant Harlan"
          role: "patrol leader"
          personality: "dutiful, suspicious, tired"
          disposition: "neutral"
      behaviors:
        - trigger: "player is a deserter (backstory/world_facts)"
          response: "Guards become hostile, attempt arrest"
        - trigger: "player cooperates and has no warrants"
          response: "Guards warn about dangers in the Blackwood and move on"
        - trigger: "player has faction reputation with King's Guard >= friendly"
          response: "Sergeant recognizes player, offers information or supplies"
```

### Conditional Behaviors: Natural Language Triggers

Prefab behaviors use **natural-language triggers** evaluated by the AI, not a programming DSL. This is deliberate:

1. World creators are writers, not programmers — the design principle is "data, not code"
2. The AI already evaluates natural language every turn — trigger conditions are just another input
3. A formal DSL would require a parser, validator, and runtime engine that adds complexity without proportional benefit at this stage

The prompt assembler formats triggers as clear conditional instructions:

```
[ACTIVE PREFAB: NPC — Valdris the Enchanter]
Location: Blackwood Tower
Disposition: unknown (first encounter)
CONDITIONAL BEHAVIORS:
- IF the player approaches without moonstone → Attacks with ward magic (becomes hostile)
- IF the player offers moonstone → Invites player inside (becomes wary but curious)
- IF player mentions the Arcanist Order → Becomes agitated and paranoid
- IF player has magical affinity → Becomes fascinated, sees player as potential subject
Apply the first matching behavior. After the initial reaction, Valdris acts according to his personality and goals.
```

**Upgrade path**: If natural-language evaluation proves unreliable for specific triggers, a `trigger_code` field can be added for code-side evaluation before prompt assembly. Start simple, add complexity only when needed.

### Per-Entity Flexibility

Each prefab carries a `flexibility` field that controls how the prompt assembler frames it to the AI:

| Flexibility | Prompt Framing | Use Case |
|-------------|---------------|----------|
| `none` | "The following exists exactly as described. Do not alter name, backstory, or core behaviors." | Authored, canonical content |
| `low` | "Core identity is fixed. You may add minor details (clothing, mood, dialogue) as needed." | Default. Consistent but living. |
| `high` | "This is a template. You may adapt name, details, and circumstances. Preserve the essential role and personality archetype." | Seed content for emergent worlds |

For `usage_policy: "generative"` worlds, the prompt assembler also generates a "world ingredients" summary from prefab tags:

```
[WORLD INGREDIENTS]
This world contains the following building blocks you may draw from:
- Locations: forest towers, abandoned mines, riverside settlements, ancient ruins
- Character archetypes: paranoid wizards, corrupt guards, desperate merchants, feral druids
- Plot patterns: dangerous bargains, faction conflicts, ancient secrets, moral dilemmas
- Notable items: moonstone, binding crystals, Arcanist relics, cursed weapons
You may combine, adapt, and remix these freely to create emergent encounters.
```

This auto-generated summary costs only ~100-150 tokens.

### Prefab Selection for Prompt Assembly

A rich world might have 50 locations, 30 NPCs, 20 items, 10 plot hooks, and 15 events. Far too much to inject every turn. The prompt assembler uses **three-tier selection**:

**Tier 1: Active (full detail, every turn)**
Prefabs currently "in play" — the player is at the location, the NPC is in the scene, the item is in inventory, the plot hook is in progress. Tracked via `active_prefabs` in GameState.

**Tier 2: Nearby/Relevant (one-line summaries)**
- Locations connected to current location (one hop)
- NPCs whose `home_location` matches current region
- Items whose `found_at` includes current location
- Events whose `trigger_conditions` match current state
- Plot hooks whose `prerequisites` are met but not yet triggered

The prompt assembler evaluates these by scanning prefab metadata against current game state. This is a code-side filter, not an AI call.

**Tier 3: Background (aggregate count only)**
Everything else. One line: "This world contains 12 other notable locations, 8 other named NPCs, and 5 discoverable plot hooks."

**Token budget for prefabs:**

| Section | Tokens |
|---------|--------|
| Current location prefab (full) | ~200-300 |
| Active NPC prefabs (full, max 2) | ~300-500 |
| Active plot hook (current phase only) | ~150-200 |
| Nearby/relevant prefabs (summaries) | ~200-300 |
| Active event (if triggered) | ~150-200 |
| **Total prefab budget** | **~1,000-1,500** |

Static world config (setting, rules, narrator instructions) compresses to ~800 tokens. Total world section: ~2,000-2,300 tokens, a modest increase from the original ~1,800.

**Hard cap**: Max 1 full location + 2 full NPC prefabs + 1 plot hook phase per turn. Everything else is summarized.

### Runtime State: active_prefabs

New field on GameState tracking which prefabs are currently in play:

```yaml
active_prefabs:
  location: "blackwood_tower"
  npcs: ["valdris"]
  items: ["moonstone"]
  plot_hooks: ["enchanter_bargain"]
  active_phase: { "enchanter_bargain": "hunt" }
```

### State Updates for Prefabs

New fields in the `[STATE_UPDATE]` block:

```
[STATE_UPDATE]
...existing fields...
prefab_activate: { type: "location", id: "blackwood_tower" }
prefab_deactivate: { type: "npc", id: "valdris" }
prefab_npc_update: { id: "valdris", disposition: "hostile", knows_add: "player is a deserter" }
plot_hook_advance: { id: "enchanter_bargain", phase: "hunt" }
plot_hook_complete: { id: "enchanter_bargain", outcome: "player freed the creature" }
event_occurred: { id: "blackwood_patrol", turn: 14 }
[/STATE_UPDATE]
```

The state engine validates these against the world config: you cannot activate a prefab that doesn't exist, advance a plot hook to an undefined phase, or fire an event that has exceeded `max_occurrences`.

### Integration with Existing Structures

| Existing Field | Relationship to Prefabs |
|---|---|
| `key_npcs` | Kept for simple NPCs. Prefab NPCs are the richer version. World creators can use either. |
| `factions` | Prefab NPCs reference factions by name. Factions stay top-level. |
| `starting_scenarios` | Can reference prefab locations and NPCs: `starting_location: "blackwood_trail"` |
| `world_facts` (runtime) | Prefab discoveries become world facts: "You know Valdris lives in the Blackwood tower" |
| `active_npcs` (runtime) | Prefab NPCs, when encountered, get copied into `active_npcs` with their full state |

Updated starting scenario example:

```yaml
starting_scenarios:
  - id: "blackwood_wanderer"
    name: "The Blackwood Wanderer"
    description: "You enter the Blackwood seeking the rumored hermit wizard..."
    opening_context: "Player begins on the trail leading into the Blackwood."
    starting_location: "blackwood_trail"
    starting_npcs_nearby: []
    starting_world_facts:
      - "Rumors speak of a wizard in the deep Blackwood"
      - "Moonstone is said to be valued by magic users"
```

### Implementation Sequencing

The prefab system fits into **Phase 2** (World Platform). Implementation order:

1. Add prefab type interfaces to `shared/src/types/world.ts`
2. Add `active_prefabs` and new StateUpdateBlock fields to `shared/src/types/state.ts`
3. Build `selectPrefabs()` in `server/src/services/prompt-assembler.ts` — the selection/filtering logic
4. Add prefab serialization to prompt assembly — new sections for location, NPCs, plot hooks, events
5. Extend `state-parser.ts` and `state-engine.ts` for prefab state update fields
6. Author prefab content for the fantasy world in `server/src/worlds/fantasy.ts`
7. Playtest and tune selection algorithm and token budgets

---

## Content Rating & Policy

### Rating Tiers

| Rating | Violence | Romance | Sexual Content | Access |
|--------|----------|---------|----------------|--------|
| Teen | Mild | Fade to black | None | All players |
| Mature | Graphic combat | Suggestive | None | 17+ |
| Adult | Extreme if flagged | Explicit if flagged | Explicit if flagged | 18+ verified |

### Content Flags (Granular)

Within each rating tier, world creators set: `sexual_content`, `graphic_violence`, `drug_use`, `dark_themes`.

Effective content policy per turn = intersection of world config flags AND player account preferences.

---

## Model Routing Architecture

### Provider-Agnostic Routing

```
Player Action → Prompt Assembly → Model Router → API Gateway
                                     ↓
                              Routes based on:
                              - Content tier
                              - Subscription tier
                              - Cost budget

                              Standard: Claude / GPT
                              Unrestricted: Nous Hermes / Dolphin
```

### Model Configuration (data, not code)

```yaml
model_config:
  standard:
    primary: "claude-sonnet-4-20250514"
    fallback: "gpt-4o"
  unrestricted:
    primary: "nous-hermes-3-llama-3.2-8b"
    fallback: "dolphin-3.0-llama-3.1-8b"
  budget:
    primary: "dolphin-mistral-24b-venice"
    fallback: "llama-3.2-8b-abliterated"
```

---

## State Document Architecture

### Turn Flow

1. App assembles prompt: World Config + Character Sheet + State + Content Policy + Recent History + Player Action
2. Model Router selects model
3. AI generates: narrative prose + suggested choices + `[STATE_UPDATE]` block
4. App parses response, displays narrative, patches state doc, validates updates, persists

### State Update Format

```
[STATE_UPDATE]
conditions_add: ["exhausted"]
conditions_remove: ["well-rested"]
inventory_add: ["rusty iron key"]
inventory_remove: ["lockpick (broken)"]
reputation_update: { "town_guard": "hostile", "reason": "caught stealing" }
npc_update: { "Mira": { "disposition": "worried", "knows_add": "you were arrested" } }
world_fact_add: "The jail cell has a loose stone in the north wall"
skill_used: "stealth"
scene_context: "exploration"
time_advance: { "days": 0, "time_of_day": "night" }
prefab_activate: { "type": "location", "id": "blackwood_tower" }
prefab_deactivate: { "type": "npc", "id": "valdris" }
prefab_npc_update: { "id": "valdris", "disposition": "hostile", "knows_add": "player is a deserter" }
plot_hook_advance: { "id": "enchanter_bargain", "phase": "hunt" }
plot_hook_complete: { "id": "enchanter_bargain", "outcome": "player freed the creature" }
event_occurred: { "id": "blackwood_patrol" }
[/STATE_UPDATE]
```

The prefab-related fields (`prefab_activate`, `prefab_deactivate`, `prefab_npc_update`, `plot_hook_advance`, `plot_hook_complete`, `event_occurred`) are validated by the state engine against the world config. Invalid references (nonexistent prefab IDs, undefined phases, exceeded event limits) are rejected and logged.

App validates: reject rule-violating updates, clamp values, log rejections.

---

## Timed Conditions

Timed conditions model long-arc transformations: pregnancy, disease, slow-healing injuries, curses, aging effects, withdrawal, etc. They progress through stages as in-world time passes.

The PRD includes detailed condition template examples for pregnancy (6 stages with symptoms, physical changes, skill impacts, NPC reactions, narrative hints) and slow-healing wounds.

Timed conditions are a **Phase 2** feature.

---

## Context Window Management

1. **Always include**: World Config, Character Sheet, full State Document, Content Policy
2. **Recent history**: Last 6-8 turns raw
3. **Summarized history**: Every 10 turns, condense older turns to a paragraph
4. **Priority injection**: Pull relevant summaries when near key NPCs/locations

Budget per turn (~8,000 tokens input): World Config ~1,800 + Character ~800 + Conditions ~300-600 + Summary ~500 + Recent turns ~4,000 + Action ~500

---

## Progression System

Lightweight: **Novice → Practiced → Skilled → Expert**

- State doc tracks `skill_used` each turn
- ~5 uses → Practiced, ~15 → Skilled, ~30 → Expert
- AI adjusts descriptions and success likelihood per tier

---

## Prompt Assembly (Full Structure)

```
SYSTEM PROMPT:
  [1. Narrator Role]
  [2. World Setting — from World Config]
  [3. World Rules]
  [4. Content Policy — computed per-turn]
  [5. Narrator Style]
  [6. Tone Modifiers]
  [7. Character Sheet]
  [8. Current State — conditions, inventory, reputation, time, NPCs, world facts]
  [9. Timed Conditions — active stages with narrative hints]
  [10. Active Prefabs — current location, scene NPCs, active plot hook phase, triggered events]
  [11. Nearby Prefabs — one-line summaries of connected locations, regional NPCs, available hooks]
  [12. Active NPCs (non-prefab)]
  [13. History Summary]
  [14. Response Format Instructions]

USER MESSAGE:
  [Recent narrative history — last 6-8 turns]
  [Player action]
```

Sections 10-11 are produced by the `selectPrefabs()` function (see Prefab Content System). They draw from the `active_prefabs` field in GameState and the world config's `prefabs` container. The three-tier selection system ensures only relevant prefabs consume context window budget.

---

## Pricing Tiers

| Tier | Price | Models | Content | Turns |
|------|-------|--------|---------|-------|
| Free | $0 | Budget | Teen only | ~10-20/day |
| Standard | ~$10/mo | Claude/GPT | Teen + Mature | Unlimited |
| Premium | ~$20/mo | Best per category | All incl. Adult | Unlimited |

---

## Architecture

```
Client (Web UI) → API Layer (Backend) → Model Router → OpenRouter/Anthropic/Venice
                        ↓
                   Database
                   - Players
                   - Characters
                   - Worlds
                   - Game State
                   - Turn Log
                   - Model Config
```

Key decisions:
- State lives server-side (prevents cheating, enables multiplayer later)
- World Configs are data, not code
- Turn log is append-only
- Model config is data, not code
- Content policy computed per-turn

---

## Implementation Phases

### Phase 1: Core Loop (MVP) ← CURRENT
Character creation, single fantasy world, game screen, state document, time tracking, state parsing, context management, persistence, single model provider.

### Phase 2: World Platform + Content Tiers
World Config schema + storage, browse UI, content ratings, model routing, timed conditions engine, multiple characters/saves, 3-5 launch worlds.

### Phase 3: Creators & Community
World creation editor, custom conditions, publishing, analytics, creator revenue share.

### Phase 4: Polish & Scale
Skill progression, NPC depth, history summarization, hybrid routing, A/B testing, performance optimization, billing.

---

## Open Questions

1. Validation strictness — how aggressively reject AI state updates?
2. Character death — permanent? Per-world? Fate points?
3. World creator tools — minimum viable creation experience?
4. Age verification depth
5. Model quality parity between premium and unrestricted
6. Hybrid routing accuracy
7. Legal structure for adult content
