# DreamLives — Phase 1 (MVP) Implementation Plan

## Product Summary

DreamLives is an AI-powered interactive fiction platform. Players create detailed characters (stats + freeform backstory) and play through AI-generated narratives in reactive worlds. Each turn: the AI narrates in 2nd person, the world reacts to your character's traits/history/reputation, you choose from suggested actions or type your own, and the state document updates.

Phase 1 is the **core loop**: one player, one hardcoded fantasy world, character creation, turn-based play, state persistence.

---

## Tech Stack

| Layer | Choice | Why |
|-------|--------|-----|
| Frontend | **React + Vite + TypeScript** | Fast to scaffold, Vite already used in repo, TypeScript catches state bugs |
| Styling | **Tailwind CSS** | Fast MVP prototyping, no design system needed |
| Backend | **Node.js + Express + TypeScript** | Same language as frontend, simple HTTP server, shared types |
| Database | **SQLite via better-sqlite3** | Zero infrastructure, single file, JSON columns for flexible state |
| AI Provider | **OpenRouter → Claude Sonnet** | Single API key, easy model swapping later |
| Monorepo | **npm workspaces** | No Turborepo/Nx overhead. 3 packages: `shared/`, `server/`, `client/` |

**Why not Python?** The backend is mostly string assembly + HTTP calls + JSON parsing. One language across the stack is the biggest velocity multiplier for a solo/small team.

**Why not Next.js?** No SEO need for a game UI. A plain Vite SPA + Express backend is simpler to deploy and reason about.

---

## Directory Structure

```
vardark/dreamlives/
  package.json              # npm workspaces root
  tsconfig.base.json        # shared TS config
  .env.example              # OPENROUTER_API_KEY, PORT, DATABASE_PATH
  README.md

  shared/                   # shared types package
    package.json
    tsconfig.json
    src/
      types/
        character.ts        # CharacterSheet, Stats
        world.ts            # WorldConfig
        state.ts            # GameState, Condition, InventoryItem, NpcState
        turn.ts             # Turn, TurnChoice, StateUpdateBlock
        api.ts              # Request/Response DTOs
      constants/
        defaults.ts         # stat names, time-of-day values, starting conditions

  server/                   # Express backend
    package.json
    tsconfig.json
    src/
      index.ts              # Express app entry
      routes/
        character.ts        # POST/GET characters
        game.ts             # start game, submit turn, load game
        save.ts             # save/load
      services/
        prompt-assembler.ts # builds full system prompt (THE critical service)
        state-parser.ts     # parses [STATE_UPDATE] blocks from AI response
        state-engine.ts     # validates + applies state patches
        model-router.ts     # calls OpenRouter API
        game-engine.ts      # orchestrates a turn: assemble → call → parse → patch → persist
        time-tracker.ts     # in-world time advancement
      data/
        db.ts               # SQLite setup
        character-repo.ts
        game-repo.ts
        save-repo.ts
      worlds/
        fantasy.ts          # hardcoded fantasy world config
      prompts/
        system-template.ts  # narrator role, format instructions
        format-instructions.ts

  client/                   # React SPA
    package.json
    tsconfig.json
    vite.config.ts
    index.html
    src/
      main.tsx
      App.tsx
      api/
        client.ts           # fetch wrapper for backend API
      screens/
        MainMenu.tsx
        CharacterCreate.tsx
        GameScreen.tsx
        SaveLoad.tsx
      components/
        NarrativeDisplay.tsx
        ChoicePanel.tsx
        CharacterSheet.tsx
        TimeDisplay.tsx
      hooks/
        useGame.ts
      styles/
        global.css
```

---

## Data Models

### Character Sheet
```typescript
interface CharacterSheet {
  id: string;
  name: string;
  bodyBuild: "small" | "lean" | "average" | "sturdy" | "large" | "imposing";
  ageRange: "teen" | "young_adult" | "adult" | "middle_aged" | "elder";
  appearance: string;            // freeform short description
  primarySkills: string[];       // pick 2 from: combat, stealth, social, survival, scholarly, craft, athletic, medical
  secondarySkill: string;        // pick 1
  traits: string[];              // pick 2-3 from predefined list
  backstory: string;             // 1-3 paragraphs freeform
  createdAt: string;
}
```

