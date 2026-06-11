# The Expurgation in Cygnus

> A roguelike action-survival game with retro-futuristic pixel art, built in Unity as a university project.

[![Unity](https://img.shields.io/badge/Unity-6-black?logo=unity&logoColor=white)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-darkgreen?logo=csharp&logoColor=white)](https://docs.microsoft.com/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue?logo=windows&logoColor=white)](https://github.com/Erlandsonjr/the-expurgation-in-cygnus)
[![GDD](https://img.shields.io/badge/Document-GDD%20PDF-red?logo=adobeacrobatreader&logoColor=white)](GDD%20The%20Expurgation%20in%20Cygnus.pdf)

---

## About

The mining planet **Cygnus** has been invaded by the alien threat **Arkano**. Take control of the **Purger**, the planet's last defender, and fight for survival against ever-growing waves of invaders across **10 rounds of intense combat**.

Surviving each wave unlocks a **Choice Matrix** — a random selection of upgrade cards that shape your combat style in real time.

---

## Key Features

- **Wave-based combat** — 10 rounds with progressive difficulty
- **Choice Matrix (Upgrades)** — pick from random cards at the end of each wave (Damage, Speed, Healing)
- **Diverse arsenal** — *Heavy Rifle*, *Explosive Bow*, and *Laser Pistol*
- **Tactical dash** — directional dodge with cooldown for in-combat repositioning
- **Original soundtrack** — atmospheric, cybernetic synth-rock composed for the game
- **32-bit pixel art** retro-futuristic sci-fi setting

---

## Controls

| Key / Button | Action |
|---|---|
| `A` / `D` | Move Left / Right |
| `Space` | Jump |
| `LMB` (left click) | Shoot |
| `RMB` (right click) | Dash (Tactical Dodge) |
| `ESC` | Pause |

---

## How to Play (Windows Build)

1. Download `build.zip` from this repository.
2. Extract **all contents** of the `.zip` into a folder.
3. Run `The Expurgation in Cygnus.exe`.

> The executable **must stay in the same folder** as `_Data/` — moving it separately will prevent the game from launching.

---

## Technologies

| Tool | Purpose |
|---|---|
| Unity 6 | Game engine and editor |
| C# | All game scripts and logic |
| Universal Render Pipeline (URP) | Rendering and visual post-processing |
| TextMesh Pro | UI and typography |
| Unity Input System | Player input handling |

---

## Project Structure

```
Assets/
├── _Scripts/            # C# scripts (player, enemies, wave system, upgrades)
├── _Animations/         # Animation Controllers and clips
├── _Audio/              # Soundtrack and sound effects
├── _Prefabs/            # Enemy, projectile, and UI prefabs
├── _ScriptableObjects/  # Upgrade data and weapon configurations
├── _Sprites/            # Pixel art (characters, tiles, UI)
├── _Settings/           # URP and Input System settings
└── Scenes/              # Unity scenes (menu, gameplay)
```

---

## Documentation

A complete **Game Design Document (GDD)** is available in this repository:

[GDD The Expurgation in Cygnus.pdf](GDD%20The%20Expurgation%20in%20Cygnus.pdf)

---

## Team

Developed by **Erlandson Junior** and **Aderson Mesquita** as a **Game Development** course project.
