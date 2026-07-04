# Game Engine Internals

## Overview

`GameEngine` is a plain **.NET 10 class library** with no Blazor or browser
dependencies. All game state is held in a single `GameState` instance and
mutated each frame by `GameEngine.Update(float dt)`.

---

## GameState

`GameState` is the single source of truth serialised to JSON each frame and
sent to the JavaScript renderer.

| Property | Type | Description |
|---|---|---|
| `Status` | `GameStatus` | Current game phase |
| `Score` | `int` | Running score |
| `HighScore` | `int` | Best score (loaded from `localStorage`) |
| `Wave` | `int` | Current wave number (1-based) |
| `TorpedoCount` | `int` | Loaded torpedoes (0 – 2) |
| `IsReloading` | `bool` | Reload in progress |
| `ReloadTimer` | `float` | Seconds elapsed during reload |
| `Ships` | `List<Ship>` | Active ships on screen |
| `Torpedoes` | `List<Torpedo>` | Active torpedoes in flight |
| `Explosions` | `List<Explosion>` | Active explosion effects |
| `FloatingTexts` | `List<FloatingText>` | Score pop-up labels |
| `ComboCount` | `int` | Consecutive hits without a miss or escape |
| `ComboTimer` | `float` | Seconds until combo resets |
| `ShipsEscaped` | `int` | Ships that reached the screen edge |
| `ShakeTimer` | `float` | Screen-shake seconds remaining |
| `WaveStartTimer` | `float` | Seconds since current wave became active |
| `SelectedTube` | `int` | Index into `TorpedoTubes` — currently aimed tube |
| `TubeAnglesDeg` | `float[]` | Serialised copy of `TorpedoTubes.AngleDeg` for the JS renderer |
| `TubeCount` | `int` | Serialised copy of `TorpedoTubes.Count` |

### Constants

| Constant | Value | Meaning |
|---|---|---|
| `MaxTorpedoes` | `2` | Tube capacity |
| `ReloadDuration` | `4 f` | Seconds for a full reload |
| `MaxEscaped` | `5` | Ships escaped before game over |
| `WaveClearPause` | `3 f` | Inter-wave delay (seconds) |
| `ComboTimeout` | `2.5 f` | Seconds of inactivity before combo resets |

---

## GameStatus Enum

```
StartScreen → Playing → WaveClear → Playing → … → GameOver
                 ↕
              Paused
```

| Value | Meaning |
|---|---|
| `StartScreen` | Title screen, waiting for first click |
| `Playing` | Active gameplay |
| `WaveClear` | Brief inter-wave pause (3 s) |
| `Paused` | Game frozen, "P" resumes |
| `GameOver` | Player lost; shows stats |

---

## Update Loop

`GameEngine.Update(float dt)` is called by the Blazor `System.Timers.Timer` at
≈ 60 fps (16 ms intervals). `dt` is clamped to `[0, 0.1]` seconds to prevent
spiral-of-death on lag spikes.

### Playing phase — execution order

1. Decrement `ComboTimer`; reset `ComboCount` when it hits zero.
2. Advance `WaveStartTimer` (banner fade).
3. Decay `ShakeTimer`.
4. Animate `FloatingTexts` (drift upward, fade out).
5. **Spawn** — add a ship when `SpawnTimer ≥ SpawnIntervalSeconds` and quota not reached.
6. **Move ships** — translate by `CurrentSpeed × Direction` each frame.
7. **Escape check** — ship beyond screen edge → increment `ShipsEscaped`, reset combo.
8. **Fire particles** — generate and age particles on `Burning` ships.
9. Prune inactive ships.
10. **Game over** check — `ShipsEscaped ≥ MaxEscaped`.
11. **Move torpedoes** — translate by `(Vx, Vy)`; deactivate if out of bounds.
12. Prune inactive torpedoes.
13. **Reload** — advance `ReloadTimer`; restore `TorpedoCount` when complete.
14. **`CollisionDetector.Detect(state)`**.
15. Animate and prune explosions.
16. **Wave-clear** check — all ships spawned and none remaining.

### WaveClear phase

Advances `WaveClearTimer`. When it exceeds `WaveClearPause` (3 s):

- Increments `Wave`.
- Resets per-wave counters (`ShipsSpawnedThisWave`, `TorpedosFired`, etc.).
- Restores `TorpedoCount` to `MaxTorpedoes`.
- Transitions to `Playing`.

---

## CollisionDetector

`CollisionDetector.Detect(GameState)` is a **static** method that iterates
every active, non-sinking torpedo against every active, non-sinking ship.

### Hit detection — `Ship.CheckCollision(float torpX, float torpY)`

Collision is accepted when:

1. **Horizontal**: `|torpX − ship.X| < ship.Width / 2`
2. **Vertical**: torpedo is within the *waterline hit band* (lower 30 % of
   sprite height, minimum 10 px) plus a 6 px downward tolerance.

### Damage resolution

| Ship state before hit | `RequiresTwoHits` | Outcome |
|---|---|---|
| `Healthy` | `false` | → `Sinking`; award `BasePoints × multiplier` |
| `Healthy` | `true` | → `Burning`; award `HitPoints × multiplier` |
| `Burning` | `true` | → `Sinking`; award `KillPoints × multiplier` |

`HitPoints = BasePoints × 0.30`  
`KillPoints = BasePoints × 0.70`

### Bonus torpedo drop

When a `Cargo`, `Cruiser`, `Tanker`, or `Carrier` is sunk, there is a **30 %
chance** to award a bonus torpedo (capped at `MaxTorpedoes`).