### Game State (the "State Document")
```typescript
interface GameState {
  gameId: string;
  characterId: string;
  worldId: string;
  turnCount: number;

  // In-world time
  time: {
    currentDay: number;
    timeOfDay: "dawn" | "morning" | "midday" | "afternoon" | "evening" | "night";
    turnsToday: number;
  };

  // Mutable character state
  conditions: string[];                    // simple conditions: "fatigued", "muddy"
  inventory: string[];                     // "hunting knife", "30 copper"
  skillProgress: Record<string, string>;   // skill -> "novice" | "practiced" | "skilled" | "expert"

  // World state
  reputation: ReputationEntry[];           // { faction/npc, stance, reason }
  activeNpcs: NpcState[];                  // { name, role, disposition, knows[] }
  worldFacts: string[];                    // accumulated facts

  // Context management
  historySummary: string;                  // rolling summary of older turns
}

interface StateUpdateBlock {
  conditions_add?: string[];
  conditions_remove?: string[];
  inventory_add?: string[];
  inventory_remove?: string[];
  reputation_update?: { target: string; stance: string; reason: string };
  npc_update?: { name: string; disposition?: string; knows_add?: string };
  world_fact_add?: string;
  skill_used?: string;
  scene_context?: "exploration" | "combat" | "social" | "intimate" | "tension";
  time_advance?: { days: number; time_of_day: string };
}
```

### Turn Log
```typescript
interface Turn {
  turnNumber: number;
  playerAction: string;
  narrative: string;              // AI's 2nd-person narrative
  choices: string[];              // 3 suggested next actions
  rawStateUpdate: string;         // raw [STATE_UPDATE] block
  parsedStateUpdate: StateUpdateBlock;
  modelUsed: string;
  timestamp: string;
}
```

### Database (SQLite, JSON columns)
```sql
CREATE TABLE characters (id TEXT PK, data JSON, created_at TEXT);
CREATE TABLE games (id TEXT PK, character_id TEXT, state JSON, created_at TEXT, updated_at TEXT);
CREATE TABLE turns (id TEXT PK, game_id TEXT, turn_number INT, data JSON, created_at TEXT);
CREATE TABLE saves (id TEXT PK, game_id TEXT, name TEXT, state_snapshot JSON, created_at TEXT);
```

---

## Core Services

### prompt-assembler.ts (most important file)
Builds the full system prompt per-turn from modular sections:
1. Narrator role
2. World setting + rules
3. Content policy (mature-rated)
4. Narrator style + tone modifiers
5. Character sheet (serialized)
6. Current state (conditions, inventory, reputation, time, NPCs)
7. Active NPCs in scene
8. History summary
9. Response format instructions (narrative + 3 choices + `[STATE_UPDATE]` JSON)

### state-parser.ts
Splits AI response on `[STATE_UPDATE]...[/STATE_UPDATE]` markers. Extracts narrative, choices, and state update JSON. Validates JSON structure. Graceful fallback: if parsing fails, return narrative only (no state change).

### state-engine.ts
Pure function: `apply(currentState, stateUpdate) → newState`. Applies deltas, clamps values, removes expired conditions, validates constraints. Immutable — returns new object.

### model-router.ts
Calls OpenRouter `POST /api/v1/chat/completions` with `anthropic/claude-sonnet-4-20250514`. Handles retries (1 on 5xx), timeout (60s), rate limit backoff.

### game-engine.ts (the orchestrator)
`processTurn(gameId, playerAction)`:
1. Load game state + character + world config
2. Build context (recent turns + summary)
3. Assemble prompt
4. Call model
5. Parse response
6. Apply state update
7. Persist new state + turn log
8. Return `{ narrative, choices, updatedState }`

---

## API Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| `POST` | `/api/characters` | Create character |
| `GET` | `/api/characters/:id` | Get character |
| `POST` | `/api/games` | Start new game `{ characterId, worldId }` |
| `GET` | `/api/games/:id` | Get current game state |
| `POST` | `/api/games/:id/turn` | Submit action, get narrative + new state |
| `POST` | `/api/games/:id/save` | Save game |
| `GET` | `/api/saves` | List saves |
| `POST` | `/api/saves/:id/load` | Load save |

