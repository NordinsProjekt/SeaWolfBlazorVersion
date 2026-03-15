// canvasRenderer.js — orchestrator
// Delegates all rendering to the specialised renderer modules.
// Load order in App.razor must be:
//   rendererCore.js ? rendererBackground.js ? rendererShips.js ?
//   rendererProjectiles.js ? rendererHUD.js ? rendererScreens.js ?
//   canvasRenderer.js
window.SeaWolfRenderer = (() => {
    const core        = window.SeaWolfRendererCore;
    const background  = window.SeaWolfRendererBackground;
    const ships       = window.SeaWolfRendererShips;
    const projectiles = window.SeaWolfRendererProjectiles;
    const hud         = window.SeaWolfRendererHUD;
    const screens     = window.SeaWolfRendererScreens;

    return {
        init(canvasId) {
            core.init(canvasId);
        },

        renderFrame(stateJson) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();
            if (!ctx) return;

            const s = JSON.parse(stateJson);
            core.tickWaveOffset();

            ctx.clearRect(0, 0, canvas.width, canvas.height);
            background.draw(core);

            if (s.status === 'StartScreen') {
                screens.drawStartScreen(core, s);
                hud.drawPeriscopeVignette(core);
                return;
            }

            if (s.status === 'GameOver') {
                screens.drawGameOver(core, s);
                hud.drawPeriscopeVignette(core);
                return;
            }

            if (s.status === 'MissionBriefing') {
                screens.drawMissionBriefing(core, s);
                hud.drawPeriscopeVignette(core);
                return;
            }

            if (s.status === 'MissionComplete') {
                screens.drawMissionComplete(core, s);
                hud.drawPeriscopeVignette(core);
                return;
            }

            if (s.status === 'CampaignComplete') {
                screens.drawCampaignComplete(core, s);
                hud.drawPeriscopeVignette(core);
                return;
            }

            // World — wrapped in screen-shake transform
            ctx.save();
            if ((s.shakeTimer ?? 0) > 0) {
                const mag = s.shakeTimer * 10;
                ctx.translate((Math.random() - 0.5) * mag, (Math.random() - 0.5) * mag);
            }
            [...s.ships].sort((a, b) => a.y - b.y).forEach(ship => ships.drawShip(core, ship));
            s.torpedoes.forEach(t => projectiles.drawTorpedo(core, t));
            s.explosions.forEach(ex => projectiles.drawExplosion(core, ex));
            ctx.restore();

            // Fixed-position UI (no shake)
            if (s.floatingTexts) s.floatingTexts.forEach(ft => projectiles.drawFloatingText(core, ft));
            hud.drawTubeSpread(core, s);
            hud.drawPeriscopeVignette(core);
            hud.drawHUD(core, s);

            if (s.status === 'Playing') screens.drawEscapeWarnings(core, s);
            if (s.status === 'Playing' && s.mode !== 'Campaign') screens.drawWaveStartBanner(core, s);
            if (s.status === 'Playing' && s.mode === 'Campaign') hud.drawCampaignHUD(core, s);

            if (s.status === 'WaveClear' && s.mode !== 'Campaign') {
                const victoryImages = core.getVictoryImages();
                if (s.wave !== core.getLastVictoryWave() && victoryImages.length > 0) {
                    core.setLastVictoryWave(s.wave);
                    let next;
                    do { next = Math.floor(Math.random() * victoryImages.length); }
                    while (victoryImages.length > 1 && next === core.getVictoryImageIdx());
                    core.setVictoryImageIdx(next);
                }
                screens.drawWaveClear(core, s.wave, s.lastWaveBonus, s.waveClearTimer, s.lastAccuracy, s.accuracyBonus);
            }

            if (s.status === 'Paused') screens.drawPaused(core);
        }
    };
})();
