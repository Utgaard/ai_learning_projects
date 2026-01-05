WIDTH = 800
HEIGHT = 600
TITLE = "Dodge MVP"

PLAYER_SIZE = 50
PLAYER_SPEED = 5
player = Rect((WIDTH // 2 - PLAYER_SIZE // 2, HEIGHT // 2 - PLAYER_SIZE // 2), (PLAYER_SIZE, PLAYER_SIZE))


def clamp_player():
    # Keep the player fully on screen by clamping its top-left corner.
    player.x = max(0, min(player.x, WIDTH - PLAYER_SIZE))
    player.y = max(0, min(player.y, HEIGHT - PLAYER_SIZE))


def update():
    dx = (1 if keyboard.right else 0) - (1 if keyboard.left else 0)
    dy = (1 if keyboard.down else 0) - (1 if keyboard.up else 0)

    player.x += dx * PLAYER_SPEED
    player.y += dy * PLAYER_SPEED
    clamp_player()


def draw():
    screen.fill((30, 30, 40))
    screen.draw.filled_rect(player, (200, 200, 255))
