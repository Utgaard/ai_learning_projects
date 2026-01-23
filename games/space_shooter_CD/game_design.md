# Retro 2D Space Shooter (Phaser.js) - Game Design

## Summary
A classic top-down space shooter with pixel art, simple controls, and escalating enemy waves. Built in Phaser.js with a low "legacy" resolution and crisp pixel scaling.

## Requirements
- Tech: Phaser.js (HTML5 Canvas/WebGL)
- Resolution: low legacy base resolution (default 320x180) with integer scaling
- Controls: Arrow keys or WASD to move, Space to shoot
- Visuals: pixel art, retro palette, simple UI
- Audio: use provided asset sound effects
- Playable at each milestone
- Code stays simple, readable, and beginner-friendly

## Core Gameplay Loop
1) Player moves and shoots.
2) Enemies spawn in waves and move downward.
3) Destroy enemies to gain score.
4) Player loses lives on collision or enemy projectiles.
5) Game over after lives run out; show score and restart.

## Milestones (Each Playable)

### Milestone 1: Core Movement + Shooting (Playable)
**Goal:** Basic ship control and shooting, with a simple background.
- Player ship moves within screen bounds
- Shoot basic laser bolts
- Minimal HUD: score placeholder
- Background image, no enemies yet
- Audio: laser sound

**Playable Test:** Move ship and fire continuously on a static background.

### Milestone 2: Enemies + Scoring (Playable)
**Goal:** Add enemies, collisions, and scoring.
- Spawn small enemies in waves
- Enemy movement (simple down drift or zig-zag)
- Player shots destroy enemies
- Score increments on destroy
- Explosion animation on enemy death
- Audio: explosion sound

**Playable Test:** Shoot incoming enemies and rack up score.

### Milestone 3: Lives + Difficulty (Playable)
**Goal:** Add survivability, difficulty scaling, and game over.
- Player lives and hit feedback
- Enemy bullets (basic) or collision damage
- Wave speed/size increases over time
- Game over screen and restart
- Optional: power-up drops (if time permits)

**Playable Test:** Survive escalating waves; game ends when lives reach zero.

## Asset Links (Planned Usage)

### Player Ship
- `assets/player/space_shooter/sprites/`
- Fallback:
  - `assets/player/space_ship_shooter/ship.png`
  - `assets/player/top_down_shooter_ship/ship-01.png`

### Enemies
- `assets/enemies/space_shooter/`
- Fallback:
  - `assets/enemies/space_ship_shooter/enemy-small.png`
  - `assets/enemies/space_ship_shooter/enemy-medium.png`
  - `assets/enemies/space_ship_shooter/enemy-big.png`

### Projectiles
- `assets/projectiles/space_ship_shooter/laser-bolts.png`
- Fallback:
  - `assets/projectiles/sewers/bullet.png`

### Explosions
- `assets/explosions/space_shooter/`
- Fallback:
  - `assets/explosions/space_ship_shooter/explosion.png`
  - `assets/explosions/misc_explosion/sprites/`

### Background
- `assets/backgrounds/space_shooter/bg-stars.png`
- Fallback:
  - `assets/backgrounds/space_background_pack/blue-with-stars.png`

### Audio
- Laser:
  - `assets/audio/grotto/laser.wav`
  - `assets/audio/sewers/rainbowlaser.ogg`
- Explosion:
  - `assets/audio/space_shooter/explosion.wav`
  - `assets/audio/sewers/explosion.wav`

## Decisions (Task #1)
- Resolution: 320x180 base, integer scaled
- Controls: WASD + Arrow keys both active; Space to shoot
- Enemy bullets: straight down
