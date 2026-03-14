// rendererCore.js — shared canvas state, colour palette, sprite management
window.SeaWolfRendererCore = (() => {
    let canvas = null;
    let ctx = null;
    let waveOffset = 0;
    let boatImage  = null;
    let boatSprite  = null;
    let cargoSprite = null;
    let cruiserSprite = null;
    let fishingSprite = null;
    let tankerSprite  = null;
    let carrierSprite = null;
    let _stars = null;

    // ── Screen images ────────────────────────────────────────────────────────
    let startScreenImage = null;
    const victoryImages  = [];
    let _victoryImageIdx  = 0;
    let _lastVictoryWave  = -1;

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

    // Crops an image to `cropH` rows (or full height if null), then snaps
    // any fringe pixels with very low alpha to fully transparent.
    function _prepareSprite(img, cropH, removeTealBg = false) {
        const h = cropH ?? img.naturalHeight;
        const off = document.createElement('canvas');
        off.width  = img.naturalWidth;
        off.height = h;
        const offCtx = off.getContext('2d');
        offCtx.drawImage(img, 0, 0);
        const data = offCtx.getImageData(0, 0, off.width, h);
        const px   = data.data;
        for (let i = 0; i < px.length; i += 4) {
            if (px[i + 3] < 40) { px[i + 3] = 0; continue; }
            if (removeTealBg) {
                const r = px[i], g = px[i + 1], b = px[i + 2];
                if ((r + g + b) < 160 && b > r && (b - r) >= 8) px[i + 3] = 0;
            }
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
        tickWaveOffset() { waveOffset += 0.5; },
        getStars()       { return _stars; },
        setStars(s)      { _stars = s; },

        getSprite(type) {
            switch (type) {
                case 'cargo':       return cargoSprite;
                case 'cruiser':     return cruiserSprite;
                case 'fishingboat': return fishingSprite;
                case 'tanker':      return tankerSprite;
                case 'carrier':     return carrierSprite;
                default:            return boatSprite;
            }
        },

        getStartScreenImage()  { return startScreenImage; },
        getVictoryImages()     { return victoryImages; },
        getVictoryImageIdx()   { return _victoryImageIdx; },
        setVictoryImageIdx(i)  { _victoryImageIdx = i; },
        getLastVictoryWave()   { return _lastVictoryWave; },
        setLastVictoryWave(w)  { _lastVictoryWave = w; },

        // ── Initialisation ───────────────────────────────────────────────────
        init(canvasId) {
            canvas = document.getElementById(canvasId);
            ctx = canvas.getContext('2d');

            startScreenImage = new Image();
            startScreenImage.src = 'images/Startscreen/MainPage.png';

            ['Victory1', 'Victory2', 'Victory3', 'victory4'].forEach(name => {
                const img = new Image();
                img.src = `images/Victory/${name}.png`;
                victoryImages.push(img);
            });

            boatImage = new Image();
            boatImage.onload = () => { boatSprite = _prepareSprite(boatImage, null, true); };
            boatImage.src = 'images/boat.png';

            const cargoImg = new Image();
            cargoImg.onload = () => { cargoSprite = _prepareSprite(cargoImg, 820); };
            cargoImg.src = 'images/cargo.png';

            const cruiserImg = new Image();
            cruiserImg.onload = () => { cruiserSprite = _prepareSprite(cruiserImg, 800, true); };
            cruiserImg.src = 'images/cruiser.png';

            const fishingImg = new Image();
            fishingImg.onload = () => { fishingSprite = _prepareSprite(fishingImg, 900, true); };
            fishingImg.src = 'images/fishing.png';

            const tankerImg = new Image();
            tankerImg.onload = () => { tankerSprite = _prepareSprite(tankerImg, 750, true); };
            tankerImg.src = 'images/tanker.png';

            const carrierImg = new Image();
            carrierImg.onload = () => { carrierSprite = _prepareSprite(carrierImg, 790, true); };
            carrierImg.src = 'images/carrier.png';
        }
    };
})();
