// canvasRenderer.js - orchestrator
// Delegates all rendering to the specialised renderer modules.
// Load order in App.razor must be:
//   rendererCore.js -> rendererBackground.js -> rendererShips.js ->
//   rendererProjectiles.js -> rendererHUD.js -> rendererScreens.js ->
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

            // Tell the JS game loop the current status so it can throttle
            // back to an idle rate for anything that isn't active gameplay
            // (menus, wave-clear, paused, timers) — see gameLoop.js.
            if (window.SeaWolfGameLoop) window.SeaWolfGameLoop.reportStatus(s.status);

            // Track status changes for the cross-fade overlay drawn at the
            // very end of this function (see the finally block below) —
            // wrapping the whole body in try/finally means every early
            // return (StartScreen, GameOver, briefings, etc.) still gets
            // the fade treatment, not just the main gameplay path.
            core.noteStatusForFade(s.status);

            try {
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

                // World - wrapped in screen-shake transform
                ctx.save();
                if ((s.shakeTimer ?? 0) > 0) {
                    const mag = s.shakeTimer * 10;
                    ctx.translate((Math.random() - 0.5) * mag, (Math.random() - 0.5) * mag);
                }
                projectiles.updateAndDrawBubbles(core);
                [...s.ships].sort((a, b) => a.y - b.y).forEach(ship => ships.drawShip(core, ship));
                s.torpedoes.forEach(t => projectiles.drawTorpedo(core, t));
                s.explosions.forEach(ex => projectiles.drawExplosion(core, ex));
                ships.updateAndDrawSplashes(core);
                ctx.restore();

                // Drop trailing wake/smoke/transition state for any ship
                // that's no longer in play (sunk-and-removed or escaped).
                ships.cleanupStaleShipEffects(new Set(s.ships.map(sh => sh.id)));

                // Fixed-position UI (no shake)
                if (s.floatingTexts) s.floatingTexts.forEach(ft => projectiles.drawFloatingText(core, ft));
                hud.drawTubeSpread(core, s);
                hud.drawPeriscopeVignette(core);

                // Screen-space punch on a killing blow — drawn after the
                // vignette so it isn't dimmed by the vignette's darkening,
                // but still under the HUD so readouts stay crisp.
                const flashAlpha = core.getImpactFlashAlpha();
                if (flashAlpha > 0) {
                    ctx.save();
                    ctx.globalAlpha = flashAlpha * 0.5;
                    ctx.fillStyle = '#fff6d8';
                    ctx.fillRect(0, 0, canvas.width, canvas.height);
                    ctx.restore();
                }

                hud.drawHUD(core, s);

                if (s.status === 'Playing') screens.drawEscapeWarnings(core, s);
                if (s.status === 'Playing' && s.mode !== 'Campaign') screens.drawWaveStartBanner(core, s);
                if (s.status === 'Playing' && s.mode === 'Campaign') hud.drawCampaignHUD(core, s);

                if (s.status === 'WaveClear' && s.mode !== 'Campaign') {
                    // Only start fetching the victory splash images the first
                    // time they're actually needed (see rendererCore.js).
                    core.ensureVictoryImagesLoaded();
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
            } finally {
                const fadeAlpha = core.getStatusFadeAlpha();
                if (fadeAlpha > 0) {
                    ctx.save();
                    ctx.globalAlpha = fadeAlpha;
                    ctx.fillStyle = '#000';
                    ctx.fillRect(0, 0, canvas.width, canvas.height);
                    ctx.restore();
                }
            }
        }
    };
})();
