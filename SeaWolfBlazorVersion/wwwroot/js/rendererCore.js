// rendererCore.js — shared canvas state, colour palette, sprite management
window.SeaWolfRendererCore = (() => {
    let canvas = null;
    let ctx = null;
    let waveOffset = 0;
    let _lastWaveTickTime = null;
    let destroyerImage = null;
    let destroyerSprite = null;
    let ptBoatImage = null;
    let ptBoatSprite = null;
    let cargoSprite = null;
    let cruiserSprite = null;
    let fishingSprite = null;
    let tankerSprite  = null;
    let carrierSprite = null;
    let _stars = null;

    // ── Screen images ────────────────────────────────────────────────────────
    let startScreenImage = null;
    const victoryImages  = [];
    let _victoryImagesRequested = false;
    let _victoryImageIdx  = 0;
    let _lastVictoryWave  = -1;

    // ── One-shot / transition effect timers (all wall-clock based so they're
    // unaffected by the idle render throttle — see gameLoop.js) ─────────────
    const IMPACT_FLASH_MS = 220;
    let _impactFlashStart = null; // performance.now() at trigger, or null

    const STATUS_FADE_MS = 260;
    let _lastFadeStatus = null;   // last status we've already faded for
    let _fadeStart = null;        // performance.now() when the fade began

    // ── Colour palette ───────────────────────────────────────────────────────
    const COLORS = {
        destroyer:   { hull: '#3a4a5c', super: '#2e3d4e', accent: '#c0c0c0' },
        ptBoat:      { hull: '#2d4a2d', super: '#1e3320', accent: '#8b7355' },
        cargo:       { hull: '#6b3a2a', super: '#4a2a1e', accent: '#d4a853' },
        torpedo:     '#c8ff00',
        ocean:       ['#001428', '#002244', '#003366'],
        crosshair:   '#00FF00',
        hud:         '#00FF00',
        hudDim:      '#007700'
    };

    // Pixel-art sprites ship with clean, hard-edged alpha already baked in.
    // This just snaps any faint anti-aliasing fringe fully transparent so a
    // sprite never shows a soft halo over the ocean background.
    function _prepareSprite(img) {
        const off = document.createElement('canvas');
        off.width  = img.naturalWidth;
        off.height = img.naturalHeight;
        const offCtx = off.getContext('2d');
        offCtx.drawImage(img, 0, 0);
        const data = offCtx.getImageData(0, 0, off.width, off.height);
        const px   = data.data;
        for (let i = 0; i < px.length; i += 4) {
            if (px[i + 3] < 40) px[i + 3] = 0;
        }
        offCtx.putImageData(data, 0, 0);
        return off;
    }

    return {
        // ── Accessors used by other modules ──────────────────────────────────
        getCanvas()      { return canvas; },
        getCtx()         { return ctx; },
        getColors()      { return COLORS; },
        getWaveOffset()  { return waveOffset; },

        // Advances the ocean-wave animation by real elapsed time rather than
        // a fixed amount per call. renderFrame (and therefore this) can now
        // run at a throttled rate on idle/menu screens (see gameLoop.js) —
        // scaling by elapsed ms keeps the waves scrolling at the same speed
        // either way instead of visibly slowing down while idle.
        tickWaveOffset() {
            const now = performance.now();
            if (_lastWaveTickTime === null) _lastWaveTickTime = now;
            const elapsed = Math.min(now - _lastWaveTickTime, 100);
            _lastWaveTickTime = now;
            // Baseline: 0.5 per ~16.67ms, matching the old fixed per-call amount at 60fps.
            waveOffset += (elapsed / 16.6667) * 0.5;
        },

        getStars()       { return _stars; },
        setStars(s)      { _stars = s; },

        getSprite(type) {
            switch (type) {
                case 'destroyer':   return destroyerSprite;
                case 'ptboat':      return ptBoatSprite;
                case 'cargo':       return cargoSprite;
                case 'cruiser':     return cruiserSprite;
                case 'fishingboat': return fishingSprite;
                case 'tanker':      return tankerSprite;
                case 'carrier':     return carrierSprite;
                default:            return destroyerSprite;
            }
        },

        getStartScreenImage()  { return startScreenImage; },
        getVictoryImages()     { return victoryImages; },
        getVictoryImageIdx()   { return _victoryImageIdx; },
        setVictoryImageIdx(i)  { _victoryImageIdx = i; },
        getLastVictoryWave()   { return _lastVictoryWave; },
        setLastVictoryWave(w)  { _lastVictoryWave = w; },

        // Victory splash images only ever appear after a whole wave is
        // cleared, so fetching them at page load just competes for
        // network/CPU with boot-critical resources for no benefit. Loaded
        // once, lazily, the first time a WaveClear screen actually needs them.
        ensureVictoryImagesLoaded() {
            if (_victoryImagesRequested) return;
            _victoryImagesRequested = true;
            ['Victory1', 'Victory2', 'Victory3', 'victory4'].forEach(name => {
                const img = new Image();
                img.src = `images/Victory/${name}.png`;
                victoryImages.push(img);
            });
        },

        // ── One-shot impact flash (kill shots) ───────────────────────────────
        // Called from rendererShips.js the instant a ship transitions to
        // Sinking. Purely cosmetic screen-space punch, decays on its own —
        // nothing else needs to reset or clear it.
        triggerImpactFlash() {
            _impactFlashStart = performance.now();
        },
        getImpactFlashAlpha() {
            if (_impactFlashStart === null) return 0;
            const elapsed = performance.now() - _impactFlashStart;
            if (elapsed >= IMPACT_FLASH_MS) { _impactFlashStart = null; return 0; }
            // Quick punch-in, fast fade — not a linear ramp.
            const t = elapsed / IMPACT_FLASH_MS;
            return (1 - t) * (1 - t);
        },

        // ── Cross-fade-through-black on any status change ────────────────────
        // Call once per frame with the current status; drawing the returned
        // alpha as a full-canvas black overlay (last, on top of everything)
        // gives every screen transition — menu, briefing, wave, game over —
        // a brief fade instead of an instant hard cut.
        noteStatusForFade(status) {
            if (_lastFadeStatus === null) {
                // First frame ever — nothing to fade from, just record it.
                _lastFadeStatus = status;
                return;
            }
            if (status !== _lastFadeStatus) {
                _lastFadeStatus = status;
                _fadeStart = performance.now();
            }
        },
        getStatusFadeAlpha() {
            if (_fadeStart === null) return 0;
            const elapsed = performance.now() - _fadeStart;
            if (elapsed >= STATUS_FADE_MS) { _fadeStart = null; return 0; }
            return 1 - (elapsed / STATUS_FADE_MS);
        },

        // ── Initialisation ───────────────────────────────────────────────────
        init(canvasId) {
            canvas = document.getElementById(canvasId);
            ctx = canvas.getContext('2d');

            startScreenImage = new Image();
            startScreenImage.src = 'images/Startscreen/MainPage.png';

            destroyerImage = new Image();
            destroyerImage.onload = () => { destroyerSprite = _prepareSprite(destroyerImage); };
            destroyerImage.src = 'images/boat.png';

            ptBoatImage = new Image();
            ptBoatImage.onload = () => { ptBoatSprite = _prepareSprite(ptBoatImage); };
            ptBoatImage.src = 'images/ptboat.png';

            const cargoImg = new Image();
            cargoImg.onload = () => { cargoSprite = _prepareSprite(cargoImg); };
            cargoImg.src = 'images/cargo.png';

            const cruiserImg = new Image();
            cruiserImg.onload = () => { cruiserSprite = _prepareSprite(cruiserImg); };
            cruiserImg.src = 'images/cruiser.png';

            const fishingImg = new Image();
            fishingImg.onload = () => { fishingSprite = _prepareSprite(fishingImg); };
            fishingImg.src = 'images/fishing.png';

            const tankerImg = new Image();
            tankerImg.onload = () => { tankerSprite = _prepareSprite(tankerImg); };
            tankerImg.src = 'images/tanker.png';

            const carrierImg = new Image();
            carrierImg.onload = () => { carrierSprite = _prepareSprite(carrierImg); };
            carrierImg.src = 'images/carrier.png';
        }
    };
})();
