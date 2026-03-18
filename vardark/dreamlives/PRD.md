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
[/STATE_UPDATE]
```

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
  [10. Active NPCs]
  [11. History Summary]
  [12. Response Format Instructions]

USER MESSAGE:
  [Recent narrative history — last 6-8 turns]
  [Player action]
```

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
