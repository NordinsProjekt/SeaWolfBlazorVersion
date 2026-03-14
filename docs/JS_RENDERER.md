# JavaScript Rendering Pipeline

## Overview

All rendering is done on a single `1280 × 720` HTML `<canvas>` element using
the Canvas 2D API. The pipeline is split into seven modules, each with a
single responsibility, plus an orchestrator that drives them every frame.

---

## Module Map

```
canvasRenderer.js  (SeaWolfRenderer — orchestrator)
    │
    ├── rendererCore.js         (SeaWolfRendererCore)
    ├── rendererBackground.js   (SeaWolfRendererBackground)
    ├── rendererShips.js        (SeaWolfRendererShips)
    ├── rendererProjectiles.js  (SeaWolfRendererProjectiles)
    ├── rendererHUD.js          (SeaWolfRendererHUD)
    └── rendererScreens.js      (SeaWolfRendererScreens)

inputBridge.js      (SeaWolfInput)
audio.js            (SeaWolfAudio)
```

---

## Module Reference

### `rendererCore.js` — `window.SeaWolfRendererCore`

Owns the shared canvas/context reference and all loaded assets.

**Initialised by:** `SeaWolfRenderer.init(canvasId)` → `core.init(canvasId)`

| Responsibility | Detail |
|---|---|
| Canvas & context | Retrieves `<canvas>` by ID, stores `ctx` |
| Sprite loading | Loads and pre-processes PNG sprites via `_prepareSprite()` |
| Sprite types | `boat`, `cargo`, `cruiser`, `fishing`, `tanker`, `carrier` |
| Screen images | Start screen (`MainPage.png`), 4 victory images |
| Colour palette | `COLORS` object shared with all modules |
| Wave offset | Incremented each frame to animate ocean waves |
| Star field | Generated once, cached across frames |

**`_prepareSprite(img, cropH, removeTealBg)`**  
Draws the image onto an off-screen canvas, clips to `cropH` rows, and
optionally removes teal/near-black background pixels with alpha < 40.

---

### `rendererBackground.js` — `window.SeaWolfRendererBackground`

Draws the static and animated scene backdrop every frame.

**Draw order (back to front):**

1. Sky-to-ocean linear gradient
2. Moon with radial glow
3. Animated starfield (twinkling via `sin(time + phase)`)
4. Dark water fill below the horizon
5. 14 rows of animated sine-wave ocean surface lines
6. Horizon shimmer line
7. Two dashed depth-zone reference bands (at y = 355 and y = 475)

**Horizon position:** `276 × (canvasHeight / 600)` — scales with canvas height.

---

### `rendererShips.js` — `window.SeaWolfRendererShips`

Draws each ship, its damage state, far-lane depth effect, bow wave, and fire.

**`drawShip(core, ship)`**

1. Compute `sinkProgress` (0 → 1 over 1.5 s).
2. `ctx.save()` → translate to `(ship.x, ship.y)`, flip horizontally for
   `direction === -1`.
3. If sinking: rotate by `sinkProgress × 0.4`, fade alpha.
4. If far-lane (`depthScale < 1`): dim alpha to 0.82.
5. **Sprite path** — if the sprite image is loaded, draw it scaled to
   `ship.width` wide; apply a colour tint overlay via `source-atop` compositing
   and a blue depth-haze overlay for far-lane ships.
6. **Vector fallback** — if the sprite is not yet loaded, draw a hand-coded
   canvas shape (`_drawDestroyer`, `_drawPtBoat`, `_drawCargo`).
7. After `ctx.restore()`: draw bow wake ellipse and V-wake lines in world space.
8. If `Burning`: call `_drawFireParticles(ctx, ship.fireParticles)`.

**Fire particles**  
Each particle is a radial gradient from white-yellow (centre) to transparent
red (edge). Alpha is driven by remaining `life`.

---

### `rendererProjectiles.js` — `window.SeaWolfRendererProjectiles`

**`drawTorpedo(core, t)`**  
Rotates to the torpedo's velocity direction, draws a yellow-green triangle,
then renders 8 wake bubbles along the tail with decreasing alpha.

**`drawExplosion(core, ex)`**  
Three concentric filled circles (orange → amber → white-yellow) sized to
`ex.radius`. Large explosions (`maxRadius ≥ 40`) also draw an expanding shock-
wave ring with glow. Spark squares are drawn at each `spark.x / spark.y`.

**`drawFloatingText(core, ft)`**  
Draws the score pop-up (`+150 ×3`, `+TORP`, `ESCAPED!`, etc.) centred at
`(ft.x, ft.y)` with alpha fading from `life / maxLife`.

---

### `rendererHUD.js` — `window.SeaWolfRendererHUD`

**`drawHUD(core, s)`**