---

## DifficultyManager

### GetWave(int waveNumber)

Returns a `WaveConfig` record.  
Waves 1 – 10 are hand-tuned; beyond wave 10 scaling is procedural:

```
TotalShips           = 25 + (wave − 10) × 3
SpeedMultiplier      = 3.0 + (wave − 10) × 0.15
SpawnIntervalSeconds = max(0.5, 0.8 − (wave − 10) × 0.05)
WaveBonusPoints      = 3200 + (wave − 10) × 500
```

### Predefined waves

| Wave | Ships | Speed | Interval (s) | Bonus pts |
|---:|---:|---:|---:|---:|
| 1 | 6 | ×1.0 | 3.0 | 500 |
| 2 | 8 | ×1.1 | 2.7 | 600 |
| 3 | 10 | ×1.2 | 2.4 | 700 |
| 4 | 12 | ×1.3 | 2.1 | 900 |
| 5 | 14 | ×1.5 | 1.8 | 1 100 |
| 6 | 15 | ×1.7 | 1.6 | 1 300 |
| 7 | 18 | ×2.0 | 1.4 | 1 600 |
| 8 | 20 | ×2.2 | 1.2 | 2 000 |
| 9 | 22 | ×2.5 | 1.0 | 2 500 |
| 10 | 25 | ×3.0 | 0.8 | 3 200 |

### PickShipType(WaveConfig)

Builds a weighted pool of `ShipType` values and picks one at random:

| Ship type | Base weight | Availability |
|---|---|---|
| Destroyer | 5 | All waves |
| Cargo | 3 | All waves |
| PT Boat | 2 + `ExtraPtBoatWeight` | All waves |
| Fishing Boat | max(1, 5 − wave) | Waves 1 – 5 |
| Tanker | min(wave / 2, 3) | Wave 2+ |
| Cruiser | min(wave − 2, 4) | Wave 3+ |
| Carrier | min((wave − 4) / 2, 2) | Wave 6+ |

---

## Ship Model

### Factory — `Ship.Create(ShipType, float difficultyMultiplier, int direction, bool farLane)`

| Parameter | Effect |
|---|---|
| `difficultyMultiplier` | Scales `BaseSpeed` |
| `direction` | `1` = spawns left, moves right; `−1` = spawns right, moves left |
| `farLane` | `true` → `DepthScale = 0.55`, dimensions ×0.55, points ×1.8 |

### Ship types — near-lane stats

| Type | Width | Height | Base speed | Base pts | Two-hit |
|---|---:|---:|---:|---:|---:|
| Destroyer | 130 | 45 | 1.5 | 100 | No |
| PT Boat | 90 | 30 | 3.0 | 200 | No |
| Cargo | 160 | 55 | 1.0 | 150 | Yes |
| Cruiser | 155 | 52 | 1.2 | 300 | Yes |
| Fishing Boat | 95 | 32 | 0.7 | 75 | No |
| Tanker | 185 | 60 | 0.6 | 400 | Yes |
| Carrier | 220 | 68 | 0.45 | 700 | Yes |

("Base speed" is expressed relative to Cargo; the actual `BaseSpeed` values in
`Ship.cs` are px/s and multiplied by the current wave's `SpeedMultiplier`.)

### ShipDamageState

| State | Description |
|---|---|
| `Healthy` | Full speed, no visual damage |
| `Burning` | 30 % of base speed (`Ship.BurningSpeedMultiplier`), fire particles, requires second torpedo to sink |
| `Sinking` | Rotating, sinking animation (1.5 s), then removed |

---

## Input Handling

All input is bridged from JavaScript via `[JSInvokable]` methods on `Game.razor`.

| Method | Trigger | Action |
|---|---|---|
| `OnMouseMove(x, y)` | `mousemove` | Updates `SelectedTube` via `ComputeAimedTube` |
| `OnClick(x, y)` | `click` | Start game or fire torpedo |
| `OnKeyDown(key)` | `keydown` | Fire, pan tubes, pause |

### TorpedoTubes geometry

Tube angles are computed once in `TorpedoTubes` (not hardcoded separately in
the engine and the renderer). They're spaced so each tube's line crosses the
main ship lane (`ReferenceLaneY = 420`) at evenly-spaced X positions, rather
than being spaced evenly by degree — evenly-by-degree spacing bunches the
X-crossings near the centre and leaves large gaps near the edges.

### `ComputeAimedTube(float mouseX, float mouseY)`

Finds whichever tube's `TargetX` is closest to `mouseX` (nearest-neighbour)
and returns its index `[0, TorpedoTubes.Count - 1]`. Tube 2 (`DefaultTube`,
centre) fires straight up.

### `FireTorpedoFromTube(int tubeIndex)`

| Tube | Angle | Crosses the lane at |
|---:|---:|---|
| 0 | −57.0 ° | x = 240 |
| 1 | −37.6 ° | x = 440 |
| 2 | 0 ° | x = 640 |
| 3 | +37.6 ° | x = 840 |
| 4 | +57.0 ° | x = 1040 |

(A wider 7-tube fan spanning x = 40..1240 was tried and reverted — the outer
two tubes' crosshairs fell inside the periscope vignette's dark corners and
were effectively unusable even though they fired correctly.)

Torpedo velocity components (`speed = 300`):

```
Vx = sin(angleRad) × speed
Vy = −cos(angleRad) × speed
```

Returns `false` (and does not fire) if the status is not `Playing`, there are
no torpedoes, or a reload is in progress.
