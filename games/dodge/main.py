import math
import random
from pathlib import Path
from pgzero.loaders import set_root


WIDTH = 800
HEIGHT = 600
TITLE = "Dodge MVP"

PLAYER_SIZE = 50
PLAYER_SPEED = 5
OBSTACLE_SIZE = (50, 30)

BASE_OBSTACLE_SPEED = 4
BASE_SPAWN_INTERVAL = 1.0
MIN_SPAWN_INTERVAL = 0.3
SPEED_RAMP = 0.3
SPAWN_RAMP = 0.04

LIVES_START = 3
SCORE_COLOR = "white"
SPARK_COUNT = 20
SPARK_SPEED = 220
SPARK_LIFETIME = 0.6
GAME_OVER_SPARK_COUNT = 80
GAME_OVER_SPARK_SPEED = 360
GAME_OVER_SPARK_LIFETIME = 1.0
EXHAUST_SPAWN_RATE = 120.0
EXHAUST_LIFETIME = 0.35
EXHAUST_SPEED = 140
EXHAUST_SPREAD = 12

PLAYER_START = (WIDTH // 2 - PLAYER_SIZE // 2, HEIGHT // 2 - PLAYER_SIZE // 2)
PLAYER_IMAGE = "player"
OBSTACLE_IMAGE = "asteroid"
BANK_ANGLE = 30
BANK_TIME = 0.2  # seconds to reach full bank
OBSTACLE_ROT_SPEED_RANGE = (-120, 120)
DEATH_SPIN_SPEED = 1080
DEATH_FALL_SPEED = 240
DEATH_OFFSCREEN_MARGIN = 40

ASSET_ROOT = Path(__file__).resolve().parent
if not (ASSET_ROOT / "music").is_dir():
    ASSET_ROOT = Path.cwd()
    if not (ASSET_ROOT / "music").is_dir():
        ASSET_ROOT = Path.cwd() / "games" / "dodge"
set_root(ASSET_ROOT)

player = Rect(PLAYER_START, (PLAYER_SIZE, PLAYER_SIZE))
player_sprite = Actor(PLAYER_IMAGE, topleft=PLAYER_START)
obstacles = []
game_over = False
score = 0.0
spawn_timer = 0.0
current_obstacle_speed = BASE_OBSTACLE_SPEED
current_spawn_interval = BASE_SPAWN_INTERVAL
lives = LIVES_START
explode_played = False
music_playing = False
player_angle = 0.0
sparks = []
player_state = "alive"
player_spin_speed = 0.0
player_fall_speed = 0.0
exhaust_particles = []
exhaust_timer = 0.0


def start_music():
    global music_playing
    if not music_playing:
        music.play("theme")
        music.set_volume(0.2)
        music_playing = True


def stop_music():
    global music_playing
    if music_playing:
        music.stop()
        music_playing = False


def reset_game():
    global game_over, score, spawn_timer, current_obstacle_speed, current_spawn_interval, lives, explode_played, player_angle, sparks, player_state, player_spin_speed, player_fall_speed, exhaust_particles, exhaust_timer
    player.topleft = PLAYER_START
    player_sprite.topleft = PLAYER_START
    player_angle = 0.0
    player_sprite.angle = player_angle
    obstacles.clear()
    sparks.clear()
    exhaust_particles.clear()
    exhaust_timer = 0.0
    score = 0.0
    spawn_timer = 0.0
    current_obstacle_speed = BASE_OBSTACLE_SPEED
    current_spawn_interval = BASE_SPAWN_INTERVAL
    lives = LIVES_START
    explode_played = False
    game_over = False
    player_state = "alive"
    player_spin_speed = 0.0
    player_fall_speed = 0.0
    stop_music()
    start_music()


def clamp_player():
    # Keep the player fully on screen by clamping its top-left corner.
    player.x = max(0, min(player.x, WIDTH - PLAYER_SIZE))
    player.y = max(0, min(player.y, HEIGHT - PLAYER_SIZE))


def spawn_obstacle():
    if game_over:
        return
    x = random.randint(0, WIDTH - OBSTACLE_SIZE[0])
    rect = Rect((x, -OBSTACLE_SIZE[1]), OBSTACLE_SIZE)
    sprite = Actor(OBSTACLE_IMAGE, topleft=rect.topleft)
    rotation_speed = random.uniform(OBSTACLE_ROT_SPEED_RANGE[0], OBSTACLE_ROT_SPEED_RANGE[1])
    obstacles.append(
        {
            "rect": rect,
            "sprite": sprite,
            "angle": 0.0,
            "rotation_speed": rotation_speed,
        }
    )


def move_obstacles():
    for obstacle in obstacles:
        obstacle["rect"].y += current_obstacle_speed
        obstacle["sprite"].topleft = obstacle["rect"].topleft
    # Keep only obstacles still on screen
    obstacles[:] = [o for o in obstacles if o["rect"].top < HEIGHT]


def rotate_obstacles(dt):
    for obstacle in obstacles:
        obstacle["angle"] = (obstacle["angle"] + obstacle["rotation_speed"] * dt) % 360
        obstacle["sprite"].angle = obstacle["angle"]


def spawn_sparks(position, count=SPARK_COUNT, speed=SPARK_SPEED, lifetime=SPARK_LIFETIME):
    x, y = position
    for _ in range(count):
        angle = random.uniform(0, 360)
        spark_speed = random.uniform(speed * 0.5, speed)
        vx = math.cos(math.radians(angle)) * spark_speed
        vy = math.sin(math.radians(angle)) * spark_speed
        sparks.append(
            {
                "x": x + random.uniform(-10, 10),
                "y": y + random.uniform(-10, 10),
                "vx": vx,
                "vy": vy,
                "age": 0.0,
                "lifetime": lifetime,
            }
        )


def update_sparks(dt):
    for spark in sparks:
        spark["x"] += spark["vx"] * dt
        spark["y"] += spark["vy"] * dt
        spark["age"] += dt
    sparks[:] = [spark for spark in sparks if spark["age"] < spark["lifetime"]]


def spawn_exhaust(dt):
    global exhaust_timer
    exhaust_timer += dt
    spawn_interval = 1.0 / EXHAUST_SPAWN_RATE
    while exhaust_timer >= spawn_interval:
        exhaust_timer -= spawn_interval
        angle_rad = math.radians(player_angle)
        back_x = player.centerx
        back_y = player.bottom
        jitter_x = random.uniform(-4, 4)
        jitter_y = random.uniform(-4, 4)
        spread = math.radians(random.uniform(-EXHAUST_SPREAD, EXHAUST_SPREAD))
        dir_angle = angle_rad + math.pi + spread
        speed = random.uniform(EXHAUST_SPEED * 0.6, EXHAUST_SPEED)
        exhaust_particles.append(
            {
                "x": back_x + jitter_x,
                "y": back_y + jitter_y,
                "vx": math.sin(dir_angle) * speed,
                "vy": -math.cos(dir_angle) * speed,
                "age": 0.0,
            }
        )


def update_exhaust(dt):
    for particle in exhaust_particles:
        particle["x"] += particle["vx"] * dt
        particle["y"] += particle["vy"] * dt
        particle["age"] += dt
    exhaust_particles[:] = [p for p in exhaust_particles if p["age"] < EXHAUST_LIFETIME]


def get_input_vector():
    dx = (1 if keyboard.right else 0) - (1 if keyboard.left else 0)
    dy = (1 if keyboard.down else 0) - (1 if keyboard.up else 0)
    return dx, dy


def update_player(dx, dy):
    player.x += dx * PLAYER_SPEED
    player.y += dy * PLAYER_SPEED
    clamp_player()
    player_sprite.topleft = player.topleft


def update_difficulty(dt):
    global score, current_obstacle_speed, current_spawn_interval
    score += dt
    current_obstacle_speed = BASE_OBSTACLE_SPEED + score * SPEED_RAMP
    current_spawn_interval = max(MIN_SPAWN_INTERVAL, BASE_SPAWN_INTERVAL - score * SPAWN_RAMP)


def update_spawning(dt):
    global spawn_timer
    spawn_timer += dt
    while spawn_timer >= current_spawn_interval:
        spawn_obstacle()
        spawn_timer -= current_spawn_interval


def update_bank_angle(dx, dt):
    global player_angle
    if dx < 0:
        target_angle = BANK_ANGLE
    elif dx > 0:
        target_angle = -BANK_ANGLE
    else:
        target_angle = 0
    max_step = (BANK_ANGLE / BANK_TIME) * dt
    delta = target_angle - player_angle
    if abs(delta) <= max_step:
        player_angle = target_angle
    else:
        player_angle += max_step if delta > 0 else -max_step
    player_sprite.angle = player_angle


def update_death(dt):
    global player_angle, player_state
    player.y += player_fall_speed * dt
    player_angle = (player_angle + player_spin_speed * dt) % 360
    player_sprite.angle = player_angle
    player_sprite.topleft = player.topleft
    if player.top > HEIGHT + DEATH_OFFSCREEN_MARGIN:
        player_state = "dead"


def handle_collisions():
    global game_over, lives, explode_played, player_state, player_spin_speed, player_fall_speed
    hit_obstacle = next((obstacle for obstacle in obstacles if obstacle["rect"].colliderect(player)), None)
    if not hit_obstacle:
        return
    obstacles.remove(hit_obstacle)
    spawn_sparks(player.center)
    lives -= 1
    sounds.hit.play()
    if lives <= 0:
        game_over = True
        stop_music()
        player_state = "dying"
        player_spin_speed = DEATH_SPIN_SPEED
        player_fall_speed = DEATH_FALL_SPEED
        spawn_sparks(
            player.center,
            count=GAME_OVER_SPARK_COUNT,
            speed=GAME_OVER_SPARK_SPEED,
            lifetime=GAME_OVER_SPARK_LIFETIME,
        )
        if not explode_played:
            sounds.explode.play()
            explode_played = True


def update(dt):
    global game_over

    if game_over and player_state == "dying":
        update_death(dt)
        update_sparks(dt)
        return

    if game_over:
        update_sparks(dt)
        return

    if player_state != "alive":
        update_sparks(dt)
        return

    dx, dy = get_input_vector()
    update_player(dx, dy)
    update_bank_angle(dx, dt)
    #spawn_exhaust(dt)
    update_exhaust(dt)
    update_difficulty(dt)
    update_spawning(dt)
    move_obstacles()
    rotate_obstacles(dt)
    handle_collisions()
    update_sparks(dt)


def on_key_down(key):
    if key == keys.R:
        reset_game()


def draw():
    screen.fill((30, 30, 40))
    if player_state != "dead":
        player_sprite.draw()
    for obstacle in obstacles:
        obstacle["sprite"].draw()
    for spark in sparks:
        t = max(0.0, 1.0 - (spark["age"] / spark["lifetime"]))
        intensity = int(255 * t)
        radius = max(1, int(3 * t))
        screen.draw.filled_circle((spark["x"], spark["y"]), radius, (255, intensity, 0))
    for particle in exhaust_particles:
        t = max(0.0, 1.0 - (particle["age"] / EXHAUST_LIFETIME))
        intensity = int(200 * t)
        radius = max(1, int(2 * t))
        screen.draw.filled_circle((particle["x"], particle["y"]), radius, (255, 140 + intensity // 2, 0))
    screen.draw.text(f"Score: {int(score)}", topleft=(10, 10), fontsize=36, color=SCORE_COLOR)
    screen.draw.text(
        f"Speed: {current_obstacle_speed:.1f}  Lives: {lives}",
        topright=(WIDTH - 10, 10),
        fontsize=28,
        color=SCORE_COLOR,
    )
    if game_over:
        screen.draw.text("Game Over, Hit R to restart", center=(WIDTH // 2, HEIGHT // 2), fontsize=64, color="white")


reset_game()
