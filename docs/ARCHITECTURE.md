# Architecture

## Solution Layout

```
SeaWolfBlazorVersion.sln
├── GameEngine/                         ← class library  (no UI dependencies)
│   └── Engine/
│       ├── GameEngine.cs               ← update loop, input handlers
│       ├── GameState.cs                ← mutable snapshot of all game data
│       ├── CollisionDetector.cs        ← static torpedo-vs-ship detection
│       ├── DifficultyManager.cs        ← wave configs, ship-type distribution
│       └── Models/
│           ├── Ship.cs
│           ├── Torpedo.cs
│           ├── Explosion.cs
│           ├── FireParticle.cs
│           ├── FloatingText.cs
│           └── Enums/
│               ├── GameStatus.cs
│               ├── ShipDamageState.cs
│               └── ShipType.cs
│
├── SeaWolfBlazorVersion/               ← Blazor Server app
│   ├── Program.cs
│   ├── Services/
│   │   ├── AudioService.cs             ← Web Audio API bridge (IJSRuntime)
│   │   └── HighScoreService.cs         ← localStorage bridge (IJSRuntime)
│   ├── Components/
│   │   ├── App.razor                   ← HTML shell, script load order
│   │   ├── Routes.razor
│   │   └── Pages/
│   │       └── Game.razor              ← canvas host, game-loop timer, JS interop
│   └── wwwroot/
│       └── js/
│           ├── rendererCore.js         ← canvas/ctx, sprites, colour palette
│           ├── rendererBackground.js   ← sky, ocean, waves, stars, moon
│           ├── rendererShips.js        ← ship drawing, fire particles, wake
│           ├── rendererProjectiles.js  ← torpedoes, explosions, floating text
│           ├── rendererHUD.js          ← HUD, tube lines, periscope vignette
│           ├── rendererScreens.js      ← start/game-over/wave-clear/pause screens
│           ├── canvasRenderer.js       ← JS orchestrator (SeaWolfRenderer)
│           ├── inputBridge.js          ← mouse/keyboard → .NET interop
│           └── audio.js                ← Web Audio API synthesis
│
└── GameEngineTests/                    ← xUnit test project
    ├── DifficultyManagerTests.cs
    ├── ShipTests.cs
    ├── CollisionDetectorTests.cs
    └── GameEngineTests.cs
```

---

## Layers

```
┌─────────────────────────────────────────────────────┐
│               Browser (HTML / Canvas)                │
│  canvasRenderer.js  ←  renderer modules (6 files)   │
│  inputBridge.js  ←────── user input                 │
│  audio.js  ←──────────── sound synthesis             │
└───────────────────┬─────────────────────────────────┘
                    │  JS Interop (IJSRuntime)
┌───────────────────▼─────────────────────────────────┐
│              Blazor Server (Game.razor)               │
│  16 ms System.Timers.Timer game-loop                 │
│  AudioService  /  HighScoreService                   │
└───────────────────┬─────────────────────────────────┘
                    │  direct method calls
┌───────────────────▼─────────────────────────────────┐
│            GameEngine (class library)                 │
│  GameEngine.Update(dt)                               │
│  CollisionDetector.Detect(state)                     │
│  DifficultyManager.GetWave / PickShipType            │
└─────────────────────────────────────────────────────┘
```

---

## Data Flow — One Frame

```
System.Timers.Timer fires (≈16 ms)
    │
    ├─ GameEngine.Update(dt)
    │       ├─ advance ship positions
    │       ├─ advance torpedo positions
    │       ├─ CollisionDetector.Detect(state)
    │       ├─ update explosions / floating texts
    │       ├─ spawn new ships
    │       └─ check wave-clear / game-over
    │
    ├─ CheckAudioTriggers()   ← damage-state diffs → AudioService
    │
    └─ JsonSerializer.Serialize(state)
            │
            └─ JS: SeaWolfRenderer.renderFrame(json)
                        ├─ background.draw()
                        ├─ ships.drawShip() × N
                        ├─ projectiles.drawTorpedo() × N
                        ├─ projectiles.drawExplosion() × N
                        ├─ projectiles.drawFloatingText() × N
                        ├─ hud.drawTubeSpread()
                        ├─ hud.drawPeriscopeVignette()
                        └─ hud.drawHUD()
```

---

## Dependency Injection

Registered in `Program.cs` as **scoped** (one instance per SignalR circuit):

| Service | Interface |
|---|---|
| `HighScoreService` | `IJSRuntime` |
| `AudioService` | `IJSRuntime` |

---

## JavaScript Module Load Order

`App.razor` loads scripts in the following fixed order so every module is
available before the next one references it:

```html
<script src="js/audio.js"></script>
<script src="js/rendererCore.js"></script>
<script src="js/rendererBackground.js"></script>
<script src="js/rendererShips.js"></script>
<script src="js/rendererProjectiles.js"></script>
<script src="js/rendererHUD.js"></script>
<script src="js/rendererScreens.js"></script>
<script src="js/canvasRenderer.js"></script>   <!-- orchestrator, loaded last -->
<script src="js/inputBridge.js"></script>
<script src="_framework/blazor.web.js"></script>
```
