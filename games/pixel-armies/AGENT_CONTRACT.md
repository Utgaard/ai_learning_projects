# Pixel Armies – Agent Contract (for Codex)

## Design authority
- `pixel-armies/design/GAME_DESIGN.md` is the single source of truth for game behavior and constraints.
- If a requested change conflicts with the design doc, the agent must:
  1) Point out the conflict
  2) Ask for clarification before proceeding
- If implementation reveals a design gap, propose a minimal doc update.


## Goals
- Build Pixel Armies in Godot 4 + C#
- SimCore must be deterministic and runnable headless for analyzer runs
- Presentation layer must not contain game logic

## Non-goals
- No player input during battles
- No overengineering (ECS, DOTS-like patterns) unless profiling proves needed
- No physics engine reliance for core combat

## Repo structure
- pixel-armies/godot/PixelArmies/ is the Godot project root
- Scripts/SimCore contains pure logic (no Godot node dependencies)
- Scripts/ contains Godot-facing runtime/rendering code

## Coding rules
- Prefer clarity over cleverness
- Enable nullable: `#nullable enable` in new C# files
- Avoid naming collisions with Godot types (use aliases or rename enums)
- Keep SimCore free of Godot types (no Vector2, Node, Color, etc.)

## Workflow
- Make small commits
- After changes: ensure project builds in Godot
- If tests/tools exist: run them

## Output expectations
- When asked to implement a feature:
  - Edit necessary files
  - Add new files if needed
  - Keep code compiling
  - Summarize what changed and where
