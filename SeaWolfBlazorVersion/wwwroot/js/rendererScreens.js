// rendererScreens.js — start screen, game-over, wave-clear, wave-start banner, escape warnings, pause
window.SeaWolfRendererScreens = (() => {
    return {
        drawStartScreen(core) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();
            const cx = canvas.width / 2, cy = canvas.height / 2;

            ctx.save();

            const startScreenImage = core.getStartScreenImage();
            if (startScreenImage && startScreenImage.complete && startScreenImage.naturalWidth > 0) {
                const imgAr = startScreenImage.naturalWidth / startScreenImage.naturalHeight;
                const canAr = canvas.width / canvas.height;
                let sw, sh, sx, sy;
                if (imgAr > canAr) {
                    sh = canvas.height; sw = sh * imgAr;
                    sx = (canvas.width - sw) / 2; sy = 0;
                } else {
                    sw = canvas.width; sh = sw / imgAr;
                    sx = 0; sy = (canvas.height - sh) / 2;
                }
                ctx.drawImage(startScreenImage, sx, sy, sw, sh);
                ctx.fillStyle = 'rgba(0,0,0,0.52)';
                ctx.fillRect(0, 0, canvas.width, canvas.height);
            }

            ctx.textAlign  = 'center';
            ctx.shadowColor = '#00FF00';
            ctx.shadowBlur = 20;
            ctx.fillStyle  = '#00FF00';
            ctx.font = 'bold 56px "Courier New", monospace';
            ctx.fillText('SEA WOLF', cx, cy - 60);

            ctx.shadowBlur = 6;
            ctx.font = 'bold 16px "Courier New", monospace';
            ctx.fillStyle = '#aaffaa';
            ctx.fillText('PERISCOPE ATTACK SIMULATION', cx, cy - 25);

            ctx.shadowBlur = 0;
            ctx.fillStyle  = '#888';
            ctx.font = '13px "Courier New", monospace';
            ctx.fillText('Move mouse to aim along a tube line  |  CLICK or SPACE to fire', cx, cy + 20);
            ctx.fillText('← → Arrow keys pan aim  |  P to pause', cx, cy + 40);
            ctx.fillText('Chain kills to build a COMBO multiplier', cx, cy + 60);

            if (Math.floor(Date.now() / 600) % 2 === 0) {
                ctx.fillStyle  = '#00FF00';
                ctx.font = 'bold 18px "Courier New", monospace';
                ctx.shadowColor = '#00FF00';
                ctx.shadowBlur = 10;
                ctx.fillText('[ CLICK TO DIVE IN ]', cx, cy + 100);
            }

            ctx.restore();
        },

        drawGameOver(core, s) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();

            const score     = s.score, highScore = s.highScore;
            const isNewHigh = score >= highScore && score > 0;
            const cx = canvas.width / 2, cy = canvas.height / 2;

            ctx.save();
            ctx.textAlign = 'center';

            ctx.fillStyle   = '#FF2200';
            ctx.shadowColor = '#FF2200';
            ctx.shadowBlur  = 24;
            ctx.font = 'bold 52px "Courier New", monospace';
            ctx.fillText('GAME OVER', cx, cy - 80);

            ctx.shadowBlur = 6;
            ctx.fillStyle  = '#00FF00';
            ctx.font = 'bold 22px "Courier New", monospace';
            ctx.fillText(`FINAL SCORE: ${score}`, cx, cy - 32);

            if (isNewHigh) {
                ctx.fillStyle   = '#FFD700';
                ctx.font = 'bold 17px "Courier New", monospace';
                ctx.shadowColor = '#FFD700';
                ctx.shadowBlur  = 12;
                ctx.fillText('★  NEW HIGH SCORE!  ★', cx, cy - 4);
            } else if (highScore > 0) {
                ctx.fillStyle  = '#888';
                ctx.font = '13px "Courier New", monospace';
                ctx.shadowBlur = 0;
                ctx.fillText(`Best: ${highScore}`, cx, cy - 4);
            }

            const accuracy = (s.totalTorpedosFired ?? 0) > 0
                ? Math.round(100 * (s.totalTorpedosHit ?? 0) / s.totalTorpedosFired)
                : 0;
            const statY = cy + 30;
            ctx.shadowBlur = 0;
            ctx.font = '13px "Courier New", monospace';
            ctx.fillStyle = '#aaffaa';
            ctx.fillText(`WAVES SURVIVED: ${s.wave ?? 1}`, cx, statY);
            ctx.fillText(`SHIPS SUNK: ${s.totalShipsSunk ?? 0}`, cx, statY + 20);
            ctx.fillText(`ACCURACY: ${accuracy}%  (${s.totalTorpedosHit ?? 0} / ${s.totalTorpedosFired ?? 0})`, cx, statY + 40);

            if (Math.floor(Date.now() / 600) % 2 === 0) {
                ctx.fillStyle   = '#00FF00';
                ctx.font = 'bold 16px "Courier New", monospace';
                ctx.shadowColor = '#00FF00';
                ctx.shadowBlur  = 8;
                ctx.fillText('[ CLICK TO PLAY AGAIN ]', cx, statY + 76);
            }

            ctx.restore();
        },

        drawWaveClear(core, wave, bonus, timer, accuracy, accuracyBonus) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();

            const progress = Math.min(timer / 3.0, 1);
            const alpha    = progress < 0.7 ? 1 : 1 - (progress - 0.7) / 0.3;
            const cx = canvas.width / 2, cy = canvas.height / 2;

            ctx.save();
            ctx.globalAlpha = alpha;

            const victoryImages  = core.getVictoryImages();
            const victoryIdx     = core.getVictoryImageIdx();
            const vImg = victoryImages[victoryIdx];
            if (vImg && vImg.complete && vImg.naturalWidth > 0) {
                const maxH  = canvas.height * 0.55;
                const maxW  = canvas.width  * 0.75;
                const scale = Math.min(maxW / vImg.naturalWidth, maxH / vImg.naturalHeight);
                const dw = vImg.naturalWidth  * scale;
                const dh = vImg.naturalHeight * scale;
                const dx = cx - dw / 2;
                const dy = cy - dh / 2 - 20;

                ctx.fillStyle = 'rgba(0,0,0,0.55)';
                ctx.fillRect(dx - 8, dy - 8, dw + 16, dh + 16);
                ctx.drawImage(vImg, dx, dy, dw, dh);

                ctx.strokeStyle = 'rgba(0,255,0,0.55)';
                ctx.lineWidth = 2;
                ctx.strokeRect(dx - 2, dy - 2, dw + 4, dh + 4);
            }

            const textY = cy + (canvas.height * 0.34);
            ctx.textAlign  = 'center';
            ctx.shadowColor = '#00FF00';
            ctx.shadowBlur = 18;
            ctx.fillStyle  = '#00FF00';
            ctx.font = 'bold 36px "Courier New", monospace';
            ctx.fillText(`WAVE ${wave} CLEAR!`, cx, textY);
            ctx.font = 'bold 20px "Courier New", monospace';
            ctx.fillStyle   = '#FFD700';
            ctx.shadowColor = '#FFD700';
            ctx.fillText(`+${bonus} pts`, cx, textY + 38);
            if (accuracy != null && accuracy > 0) {
                const aBonusStr = accuracyBonus > 0 ? `  +${accuracyBonus} ACCURACY BONUS` : '';
                const aColor    = accuracy >= 80 ? '#00FF88' : accuracy >= 60 ? '#FFD700' : '#888';
                ctx.font = '14px "Courier New", monospace';
                ctx.fillStyle   = aColor;
                ctx.shadowColor = aColor;
                ctx.shadowBlur  = 6;
                ctx.fillText(`ACCURACY ${accuracy}%${aBonusStr}`, cx, textY + 64);
            }
            ctx.restore();
        },

        drawWaveStartBanner(core, s) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();

            const t = s.waveStartTimer ?? 0;
            if (t >= 2.2) return;
            const alpha = t < 0.35 ? t / 0.35 : t < 1.75 ? 1 : 1 - (t - 1.75) / 0.45;
            if (alpha <= 0) return;

            const cx = canvas.width / 2, cy = canvas.height * 0.42;
            ctx.save();
            ctx.globalAlpha = alpha;
            ctx.textAlign   = 'center';
            ctx.font        = 'bold 64px "Courier New", monospace';
            ctx.fillStyle   = '#00FF00';
            ctx.shadowColor = '#00FF00';
            ctx.shadowBlur  = 32;
            ctx.fillText(`WAVE ${s.wave}`, cx, cy);
            ctx.font        = 'bold 22px "Courier New", monospace';
            ctx.fillStyle   = '#FFD700';
            ctx.shadowColor = '#FFD700';
            ctx.shadowBlur  = 14;
            ctx.fillText('— INCOMING! —', cx, cy + 44);
            ctx.restore();
        },

        drawEscapeWarnings(core, s) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();

            if (!s.ships || s.ships.length === 0) return;
            if (Math.floor(Date.now() / 350) % 2 !== 0) return;

            let warnLeft = false, warnRight = false;
            s.ships.forEach(ship => {
                if (ship.damageState === 'Sinking') return;
                if (ship.direction ===  1 && ship.x > canvas.width - 200) warnRight = true;
                if (ship.direction === -1 && ship.x < 200)                warnLeft  = true;
            });
            if (!warnLeft && !warnRight) return;

            ctx.save();
            ctx.fillStyle   = '#FF2200';
            ctx.shadowColor = '#FF2200';
            ctx.shadowBlur  = 14;

            if (warnRight) {
                const ax = canvas.width - 28, ay = canvas.height / 2;
                ctx.beginPath();
                ctx.moveTo(ax - 14, ay - 18);
                ctx.lineTo(ax + 14, ay);
                ctx.lineTo(ax - 14, ay + 18);
                ctx.closePath();
                ctx.fill();
                ctx.font      = 'bold 10px "Courier New", monospace';
                ctx.textAlign = 'right';
                ctx.fillText('ESCAPE!', canvas.width - 8, ay - 24);
            }
            if (warnLeft) {
                const ax = 28, ay = canvas.height / 2;
                ctx.beginPath();
                ctx.moveTo(ax + 14, ay - 18);
                ctx.lineTo(ax - 14, ay);
                ctx.lineTo(ax + 14, ay + 18);
                ctx.closePath();
                ctx.fill();
                ctx.font      = 'bold 10px "Courier New", monospace';
                ctx.textAlign = 'left';
                ctx.fillText('ESCAPE!', 8, ay - 24);
            }
            ctx.restore();
        },

        drawPaused(core) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();

            const cx = canvas.width / 2, cy = canvas.height / 2;
            ctx.save();
            ctx.fillStyle = 'rgba(0,0,0,0.5)';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.textAlign   = 'center';
            ctx.fillStyle   = '#00FF00';
            ctx.shadowColor = '#00FF00';
            ctx.shadowBlur  = 16;
            ctx.font = 'bold 42px "Courier New", monospace';
            ctx.fillText('PAUSED', cx, cy);
            ctx.shadowBlur = 0;
            ctx.font = '14px "Courier New", monospace';
            ctx.fillStyle = '#aaa';
            ctx.fillText('Press P to resume', cx, cy + 36);
            ctx.restore();
        }
    };
})();
