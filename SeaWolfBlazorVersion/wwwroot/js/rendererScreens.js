// rendererScreens.js — start screen, game-over, wave-clear, wave-start banner, escape warnings, pause
window.SeaWolfRendererScreens = (() => {
    let _showHighScores = false;

    // Hit-boxes for the three start-screen buttons (canvas coordinates).
    // Updated every frame so the host can do precise hit-testing.
    const _btns = { arcade: null, campaign: null, highScores: null };

    function _drawBtn(ctx, box, label, color, active) {
        ctx.save();
        ctx.shadowColor = color;
        ctx.shadowBlur  = active ? 18 : 8;
        ctx.strokeStyle = color;
        ctx.lineWidth   = active ? 2.5 : 1.5;
        ctx.strokeRect(box.x, box.y, box.w, box.h);
        ctx.fillStyle = active ? `${color}22` : `${color}0d`;
        ctx.fillRect(box.x, box.y, box.w, box.h);
        ctx.fillStyle   = color;
        ctx.font = 'bold 17px "Courier New", monospace';
        ctx.textAlign   = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(label, box.x + box.w / 2, box.y + box.h / 2);
        ctx.restore();
    }

    return {
        toggleHighScores() { _showHighScores = !_showHighScores; },
        getStartScreenButtons() { return _btns; },

        drawStartScreen(core, s) {
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
                ctx.fillStyle = 'rgba(0,0,0,0.55)';
                ctx.fillRect(0, 0, canvas.width, canvas.height);
            }

            // Title
            ctx.textAlign    = 'center';
            ctx.textBaseline = 'alphabetic';
            ctx.shadowColor  = '#00FF00';
            ctx.shadowBlur   = 20;
            ctx.fillStyle    = '#00FF00';
            ctx.font = 'bold 56px "Courier New", monospace';
            ctx.fillText('SEA WOLF', cx, cy - 80);

            ctx.shadowBlur = 6;
            ctx.font = 'bold 16px "Courier New", monospace';
            ctx.fillStyle = '#aaffaa';
            ctx.fillText('PERISCOPE ATTACK SIMULATION', cx, cy - 44);

            if (_showHighScores) {
                _drawHighScoreTable(ctx, canvas, s);

                // Update hit-boxes — only high-scores back-button active
                const bw = 240, bh = 40;
                _btns.arcade     = null;
                _btns.campaign   = null;
                _btns.highScores = { x: cx - bw / 2, y: canvas.height - 70, w: bw, h: bh };
                _drawBtn(ctx, _btns.highScores, '← BACK TO MENU', '#00FF00', false);
            } else {
                ctx.shadowBlur = 0;
                ctx.fillStyle  = '#777';
                ctx.font = '13px "Courier New", monospace';
                ctx.textBaseline = 'alphabetic';
                ctx.fillText('Move mouse to aim  |  CLICK or SPACE to fire  |  P to pause', cx, cy + 10);
                ctx.fillText('Chain kills to build a COMBO multiplier', cx, cy + 30);

                // Three buttons
                const bw = 230, bh = 46, gap = 22;
                const totalW = bw * 3 + gap * 2;
                const bx0 = cx - totalW / 2;
                const by  = cy + 62;

                _btns.arcade     = { x: bx0,               y: by, w: bw, h: bh };
                _btns.campaign   = { x: bx0 + bw + gap,    y: by, w: bw, h: bh };
                _btns.highScores = { x: bx0 + (bw + gap)*2, y: by, w: bw, h: bh };

                const blink = Math.floor(Date.now() / 550) % 2 === 0;
                _drawBtn(ctx, _btns.arcade,     '▶  ARCADE',      '#00FF00', blink);
                _drawBtn(ctx, _btns.campaign,   '⚑  CAMPAIGN',    '#00CCFF', blink);
                _drawBtn(ctx, _btns.highScores, '★  HIGH SCORES', '#FFD700', false);
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
        },

        drawMissionBriefing(core, s) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();
            const cx = canvas.width / 2;

            const missionIdx = (s.campaignMission ?? 1) - 1;
            const mission    = (s.campaignMissions ?? [])[missionIdx] ?? {};

            ctx.save();
            ctx.fillStyle = 'rgba(0,0,0,0.82)';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.textAlign = 'center';

            // Mission number badge
            ctx.fillStyle   = '#00CCFF';
            ctx.shadowColor = '#00CCFF';
            ctx.shadowBlur  = 10;
            ctx.font = 'bold 14px "Courier New", monospace';
            ctx.fillText(`\u2500\u2500 MISSION ${s.campaignMission ?? 1} \u2500\u2500`, cx, 82);

            // Code name
            ctx.shadowBlur = 26;
            ctx.font = 'bold 38px "Courier New", monospace';
            ctx.fillText(mission.codeName ?? 'CLASSIFIED', cx, 130);

            // Divider
            ctx.shadowBlur  = 0;
            ctx.strokeStyle = 'rgba(0,204,255,0.3)';
            ctx.lineWidth   = 1;
            ctx.beginPath(); ctx.moveTo(cx - 380, 150); ctx.lineTo(cx + 380, 150); ctx.stroke();

            // Briefing text (newline-aware word-wrap)
            const briefLines = _wrapText(ctx, mission.briefing ?? '', 700, '15px "Courier New", monospace');
            ctx.font      = '15px "Courier New", monospace';
            ctx.fillStyle = '#aaffaa';
            briefLines.forEach((line, i) => ctx.fillText(line, cx, 178 + i * 24));

            // Objectives
            const objY = 178 + briefLines.length * 24 + 22;
            ctx.strokeStyle = 'rgba(0,204,255,0.3)';
            ctx.beginPath(); ctx.moveTo(cx - 380, objY); ctx.lineTo(cx + 380, objY); ctx.stroke();

            ctx.fillStyle   = '#FFD700';
            ctx.shadowColor = '#FFD700';
            ctx.shadowBlur  = 8;
            ctx.font = 'bold 13px "Courier New", monospace';
            ctx.fillText('\u2500\u2500 OBJECTIVES \u2500\u2500', cx, objY + 24);

            ctx.font      = '13px "Courier New", monospace';
            ctx.shadowBlur = 0;
            let objLine = objY + 48;

            const targets = (mission.targetTypes ?? []).length > 0
                ? (mission.targetTypes ?? []).join(', ')
                : 'Any ship type';
            ctx.fillStyle = '#aaffaa';
            ctx.fillText(`Sink ${mission.requiredSinks ?? '?'} \u2014 Target: ${targets}`, cx, objLine);

            if ((mission.torpedoBudget ?? 0) > 0) {
                objLine += 22;
                ctx.fillStyle   = '#FFAA44';
                ctx.shadowColor = '#FFAA44';
                ctx.shadowBlur  = 4;
                ctx.fillText(`\u26a0 Torpedo budget: ${mission.torpedoBudget} shots`, cx, objLine);
            }

            const civilianLimit = mission.maxCivilianSinks ?? -1;
            if (civilianLimit >= 0) {
                objLine += 22;
                if (civilianLimit === 0) {
                    ctx.fillStyle   = '#FF4444';
                    ctx.shadowColor = '#FF4444';
                    ctx.shadowBlur  = 8;
                    ctx.fillText('\u26a0 Zero civilian casualties \u2014 any fishing boat sunk = MISSION FAIL', cx, objLine);
                } else {
                    ctx.fillStyle   = '#FFAA44';
                    ctx.shadowColor = '#FFAA44';
                    ctx.shadowBlur  = 4;
                    ctx.fillText(`\u26a0 Max civilian sinks: ${civilianLimit}`, cx, objLine);
                }
            }

            // Lives
            ctx.shadowBlur = 0;
            ctx.fillStyle  = '#00CCFF';
            ctx.font = '13px "Courier New", monospace';
            ctx.fillText(`Lives: ${s.campaignLives ?? 0}`, cx, canvas.height - 68);

            // Blinking prompt
            if (Math.floor(Date.now() / 600) % 2 === 0) {
                ctx.fillStyle   = '#00FF00';
                ctx.shadowColor = '#00FF00';
                ctx.shadowBlur  = 10;
                ctx.font = 'bold 15px "Courier New", monospace';
                ctx.fillText('[ CLICK OR PRESS SPACE TO BEGIN MISSION ]', cx, canvas.height - 40);
            }

            ctx.restore();
        },

        drawMissionComplete(core, s) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();
            const cx = canvas.width / 2, cy = canvas.height / 2;

            const missionIdx = (s.campaignMission ?? 1) - 1;
            const mission    = (s.campaignMissions ?? [])[missionIdx] ?? {};

            ctx.save();
            ctx.fillStyle = 'rgba(0,0,0,0.80)';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.textAlign = 'center';

            // Header
            ctx.fillStyle   = '#00FF88';
            ctx.shadowColor = '#00FF88';
            ctx.shadowBlur  = 24;
            ctx.font = 'bold 48px "Courier New", monospace';
            ctx.fillText('MISSION COMPLETE', cx, cy - 100);

            // Code name
            ctx.shadowBlur  = 12;
            ctx.font = 'bold 20px "Courier New", monospace';
            ctx.fillStyle   = '#00CCFF';
            ctx.shadowColor = '#00CCFF';
            ctx.fillText(mission.codeName ?? '', cx, cy - 52);

            // Stats
            ctx.shadowBlur = 0;
            ctx.font = '15px "Courier New", monospace';
            ctx.fillStyle = '#aaffaa';
            ctx.fillText(`Ships Sunk: ${s.campaignSinks ?? 0} / ${mission.requiredSinks ?? '?'}`, cx, cy + 12);
            ctx.fillText(`Score: ${s.score ?? 0}`, cx, cy + 38);

            // Lives
            ctx.fillStyle   = '#00CCFF';
            ctx.shadowColor = '#00CCFF';
            ctx.shadowBlur  = 6;
            ctx.fillText(`Lives Remaining: ${s.campaignLives ?? 0}`, cx, cy + 64);

            // Blinking prompt
            if (Math.floor(Date.now() / 600) % 2 === 0) {
                ctx.fillStyle   = '#00FF00';
                ctx.shadowColor = '#00FF00';
                ctx.shadowBlur  = 10;
                ctx.font = 'bold 15px "Courier New", monospace';
                ctx.fillText('[ CLICK OR PRESS SPACE TO CONTINUE ]', cx, cy + 116);
            }

            ctx.restore();
        },

        drawCampaignComplete(core, s) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();
            const cx = canvas.width / 2, cy = canvas.height / 2;

            const score     = s.score ?? 0;
            const highScore = s.highScore ?? 0;
            const isNewHigh = score >= highScore && score > 0;

            ctx.save();
            ctx.fillStyle = 'rgba(0,0,0,0.85)';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.textAlign = 'center';

            // Title
            ctx.fillStyle   = '#FFD700';
            ctx.shadowColor = '#FFD700';
            ctx.shadowBlur  = 28;
            ctx.font = 'bold 52px "Courier New", monospace';
            ctx.fillText('CAMPAIGN COMPLETE', cx, cy - 110);

            // Tagline
            ctx.shadowBlur  = 6;
            ctx.font = 'bold 16px "Courier New", monospace';
            ctx.fillStyle   = '#aaffaa';
            ctx.shadowColor = '#aaffaa';
            ctx.fillText('All missions accomplished. The sea is yours.', cx, cy - 60);

            // Final score
            ctx.fillStyle   = '#00FF00';
            ctx.shadowColor = '#00FF00';
            ctx.shadowBlur  = 14;
            ctx.font = 'bold 26px "Courier New", monospace';
            ctx.fillText(`FINAL SCORE: ${score}`, cx, cy - 12);

            // High score notice
            if (isNewHigh) {
                ctx.fillStyle   = '#FFD700';
                ctx.shadowColor = '#FFD700';
                ctx.shadowBlur  = 14;
                ctx.font = 'bold 17px "Courier New", monospace';
                ctx.fillText('\u2605  NEW HIGH SCORE!  \u2605', cx, cy + 24);
            } else if (highScore > 0) {
                ctx.fillStyle  = '#888';
                ctx.shadowBlur = 0;
                ctx.font = '13px "Courier New", monospace';
                ctx.fillText(`Best: ${highScore}`, cx, cy + 24);
            }

            // Stats
            ctx.shadowBlur = 0;
            ctx.font = '13px "Courier New", monospace';
            ctx.fillStyle = '#aaffaa';
            ctx.fillText(`Total Ships Sunk: ${s.totalShipsSunk ?? 0}`, cx, cy + 62);
            const accuracy = (s.totalTorpedosFired ?? 0) > 0
                ? Math.round(100 * (s.totalTorpedosHit ?? 0) / s.totalTorpedosFired)
                : 0;
            ctx.fillText(`Accuracy: ${accuracy}%  (${s.totalTorpedosHit ?? 0} / ${s.totalTorpedosFired ?? 0})`, cx, cy + 82);

            // Blinking prompt
            if (Math.floor(Date.now() / 600) % 2 === 0) {
                ctx.fillStyle   = '#00FF00';
                ctx.shadowColor = '#00FF00';
                ctx.shadowBlur  = 10;
                ctx.font = 'bold 15px "Courier New", monospace';
                ctx.fillText('[ CLICK OR PRESS SPACE TO CONTINUE ]', cx, cy + 130);
            }

            ctx.restore();
        }
    };

    // ── Private helpers ───────────────────────────────────────────────────────

    function _wrapText(ctx, text, maxWidth, font) {
        ctx.save();
        ctx.font = font;
        const lines = [];
        for (const para of (text ?? '').split('\n')) {
            const words = para.split(' ');
            let current = '';
            for (const word of words) {
                const candidate = current ? `${current} ${word}` : word;
                if (current && ctx.measureText(candidate).width > maxWidth) {
                    lines.push(current);
                    current = word;
                } else {
                    current = candidate;
                }
            }
            if (current) lines.push(current);
        }
        ctx.restore();
        return lines;
    }

    function _drawHighScoreTable(ctx, canvas, s) {
        const cx = canvas.width / 2;
        const entries = (s && s.highScoreList) ? s.highScoreList : [];

        ctx.shadowBlur = 12;
        ctx.shadowColor = '#FFD700';
        ctx.fillStyle   = '#FFD700';
        ctx.textAlign   = 'center';
        ctx.font = 'bold 28px "Courier New", monospace';
        ctx.fillText('★  HIGH SCORES  ★', cx, canvas.height / 2 - 20);

        ctx.shadowBlur = 0;
        const startY = canvas.height / 2 + 18;
        const rowH   = 32;

        if (entries.length === 0) {
            ctx.fillStyle = '#555';
            ctx.font = '15px "Courier New", monospace';
            ctx.fillText('No scores yet — dive in!', cx, startY + 20);
        } else {
            ctx.font = '11px "Courier New", monospace';
            ctx.fillStyle = '#555';
            ctx.fillText(
                '#      NAME                  SCORE     MODE',
                cx, startY - 4
            );

            for (let i = 0; i < entries.length; i++) {
                const e   = entries[i];
                const y   = startY + rowH + i * rowH;
                const isTop = i === 0;
                const rankColor = i === 0 ? '#FFD700' : i === 1 ? '#C0C0C0' : i === 2 ? '#cd7f32' : '#559955';

                ctx.textAlign = 'right';
                ctx.fillStyle = rankColor;
                ctx.font = isTop ? 'bold 16px "Courier New", monospace' : '15px "Courier New", monospace';
                ctx.fillText(`${i + 1}.`, cx - 220, y);

                ctx.textAlign = 'left';
                ctx.fillStyle = isTop ? '#FFD700' : '#00EE00';
                const nameStr = (e.name ?? '???').substring(0, 12).padEnd(12);
                ctx.fillText(nameStr, cx - 205, y);

                ctx.textAlign = 'right';
                ctx.fillStyle = isTop ? '#FFD700' : '#aaffaa';
                ctx.fillText(String(e.score).padStart(7), cx + 60, y);

                ctx.textAlign = 'left';
                ctx.fillStyle = e.mode === 'Campaign' ? '#88aaff' : '#888';
                ctx.font = '12px "Courier New", monospace';
                ctx.fillText(e.mode === 'Campaign' ? 'CAMPAIGN' : 'ARCADE', cx + 76, y);
            }
        }
        ctx.textAlign = 'center';
    }
})();
