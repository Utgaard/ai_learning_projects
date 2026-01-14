import random


WIDTH = 800
HEIGHT = 600
TITLE = "Dodge MVP"

PLAYER_SIZE = 50
PLAYER_SPEED = 5
OBSTACLE_SIZE = (50, 30)
OBSTACLE_SPEED = 4
SPAWN_INTERVAL = 1.0
SCORE_COLOR = "white"
PLAYER_START = (WIDTH // 2 - PLAYER_SIZE // 2, HEIGHT // 2 - PLAYER_SIZE // 2)

player = Rect(PLAYER_START, (PLAYER_SIZE, PLAYER_SIZE))
obstacles = []
game_over = False
score = 0.0


def reset_game():
    global game_over, score
    player.topleft = PLAYER_START
    obstacles.clear()
    score = 0.0
    game_over = False


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
        obstacle.y += OBSTACLE_SPEED
    # Keep only obstacles still on screen
    obstacles[:] = [o for o in obstacles if o.top < HEIGHT]


def update(dt):
    global game_over, score

    if game_over:
        return

    dx = (1 if keyboard.right else 0) - (1 if keyboard.left else 0)
    dy = (1 if keyboard.down else 0) - (1 if keyboard.up else 0)

    player.x += dx * PLAYER_SPEED
    player.y += dy * PLAYER_SPEED
    clamp_player()
    move_obstacles()
    score += dt

    if any(obstacle.colliderect(player) for obstacle in obstacles):
        game_over = True


def on_key_down(key):
    if key == keys.R:
        reset_game()


def draw():
    screen.fill((30, 30, 40))
    screen.draw.filled_rect(player, (200, 200, 255))
    for obstacle in obstacles:
        screen.draw.filled_rect(obstacle, (220, 80, 80))
    screen.draw.text(f"Score: {int(score)}", topleft=(10, 10), fontsize=36, color=SCORE_COLOR)
    if game_over:
        screen.draw.text("Game Over, Hit R to restart", center=(WIDTH // 2, HEIGHT // 2), fontsize=64, color="white")


clock.schedule_interval(spawn_obstacle, SPAWN_INTERVAL)