| Element | Position | Detail |
|---|---|---|
| Score | Top-left | `SCORE: N` |
| Best score | Below score | `BEST: N` (hidden if 0) |
| Wave | Top-centre | `WAVE N` |
| Ships remaining | Below wave | Count of un-spawned + active ships |
| Combo badge | Top-centre | `×2/3/4 COMBO` — pulses via `sin(Date.now())` |
| Torpedo icons | Top-right | 2 triangle icons; filled = loaded, outlined = empty |
| Reload bar | Bottom-centre | Yellow progress bar during reload |
| Escaped ships | Bottom-left | 5 small ship silhouettes; active = red |

**`drawTubeSpread(core, s)`**

Draws 5 tube lines from launch origin `(640, 680)`.  
Active tube: solid bright-green line.  
Inactive tubes: dashed, dim.  
A crosshair (`+` with circle) sits at the active tube's intersection with
`y = 420` (mid-lane targeting reference).

**`drawPeriscopeVignette(core)`**

Adds:
1. Radial gradient darkening toward the screen edges.
2. A faint green circle marking the periscope aperture.
3. Subtle CRT scan-line effect (1 px dark stripe every 3 px).

---

### `rendererScreens.js` — `window.SeaWolfRendererScreens`

| Method | Shown when |
|---|---|
| `drawStartScreen` | `status === 'StartScreen'` |
| `drawGameOver` | `status === 'GameOver'` |
| `drawWaveClear` | `status === 'WaveClear'` |
| `drawWaveStartBanner` | `waveStartTimer < 2.2` during `Playing` |
| `drawEscapeWarnings` | Ships near edges during `Playing` |
| `drawPaused` | `status === 'Paused'` |

**Start screen** — renders `MainPage.png` (cover-fit, darkened), then overlays
title, instructions, and a blinking "CLICK TO DIVE IN" prompt.

**Wave clear** — renders a randomly selected victory image (aspect-fit), then
wave bonus and accuracy stats below it.

**Escape warnings** — blinking red arrows on the left/right edges when a ship
is within 200 px of escaping.

---

### `canvasRenderer.js` — `window.SeaWolfRenderer`

The **orchestrator** called directly by Blazor.

```javascript
SeaWolfRenderer.init(canvasId)
SeaWolfRenderer.renderFrame(stateJson)
```

**`renderFrame` draw order:**

```
clearRect()
background.draw()
  │
  ├─ StartScreen  → screens.drawStartScreen()
  │                 hud.drawPeriscopeVignette()
  │                 return
  │
  ├─ GameOver     → screens.drawGameOver()
  │                 hud.drawPeriscopeVignette()
  │                 return
  │
  └─ Playing / WaveClear / Paused:
        ctx.save() + optional screen-shake translate
        ships (sorted by y — painters algorithm)
        torpedoes
        explosions
        ctx.restore()
        floatingTexts
        hud.drawTubeSpread()
        hud.drawPeriscopeVignette()
        hud.drawHUD()
        screens.drawEscapeWarnings()   (Playing only)
        screens.drawWaveStartBanner()  (Playing only)
        screens.drawWaveClear()        (WaveClear only)
        screens.drawPaused()           (Paused only)
```

Ship sorting by `y` ensures far-lane (horizon) ships are drawn beneath
near-lane ships, creating a natural depth illusion.

---

### `inputBridge.js` — `window.SeaWolfInput`

Attaches DOM event listeners on `init(dotNetRef, canvasId)` and forwards
events to Blazor via `dotNetRef.invokeMethodAsync`.

| Event | .NET method | Data passed |
|---|---|---|
| `mousemove` on canvas | `OnMouseMove` | `clientX − rect.left`, `clientY − rect.top` |
| `click` on canvas | `OnClick` | Same coordinates |
| `keydown` on document | `OnKeyDown` | `e.key` |

Arrow keys and Space are `preventDefault`-ed to stop page scrolling.

---

### `audio.js` — `window.SeaWolfAudio`

All sound is synthesised using the **Web Audio API** — no audio files required.
An `AudioContext` is created lazily on first use.

| Method | Sound | Detail |
|---|---|---|
| `playTorpedoLaunch()` | Descending whine | Sine wave 880 → 280 Hz over 0.35 s |
| `playTone(freq, dur, type, gain)` | General tone | Used for explosions, burn, reload |
| `playChord()` | Victory arpeggio | C5–E5–G5 sine tones, 120 ms apart |
| `playGameOver()` | Descending fanfare | 6-note sawtooth descent, 160 ms apart |

AudioContext creation is deferred to avoid browser autoplay restrictions —
sound only starts after the first user interaction.

---

## Asset Paths (relative to `wwwroot`)

| Asset | Path |
|---|---|
| Start screen | `images/Startscreen/MainPage.png` |
| Victory images | `images/Victory/Victory1.png` … `victory4.png` |
| Boat sprite | `images/boat.png` |
| Cargo sprite | `images/cargo.png` |
| Cruiser sprite | `images/cruiser.png` |
| Fishing boat sprite | `images/fishing.png` |
| Tanker sprite | `images/tanker.png` |
| Carrier sprite | `images/carrier.png` |
