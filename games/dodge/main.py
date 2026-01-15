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

PLAYER_START = (WIDTH // 2 - PLAYER_SIZE // 2, HEIGHT // 2 - PLAYER_SIZE // 2)
PLAYER_IMAGE = "player"
OBSTACLE_IMAGE = "asteroid"

ASSET_ROOT = Path(__file__).resolve().parent
if not (ASSET_ROOT / "music").is_dir():
    ASSET_ROOT = Path.cwd()
    if not (ASSET_ROOT / "music").is_dir():
        ASSET_ROOT = Path.cwd() / "games" / "dodge"
set_root(ASSET_ROOT)

player = Rect(PLAYER_START, (PLAYER_SIZE, PLAYER_SIZE))
obstacles = []
game_over = False
score = 0.0
spawn_timer = 0.0
current_obstacle_speed = BASE_OBSTACLE_SPEED
current_spawn_interval = BASE_SPAWN_INTERVAL
lives = LIVES_START
explode_played = False
music_playing = False


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
    global game_over, score, spawn_timer, current_obstacle_speed, current_spawn_interval, lives, explode_played
    player.topleft = PLAYER_START
    obstacles.clear()
    score = 0.0
    spawn_timer = 0.0
    current_obstacle_speed = BASE_OBSTACLE_SPEED
    current_spawn_interval = BASE_SPAWN_INTERVAL
    lives = LIVES_START
    explode_played = False
    game_over = False
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
    obstacles.append(Rect((x, -OBSTACLE_SIZE[1]), OBSTACLE_SIZE))


def move_obstacles():
    for obstacle in obstacles:
        obstacle.y += current_obstacle_speed
    # Keep only obstacles still on screen
    obstacles[:] = [o for o in obstacles if o.top < HEIGHT]


def get_input_vector():
    dx = (1 if keyboard.right else 0) - (1 if keyboard.left else 0)
    dy = (1 if keyboard.down else 0) - (1 if keyboard.up else 0)
    return dx, dy


def update_player(dx, dy):
    player.x += dx * PLAYER_SPEED
    player.y += dy * PLAYER_SPEED
    clamp_player()


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


def handle_collisions():
    global game_over, lives, explode_played
    hit_obstacle = next((obstacle for obstacle in obstacles if obstacle.colliderect(player)), None)
    if not hit_obstacle:
        return
    obstacles.remove(hit_obstacle)
    lives -= 1
    sounds.hit.play()
    if lives <= 0:
        game_over = True
        stop_music()
        if not explode_played:
            sounds.explode.play()
            explode_played = True


def update(dt):
    global game_over

    if game_over:
        return

    dx, dy = get_input_vector()
    update_player(dx, dy)
    update_difficulty(dt)
    update_spawning(dt)
    move_obstacles()
    handle_collisions()


def on_key_down(key):
    if key == keys.R:
        reset_game()


def draw():
    screen.fill((30, 30, 40))
    screen.blit(PLAYER_IMAGE, player.topleft)
    for obstacle in obstacles:
        screen.blit(OBSTACLE_IMAGE, obstacle.topleft)
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
