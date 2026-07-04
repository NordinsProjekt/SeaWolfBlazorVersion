// rendererCore.js — shared canvas state, colour palette, sprite management
window.SeaWolfRendererCore = (() => {
    let canvas = null;
    let ctx = null;
    let waveOffset = 0;
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
        tickWaveOffset() { waveOffset += 0.5; },
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
