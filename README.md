# Donkey Kong Game (C# WinForms)

## Authors
- 施丞軒  
- 范啟彥  
- 張祐銓
    
## Project Overview

This project is a **2D two-player platformer game** inspired by *Donkey Kong*, developed as a **final course project** using **C# and Windows Forms**.

The goal of this project is not only to create a playable game, but also to demonstrate how a **complete game architecture** can be built from scratch without using a game engine.  
It focuses on **clear system design**, **separation of responsibilities**, and **extensibility** for future content.

This project is intended for:
- Developers who want to understand how a 2D game can be implemented using pure C# (WinForms)
- Students who want to study game architecture, animation handling, and tile-based map systems

---

## Gameplay Features

### Two-Player Gameplay
- **Player 1 — 小明**: platforming-focused character
- **Player 2 — 小明兄弟**: enemy-style character with multiple attack types

### Win Conditions
- **Player 1 (小明)**  
  - 勝利條件：在血條歸零前抵達目標旗幟
- **Player 2 (小明兄弟)**  
  - 勝利條件：在Player 1抵達旗幟前用攻擊使他血條歸零

### Core Mechanics
- Tile-based movement and collision
- Jumping, climbing ladders, falling, and gravity
- Multiple weapon and projectile systems
- Health bar with reserve lives
- Win and death cutscenes with camera effects
- Pause menu and complete game flow control

### Weapons & Enemies
- **Bombs**: physics-based behavior (rolling, falling, ladder interaction, explosion animation)
- **Knives**: gravity-affected projectiles
- **Fireballs**: animated horizontal projectiles
- Multiple enemy types with patrol and attack behaviors

---

## Controls

### Player 1 (小明)

| Action | Key |
|------|----|
| Move | W / A / S / D |
| Jump | Space |

### Player 2 (小明兄弟)

| Action | Key |
|------|----|
| Move | Arrow Keys |
| Bomb Attack | NumPad 1 |
| Knife Attack | NumPad 2 |
| Fireball Attack | NumPad 3 |

---

## Map System Architecture

### Tile-Based Map Design
- Maps are created using **Tiled Map Editor**
- Each map layer is exported as an individual **CSV file**
- CSV files are loaded at runtime and converted into tile grids

### Layer-Based Structure
Each map consists of multiple layers, such as:
- Floor
- Wood / platforms
- Broken ladders
- Ladders
- Decorative or special tiles (vine, cactus, ice, etc.)
- Goal item (banana)

Each layer is stored as a `int[,]` grid, allowing:
- Independent collision logic per layer
- Flexible extension of new tile types
- Clean separation between visuals and logic

---

## Core System Design

### Game Loop
- Implemented using `System.Windows.Forms.Timer`
- Fixed update interval (~16ms, ~60 FPS)
- Each frame handles:
  - Input processing
  - Entity updates
  - Collision checks
  - Cutscene logic
  - Rendering

### Rendering Pipeline
- Custom rendering using `OnPaint`
- Manual control over drawing order:
  1. Background
  2. Map layers
  3. Projectiles
  4. Enemies
  5. Players
  6. UI (health bar, overlays)
- Camera effects implemented via `Graphics.Transform` (zoom & translation)

### Animation System
- Frame-based sprite animation
- Independent animation state machines for:
  - Idle
  - Run
  - Jump
  - Climb
  - Attack
  - Hurt
  - Death
- Animation speed controlled by frame counters (not time-based)

---

## Code Structure
```
DonkeyKongGame/
│
├── Form1.cs // Main menu & game entry
├── Program.cs // Application entry point
│
├── MapManager.cs // Map loading, CSV parsing, tile rendering
├── GameSelection.cs // Selected level / game state
│
├── map1.cs // Map 1 logic (these three could be integrated later)
├── map2.cs // Map 2 logic
├── map3.cs // Map 3 logic
│
├── Player.cs // Player 1 
├── Player2.cs // Player 2
│
├── Monster.cs // Enemy type (Map 1)
├── Monster2.cs // Enemy type (Map 2)
├── Monster3.cs // Enemy type (Map 3)
│
├── Bomb.cs // Bomb projectile (physics + explosion)
├── Knife.cs // Knife projectile
├── Fireball.cs // Fireball projectile
│
├── HealthBar.cs // Health & lives system
├── GameOverForm.cs // Win / Lose screen
├── SettingForm.cs // (Reserved for future use)
│
├── assets/ // Images, music, sound effects
├── outputCSV/ // Map CSV layers exported from Tiled
```

---

## Audio & Visual Assets

### Audio
- Background music for:
  - Main menu
  - Each map
- Sound effects for:
  - Attacks
  - Explosions
  - Player damage
  - Death animations

Audio playback is handled via **Windows `winmm.dll` (`mciSendString`)** to allow looping and overlapping sounds.

### Visual Assets
- Background images and some environment elements were generated using **AI-based image generation**
- Character and monster sprites are sourced from **free online asset resources**
- All assets are loaded dynamically at runtime

### Resources
- https://craftpix.net/freebies/free-3-character-sprite-sheets-pixel-art/?srsltid=AfmBOopst3hjESopUQCut_BrxqwayFSum8Hs2PyAnLu-sdsn10P4EcfF
- https://craftpix.net/freebies/free-rocks-pixel-art-asset-pack/?num=1&count=70&sq=rock&pos=2
- https://craftpix.net/freebies/free-swamp-bosses-pixel-art-character-pack/
- https://craftpix.net/freebies/free-gorgon-pixel-art-character-sprite-sheets/
- https://www.youtube.com/watch?v=9i53HSOS3U4
- https://pixabay.com/music/search/pixel/
  
---

## How to Run (currently unavailable)

1. Download the released file
2. Unzip and execute the file.

> Required resolution: Fullscreen (1920 × 1080)
> Required screen ratio：100%

---

## Notes & Future Improvements

- Refactor shared logic between `map1`, `map2`, and `map3`
- Extract cutscene logic into reusable systems
- Improve collision performance and structure
- Add more maps and enemy behaviors
- Expand the settings menu (audio, controls, graphics)

---
## Summary

This project demonstrates how a **complete 2D game** can be built using **pure C# and WinForms**, including:
- Custom rendering
- Physics and collision handling
- Animation systems
- Audio management
- Game state and flow control

It is designed to be both **playable** and **educational**, serving as a reference for students interested in low-level game implementation.
