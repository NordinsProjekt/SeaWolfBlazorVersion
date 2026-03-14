# Sea Wolf — Blazor Periscope Attack Simulation

A browser-based submarine arcade game built with **Blazor Server** and **.NET 10**.  
You command a submerged periscope, aim through five torpedo tubes, and sink enemy vessels before too many escape.

---

## Table of Contents

- [Features](#features)
- [Getting Started](#getting-started)
- [How to Play](#how-to-play)
- [Project Structure](#project-structure)
- [Documentation](#documentation)
- [Testing](#testing)

---

## Features

| Feature | Detail |
|---|---|
| Framework | Blazor Server — .NET 10 |
| Rendering | HTML `<canvas>` via modular JavaScript |
| Audio | Web Audio API (procedural synthesis, no audio files) |
| Persistence | High score saved in `localStorage` |
| Waves | 10 hand-tuned waves + infinite procedural scaling |
| Ship types | 7 — Destroyer, PT Boat, Cargo, Cruiser, Fishing Boat, Tanker, Carrier |
| Torpedo tubes | 5 angled tubes (−55 °, −25 °, 0 °, +25 °, +55 °) |
| Combo system | Up to ×4 score multiplier for chained kills |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Any modern browser (Chrome, Edge, Firefox)

### Run locally

```bash
git clone https://github.com/NordinsProjekt/SeaWolfBlazorVersion.git
cd SeaWolfBlazorVersion/SeaWolfBlazorVersion
dotnet run
```

Then open `https://localhost:5001` (or the port printed in the terminal).

---

## How to Play

| Input | Action |
|---|---|
| **Mouse move** | Aim — moves the active tube crosshair |
| **Left click** | Fire torpedo / Start game / Restart |
| **Space / Enter** | Fire torpedo / Start or restart game |
| **← → Arrow keys** | Step through torpedo tubes |
| **P** | Pause / Resume |

**Goal:** Sink ships before **5 escape**. Each wave adds faster, more numerous ships.  
Chain kills quickly to build a **combo multiplier** (×2 → ×3 → ×4).  
Accuracy above 80 % earns a wave-clear bonus.

---

## Project Structure

```
SeaWolfBlazorVersion/          ← solution root
├── GameEngine/                ← pure C# game-logic library
├── SeaWolfBlazorVersion/      ← Blazor Server web app
│   └── wwwroot/js/            ← modular canvas renderer + input bridge
├── GameEngineTests/           ← xUnit test project
└── docs/                      ← extended documentation
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for a full breakdown.

---

## Documentation

| File | Contents |
|---|---|
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Solution layout, layers, data-flow |
| [`docs/GAME_ENGINE.md`](docs/GAME_ENGINE.md) | C# engine internals — state, systems, models |
| [`docs/GAMEPLAY.md`](docs/GAMEPLAY.md) | Waves, ships, scoring, combo system |
| [`docs/JS_RENDERER.md`](docs/JS_RENDERER.md) | JavaScript rendering pipeline |

---

## Testing

```bash
cd GameEngineTests
dotnet test
```

82 tests covering `DifficultyManager`, `Ship`, `CollisionDetector`, and `GameEngine`.
