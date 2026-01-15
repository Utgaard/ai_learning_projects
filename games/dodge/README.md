# Dodge

A small 2D dodge game built in Python using **Pygame Zero**.

This project is part of a personal experiment in **human–AI collaboration**: defining clear goals and constraints, letting an AI implement incrementally, and keeping a tight human review loop.

## Gameplay

- Move the player to avoid falling obstacles
- Survive as long as possible to increase your score
- Difficulty ramps up over time
- Collision ends the game
- Restart instantly and try again

## Controls

- **Arrow keys** – Move player
- **R** – Restart after game over

## Requirements

- Python 3.11+ (tested with Python 3.13)
- Pygame Zero

## Setup

From the repository root:

```powershell
python -m venv .venv
.venv\Scripts\activate
pip install pgzero
