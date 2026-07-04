# Gameplay Guide

## Objective

Sink enemy ships passing across the screen before **5 of them escape**.  
Ships enter from either edge and move toward the opposite side.  
You fire torpedoes upward from a periscope at the bottom of the screen.

---

## Controls

| Input | Action |
|---|---|
| **Mouse move** | Aim — selects the tube whose line passes closest to the cursor |
| **Left click** | Fire torpedo (in-game) / Start or restart (title / game-over screen) |
| **Space / Enter** | Fire torpedo / Start or restart |
| **← Arrow** | Select the next tube to the left |
| **→ Arrow** | Select the next tube to the right |
| **P** | Pause / Resume |

---

## Torpedo Tubes

Five fixed tubes angle upward from the centre-bottom of the screen. The
angles aren't evenly spaced by degree — they're spaced so each tube's line
crosses the main ship lane at evenly-spaced X positions (every 200 px),
kept within the clearly-lit periscope circle so every tube's crosshair is
actually visible (a wider 7-tube spread was tried and reverted — the outer
two crosshairs landed in the dark vignetted corners and were effectively
unusable).

| Tube | Angle | Crosses the lane at |
|---:|---:|---|
| 0 | −57.0 ° | x = 240 (far left) |
| 1 | −37.6 ° | x = 440 |
| 2 | 0 ° | x = 640 (straight up / centre) |
| 3 | +37.6 ° | x = 840 |
| 4 | +57.0 ° | x = 1040 (far right) |

The active tube line is shown in bright green and glides smoothly toward the
newly-selected tube instead of snapping; inactive tubes stay dim and dashed.  
An aiming crosshair sits at the intersection of the active tube line and the
ship-lane band, and follows the same smooth motion.

**Capacity:** 2 torpedoes loaded at all times.  
**Reload:** When both are expended a **4-second reload** begins automatically.  
A yellow progress bar appears at the bottom of the screen during reload.

---

## Ships

Ships spawn off-screen and cross at varying depths and speeds.

### Near-lane ships (default)

| Ship | Width | Speed | Points | Hits to sink |
|---|---:|---:|---:|---:|
| Fishing Boat | 95 px | Slow | 75 | 1 |
| Destroyer | 130 px | Medium | 100 | 1 |
| PT Boat | 90 px | Fast | 200 | 1 |
| Cargo | 160 px | Slow | 150 | **2** |
| Cruiser | 155 px | Medium | 300 | **2** |
| Tanker | 185 px | Very slow | 400 | **2** |
| Carrier | 220 px | Very slow | 700 | **2** |

### Far-lane ships

From **wave 3** onwards, ships can appear in a **far lane** near the horizon.  
Far-lane ships are scaled to **55 %** of their normal size — harder to hit —
but award **1.8× base points**.

### Two-hit ships

Ships requiring two hits (`Cargo`, `Cruiser`, `Tanker`, `Carrier`) transition
through an intermediate **Burning** state after the first torpedo:

- Speed drops to **30 %** of normal — a deliberate, heavy crawl rather than a
  mild slowdown, so landing the first hit buys a real window to finish the
  ship off.
- Fire particles appear (rendered as flat pixel-art blocks, matching the ship
  sprites).
- A second torpedo finishes them off.

### Bonus torpedo drop

When a multi-hit ship is finally sunk, there is a **30 % chance** of a free
torpedo appearing as a floating text pop-up (`+TORP`), provided you are not
already at maximum capacity.

---

## Scoring

### Base points

Points are awarded when a ship is hit:

- **First hit (two-hit ships):** `HitPoints = BasePoints × 30 %`
- **Kill (any ship):** `KillPoints = BasePoints × 70 %` (single-hit) or full
  kill value (two-hit)

All awarded points are multiplied by the current **combo multiplier**.

### Combo multiplier

Consecutive torpedo hits without a miss or an escaped ship increase the
multiplier:

| Consecutive hits | Multiplier |
|---:|---:|
| 1 | ×1 |
| 2 – 3 | ×2 |
| 4 – 6 | ×3 |
| 7+ | ×4 |

The combo **resets** if:
- 2.5 seconds pass without a hit, **or**
- a ship escapes off the screen.

Pop-up labels showing `+<pts> ×<mult>` appear above destroyed ships in colour:

| Multiplier | Colour |
|---|---|
| ×1 | Light green |
| ×2 | Light green |
| ×3 | Gold |
| ×4 | Orange |

### Wave-clear bonus

When all ships in a wave are gone (sunk or escaped) a **wave bonus** is awarded:

| Accuracy | Extra bonus |
|---|---|
| ≥ 80 % | Wave bonus ÷ 2 |
| ≥ 60 % | Wave bonus ÷ 4 |
| < 60 % | None |

Wave bonus values range from **500 pts** (wave 1) to **3 200 pts** (wave 10)
and scale by **500 pts per wave** beyond wave 10.

---

## Waves

### Progression summary

| Wave | Ships | Speed | New ship types |
|---:|---:|---|---|
| 1 | 6 | ×1.0 | Destroyer, PT Boat, Cargo, Fishing Boat |
| 2 | 8 | ×1.1 | + Tanker |
| 3 | 10 | ×1.2 | + Cruiser, far-lane ships |
| 4 | 12 | ×1.3 | More PT Boats |
| 5 | 14 | ×1.5 | Last wave with Fishing Boats |
| 6 | 15 | ×1.7 | + Carrier (rare) |
| 7 | 18 | ×2.0 | — |
| 8 | 20 | ×2.2 | — |
| 9 | 22 | ×2.5 | — |
| 10 | 25 | ×3.0 | — |
| 11+ | +3/wave | +0.15/wave | Procedural scaling |

### Between waves

A 3-second **Wave Clear** screen shows:
- A randomly chosen victory image
- Wave bonus points awarded
- Per-wave accuracy and accuracy bonus

---

## Game Over

The game ends when **5 ships have escaped**.  
The final screen shows:

- Final score
- High score (with a "NEW HIGH SCORE" banner if beaten)
- Waves survived
- Total ships sunk
- Overall accuracy (torpedoes hit / torpedoes fired)

Click or press **Space / Enter** to restart.

---

## High Score

The best score is stored in the browser's `localStorage` under the key
`seaWolfHighScore` and persists across sessions.