Key endpoint `POST /api/games/:id/turn` takes 5-15s (AI generation). Simple long-polling for MVP; streaming is Phase 2.

---

## Frontend Screens

**MainMenu** — "New Game" + "Continue" (if saves exist). Minimal.

**CharacterCreate** — Follows PRD's 3-phase flow:
1. Structured: name, body build, age range, appearance, skill selection, trait selection
2. Freeform: backstory textarea (1-3 paragraphs)
3. Review + confirm

**GameScreen** — The core experience:
- Main area: scrollable narrative, 3 choice buttons + freeform input + submit
- Sidebar: character stats, conditions, inventory, reputation, time display
- Header: character name, world name, save button
- Loading state during AI generation

**SaveLoad** — List of saves with timestamps. Click to load.

---

## Implementation Order (14 Tasks)

Each task produces something testable. Sequence is dependency-ordered.

### Task 1: Project Scaffolding
- Init npm workspaces root `package.json`
- Create `shared/`, `server/`, `client/` with configs
- Vite + React for client, tsx/ts-node for server
- `.env.example`, `.gitignore`
- Verify `npm run dev` starts both

### Task 2: Shared Types
- All TypeScript interfaces in `shared/src/types/`
- Constants (stat names, time-of-day values, skill tiers, trait lists)

### Task 3: Database Layer
- SQLite setup in `server/src/data/db.ts`
- CREATE TABLE statements
- Repository functions: CRUD for characters, games, turns, saves

### Task 4: Hardcoded Fantasy World
- Write full `WorldConfig` in `server/src/worlds/fantasy.ts`
- Narrator role, rules, content policy, style, tone, starting scenario
- This is creative writing work — quality here = quality of the game

### Task 5: Model Router
- OpenRouter API integration
- Retries, timeout, rate limit handling
- Test with a simple prompt

### Task 6: Prompt Assembler
- Section builders as pure functions
- Main `assemble()` concatenating all sections
- Response format instructions with extreme precision
- Test by logging assembled prompt for sample state

### Task 7: State Parser
- Regex extraction of `[STATE_UPDATE]...[/STATE_UPDATE]`
- JSON parsing with validation
- Graceful fallback on malformed responses
- Test with sample AI responses

### Task 8: State Engine + Time Tracker
- `apply(state, update) → newState` pure function
- Time-of-day advancement logic
- Validation: clamp values, remove expired conditions

### Task 9: Game Engine
- Full turn orchestration
- `startGame()`: creates initial state, runs opening turn
- `processTurn()`: the full cycle
- End-to-end test: create character → start game → play 3 turns

### Task 10: API Routes
- Express routes wired to game engine + repositories
- Input validation
- Test with curl/Postman

### Task 11: Frontend — Main Menu + Character Creation
- `MainMenu.tsx`, `CharacterCreate.tsx`
- Stat/skill/trait selection UI
- Backstory editor
- API integration

### Task 12: Frontend — Game Screen
- `GameScreen.tsx` with narrative display + choice panel + sidebar
- Zustand store for game state
- Loading state during AI calls
- Turn history scroll

### Task 13: Frontend — Save/Load
- Save button, `SaveLoad.tsx` screen
- Wire to save/load endpoints

### Task 14: Polish + Playtest
- Play 10+ sessions
- Tune world config and prompt templates
- Fix state parsing edge cases
- Error handling UI

---

## What Phase 1 Does NOT Include

- User authentication / accounts (single local user)
- Multiple worlds (just the one fantasy world)
- Timed conditions engine (Phase 2 — only simple conditions for now)
- Content rating system / model routing by content tier
- Streaming responses (SSE)
- Multiplayer
- Payment / subscription
- AI-generated images
- Mobile-specific UI
- Automated tests (manual playtesting, add tests Phase 2)

---

## Key Risks

| Risk | Mitigation |
|------|------------|
| AI doesn't follow response format | Exhaustively specific format instructions + resilient parser with retry |
| State document grows unbounded | Cap world facts at 50, NPCs at 20, inventory at 30 |
| Turn latency >15s | Loading state with flavor text; streaming in Phase 2 |
| Context window fills up | Rolling summary + last 10 raw turns; Sonnet's 200k context is generous |
