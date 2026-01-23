import Phaser from 'phaser';

const BASE_WIDTH = 320;
const BASE_HEIGHT = 180;

const config = {
  type: Phaser.AUTO,
  parent: 'game-container',
  width: BASE_WIDTH,
  height: BASE_HEIGHT,
  pixelArt: true,
  physics: {
    default: 'arcade',
    arcade: { debug: false }
  },
  scale: {
    mode: Phaser.Scale.FIT,
    autoCenter: Phaser.Scale.CENTER_BOTH
  },
  scene: {
    preload,
    create,
    update
  }
};

new Phaser.Game(config);

let player;
let cursors;
let wasd;
let bullets;
let lastShotTime = 0;
let scoreText;
let laserSound;
let background;

function preload() {
  this.load.image('bg', 'assets/backgrounds/space_shooter/bg-stars.png');
  this.load.image('player', 'assets/player/space_shooter/sprites/player1.png');
  this.load.image('bullet', 'assets/projectiles/sewers/bullet.png');
  this.load.audio('laser', 'assets/audio/grotto/laser.wav');
}

function create() {
  background = this.add.image(0, 0, 'bg').setOrigin(0, 0);
  background.setDisplaySize(BASE_WIDTH, BASE_HEIGHT);

  player = this.physics.add.sprite(BASE_WIDTH / 2, BASE_HEIGHT - 30, 'player');
  player.setCollideWorldBounds(true);

  bullets = this.physics.add.group({
    classType: Phaser.Physics.Arcade.Image,
    maxSize: 30,
    runChildUpdate: true
  });

  cursors = this.input.keyboard.createCursorKeys();
  wasd = this.input.keyboard.addKeys({
    up: Phaser.Input.Keyboard.KeyCodes.W,
    down: Phaser.Input.Keyboard.KeyCodes.S,
    left: Phaser.Input.Keyboard.KeyCodes.A,
    right: Phaser.Input.Keyboard.KeyCodes.D,
    shoot: Phaser.Input.Keyboard.KeyCodes.SPACE
  });

  scoreText = this.add.text(6, 6, 'Score: 0', {
    fontSize: '8px',
    fill: '#e8f1ff'
  });

  laserSound = this.sound.add('laser', { volume: 0.4 });

  this.add.text(BASE_WIDTH / 2, BASE_HEIGHT - 6, 'Move: WASD / Arrows   Shoot: Space', {
    fontSize: '8px',
    fill: '#c7d2e6'
  }).setOrigin(0.5, 1);
}

function update(time) {
  const speed = 120;
  player.setVelocity(0);

  if (cursors.left.isDown || wasd.left.isDown) {
    player.setVelocityX(-speed);
  } else if (cursors.right.isDown || wasd.right.isDown) {
    player.setVelocityX(speed);
  }

  if (cursors.up.isDown || wasd.up.isDown) {
    player.setVelocityY(-speed);
  } else if (cursors.down.isDown || wasd.down.isDown) {
    player.setVelocityY(speed);
  }

  const wantsToShoot = cursors.space.isDown || wasd.shoot.isDown;
  if (wantsToShoot && time > lastShotTime + 200) {
    fireBullet.call(this);
    lastShotTime = time;
  }

  bullets.children.iterate((bullet) => {
    if (!bullet) return;
    if (bullet.y < -10) {
      bullets.killAndHide(bullet);
      bullet.body.enable = false;
    }
  });
}

function fireBullet() {
  const bullet = bullets.get(player.x, player.y - 10, 'bullet');
  if (!bullet) return;

  bullet.setActive(true);
  bullet.setVisible(true);
  bullet.body.enable = true;
  bullet.setVelocityY(-220);
  bullet.setOrigin(0.5, 1);

  if (laserSound) {
    laserSound.play();
  }
}
