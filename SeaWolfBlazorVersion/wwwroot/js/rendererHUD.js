// rendererHUD.js — HUD overlay, tube spread lines, periscope vignette
window.SeaWolfRendererHUD = (() => {
    // Eases toward the selected tube's angle each frame instead of snapping,
    // so switching tubes reads as a smooth periscope pan.
    let _visualAngleDeg = null;

    // Eases the displayed score toward the real score instead of snapping,
    // so a big multi-kill point gain "rolls up" rather than jumping — a
    // small bit of juice on the number players look at most.
    let _displayedScore = null;

    return {
        drawHUD(core, s) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();
            const COLORS = core.getColors();

            if (_displayedScore === null || s.score < _displayedScore) {
                // First frame, or a lower score than last shown — that only
                // happens on a fresh game, so snap instead of counting down.
                _displayedScore = s.score;
            } else if (s.score > _displayedScore) {
                _displayedScore += Math.max(1, (s.score - _displayedScore) * 0.18);
                if (_displayedScore > s.score - 0.5) _displayedScore = s.score;
            }

            ctx.save();
            ctx.font = 'bold 16px "Courier New", monospace';
            ctx.fillStyle  = COLORS.hud;
            ctx.shadowColor = COLORS.hud;
            ctx.shadowBlur = 6;

            // Top-left: score
            ctx.textAlign = 'left';
            ctx.fillText(`SCORE: ${Math.round(_displayedScore)}`, 18, 28);
            if (s.highScore > 0) {
                ctx.font = '12px "Courier New", monospace';
                ctx.fillStyle = COLORS.hudDim;
                ctx.fillText(`BEST: ${s.highScore}`, 18, 46);
            }

            // Top-center: wave (arcade only)
            if (s.mode !== 'Campaign') {
            ctx.font = 'bold 18px "Courier New", monospace';
            ctx.fillStyle = COLORS.hud;
            ctx.textAlign = 'center';
            ctx.fillText(`WAVE ${s.wave}`, canvas.width / 2, 28);

            // Ships remaining this wave
            const spawned   = s.shipsSpawnedThisWave ?? 0;
            const totalW    = s.waveTotalShips ?? 0;
            const notYet    = Math.max(0, totalW - spawned);
            const remaining = (s.ships ? s.ships.length : 0) + notYet;
            if (totalW > 0) {
                ctx.font = '11px "Courier New", monospace';
                ctx.fillStyle = remaining > 0 ? COLORS.hudDim : '#00FF88';
                ctx.shadowBlur = 3;
                ctx.fillText(`${remaining} SHIPS LEFT`, canvas.width / 2, 46);
            }
            }

            // Combo multiplier badge
            const combo = s.comboCount ?? 0;
            if (combo >= 2) {
                const mult = combo >= 7 ? 4 : combo >= 4 ? 3 : 2;
                const clr  = ['', '', '#00FF88', '#FFD700', '#FF6600'][mult];
                const pulse = 1 + Math.sin(Date.now() * 0.009) * 0.13;
                ctx.font = `bold ${Math.floor(19 * pulse)}px "Courier New", monospace`;
                ctx.fillStyle  = clr;
                ctx.shadowColor = clr;
                ctx.shadowBlur = 14;
                ctx.fillText(`×${mult} COMBO`, canvas.width / 2, 52);
            }

            // Top-right: torpedo icons
            ctx.textAlign = 'right';
            ctx.font = '12px "Courier New", monospace';
            ctx.fillText('TORPEDOES', canvas.width - 18, 18);
            for (let i = 0; i < 2; i++) {
                const tx     = canvas.width - 22 - i * 22;
                const ty     = 30;
                const loaded = i < s.torpedoCount;
                ctx.save();
                ctx.translate(tx, ty);
                ctx.fillStyle   = loaded ? COLORS.hud    : COLORS.hudDim;
                ctx.strokeStyle = loaded ? COLORS.hud    : COLORS.hudDim;
                ctx.shadowColor = loaded ? COLORS.hud    : 'transparent';
                ctx.shadowBlur  = loaded ? 8 : 0;
                ctx.lineWidth   = 1.5;
                ctx.beginPath();
                ctx.moveTo(0, -10);
                ctx.lineTo(-4, 10);
                ctx.lineTo(4, 10);
                ctx.closePath();
                loaded ? ctx.fill() : ctx.stroke();
                ctx.restore();
            }

            // Reload bar — bottom center
            if (s.isReloading) {
                ctx.shadowBlur = 0;
                ctx.textAlign  = 'center';
                ctx.font = 'bold 14px "Courier New", monospace';
                ctx.fillStyle = '#ffcc00';
                ctx.fillText('RELOADING...', canvas.width / 2, canvas.height - 30);
                const barW = 200, barH = 8;
                const bx = (canvas.width - barW) / 2, by = canvas.height - 22;
                ctx.fillStyle = '#333';
                ctx.fillRect(bx, by, barW, barH);
                const progress = Math.min(s.reloadTimer / (s.reloadDurationValue ?? 4.0), 1);
                ctx.fillStyle = '#ffcc00';
                ctx.fillRect(bx, by, barW * progress, barH);
                ctx.strokeStyle = '#ffcc00';
                ctx.lineWidth = 1;
                ctx.strokeRect(bx, by, barW, barH);
            }

            // Bottom-left: escaped ships (arcade only)
            if (s.mode !== 'Campaign') {
            const maxEsc = 5;
            const escaped = s.shipsEscaped || 0;
            ctx.shadowBlur = 0;
            ctx.textAlign  = 'left';
            ctx.font = '11px "Courier New", monospace';
            ctx.fillStyle = escaped >= 3 ? '#FF4400' : '#886600';
            ctx.fillText('ESCAPED', 18, canvas.height - 38);
            for (let i = 0; i < maxEsc; i++) {
                const ex = 20 + i * 22, ey = canvas.height - 22;
                const active = i < escaped;
                ctx.save();
                ctx.translate(ex, ey);
                ctx.fillStyle   = active ? '#FF2200' : '#333';
                ctx.strokeStyle = active ? '#FF2200' : '#555';
                ctx.shadowColor = active ? '#FF2200' : 'transparent';
                ctx.shadowBlur  = active ? 7 : 0;
                ctx.lineWidth   = 1.2;
                ctx.beginPath();
                ctx.ellipse(0, 2, 8, 3, 0, 0, Math.PI * 2);
                active ? ctx.fill() : ctx.stroke();
                if (active) {
                    ctx.fillRect(-3, -4, 6, 5);
                } else {
                    ctx.strokeRect(-3, -4, 6, 5);
                }
                ctx.restore();
            }
            }

            ctx.restore();
        },

        drawTubeSpread(core, s) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();
            const COLORS = core.getColors();

            // Read the tube fan from state (computed once in TorpedoTubes.cs)
            // instead of keeping a second hardcoded copy here that could
            // drift out of sync with the engine.
            const TUBE_ANGLES_DEG = (s.tubeAnglesDeg && s.tubeAnglesDeg.length)
                ? s.tubeAnglesDeg
                : [-56.98, -37.57, 0, 37.57, 56.98];
            const LAUNCH_Y = 680;
            const LINE_LEN = 530;
            const aimX     = canvas.width / 2;
            const selected = Math.min(
                Math.max(s.selectedTube ?? Math.floor(TUBE_ANGLES_DEG.length / 2), 0),
                TUBE_ANGLES_DEG.length - 1
            );

            // Ease the on-screen aim toward the selected tube instead of
            // snapping — this is the "smooth periscope pan" feel. The actual
            // fired trajectory always uses the exact discrete tube angle
            // (computed server-side in FireTorpedoFromTube); only the visual
            // indicator glides.
            const targetAngle = TUBE_ANGLES_DEG[selected];
            if (_visualAngleDeg === null) _visualAngleDeg = targetAngle;
            _visualAngleDeg += (targetAngle - _visualAngleDeg) * 0.25;
            if (Math.abs(targetAngle - _visualAngleDeg) < 0.05) _visualAngleDeg = targetAngle;

            ctx.save();

            // Dim reference fan — every tube except the active one, static
            TUBE_ANGLES_DEG.forEach((angleDeg, i) => {
                if (i === selected) return;
                const rad  = angleDeg * Math.PI / 180;
                const endX = aimX + Math.sin(rad) * LINE_LEN;
                const endY = LAUNCH_Y - Math.cos(rad) * LINE_LEN;

                ctx.beginPath();
                ctx.moveTo(aimX, LAUNCH_Y);
                ctx.lineTo(endX, endY);
                ctx.strokeStyle = COLORS.hudDim;
                ctx.lineWidth   = 1;
                ctx.globalAlpha = 0.35;
                ctx.shadowBlur  = 0;
                ctx.setLineDash([6, 5]);
                ctx.stroke();
            });

            // Active tube line — bright, follows the eased angle
            const activeRad  = _visualAngleDeg * Math.PI / 180;
            const activeEndX = aimX + Math.sin(activeRad) * LINE_LEN;
            const activeEndY = LAUNCH_Y - Math.cos(activeRad) * LINE_LEN;

            ctx.setLineDash([]);
            ctx.beginPath();
            ctx.moveTo(aimX, LAUNCH_Y);
            ctx.lineTo(activeEndX, activeEndY);
            ctx.strokeStyle = COLORS.crosshair;
            ctx.lineWidth   = 1.5;
            ctx.globalAlpha = 0.85;
            ctx.shadowColor = COLORS.crosshair;
            ctx.shadowBlur  = 5;
            ctx.stroke();

            ctx.setLineDash([]);
            ctx.globalAlpha = 1;
            ctx.shadowBlur  = 0;

            // Launch-origin crossmark
            ctx.strokeStyle = COLORS.hud;
            ctx.lineWidth   = 2;
            ctx.shadowColor = COLORS.hud;
            ctx.shadowBlur  = 8;
            ctx.beginPath();
            ctx.moveTo(aimX - 18, LAUNCH_Y);
            ctx.lineTo(aimX + 18, LAUNCH_Y);
            ctx.stroke();
            ctx.beginPath();
            ctx.moveTo(aimX, LAUNCH_Y - 12);
            ctx.lineTo(aimX, LAUNCH_Y + 6);
            ctx.stroke();

            // Aim indicator crosshair — rides the same eased angle as the
            // active line, so it glides smoothly between tubes too.
            const targetY = 420;
            const dist    = (LAUNCH_Y - targetY) / Math.cos(activeRad);
            const targetX = aimX + Math.sin(activeRad) * dist;
            const sz = 11;

            ctx.strokeStyle = COLORS.crosshair;
            ctx.lineWidth   = 1.5;
            ctx.shadowColor = COLORS.crosshair;
            ctx.shadowBlur  = 7;
            ctx.beginPath();
            ctx.moveTo(targetX - sz, targetY);
            ctx.lineTo(targetX + sz, targetY);
            ctx.stroke();
            ctx.beginPath();
            ctx.moveTo(targetX, targetY - sz);
            ctx.lineTo(targetX, targetY + sz);
            ctx.stroke();
            ctx.beginPath();
            ctx.arc(targetX, targetY, 3, 0, Math.PI * 2);
            ctx.fillStyle = COLORS.crosshair;
            ctx.fill();

            ctx.restore();
        },

        drawPeriscopeVignette(core) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();

            const cx = canvas.width / 2, cy = canvas.height / 2;
            const r  = canvas.height / 1.44;
            const grad = ctx.createRadialGradient(cx, cy, r * 0.7, cx, cy, r * 1.1);
            grad.addColorStop(0, 'rgba(0,0,0,0)');
            grad.addColorStop(1, 'rgba(0,0,0,0.88)');
            ctx.fillStyle = grad;
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            ctx.save();
            ctx.strokeStyle = 'rgba(0,255,0,0.15)';
            ctx.lineWidth = 3;
            ctx.beginPath();
            ctx.arc(cx, cy, r * 0.72, 0, Math.PI * 2);
            ctx.stroke();
            ctx.restore();

            // CRT scan lines
            ctx.save();
            ctx.fillStyle = 'rgba(0,0,0,0.07)';
            for (let sy = 0; sy < canvas.height; sy += 3) {
                ctx.fillRect(0, sy, canvas.width, 1);
            }
            ctx.restore();
        },

        drawCampaignHUD(core, s) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();

            const missions  = s.campaignMissions ?? [];
            const mIdx      = (s.campaignMission ?? 1) - 1;
            const mission   = missions[mIdx] ?? {};
            const reqSinks  = mission.requiredSinks ?? 0;
            const current   = s.campaignSinks ?? 0;
            const lives     = s.campaignLives ?? 0;
            const budget    = s.torpedoBudgetLeft ?? 0;
            const hasBudget = (mission.torpedoBudget ?? 0) > 0;

            ctx.save();
            ctx.textAlign  = 'left';
            ctx.shadowBlur = 0;

            // Mission label (bottom-right area, above wave counter)
            const rx = canvas.width - 18;
            const baseY = canvas.height - 68;

            ctx.textAlign   = 'right';
            ctx.font = 'bold 11px "Courier New", monospace';
            ctx.fillStyle   = '#FFD700';
            ctx.fillText(`MISSION ${s.campaignMission ?? 1}`, rx, baseY);

            // Objective progress bar
            const barW = 160, barH = 7;
            const bx   = rx - barW;
            const by   = baseY + 6;
            const ratio = reqSinks > 0 ? Math.min(current / reqSinks, 1) : 0;
            const barColor = ratio >= 1 ? '#00FF88' : '#FFD700';

            ctx.fillStyle = '#222';
            ctx.fillRect(bx, by, barW, barH);
            ctx.fillStyle = barColor;
            ctx.fillRect(bx, by, barW * ratio, barH);
            ctx.strokeStyle = barColor;
            ctx.lineWidth   = 1;
            ctx.strokeRect(bx, by, barW, barH);

            ctx.font = '11px "Courier New", monospace';
            ctx.fillStyle = '#aaa';
            ctx.fillText(`OBJ: ${current}/${reqSinks}`, rx, by + barH + 12);

            // Lives — pulses red when critically low instead of just
            // switching to a flat colour, so running out feels urgent.
            const lifeY = by + barH + 28;
            if (lives <= 1) {
                const pulse = 0.6 + Math.sin(Date.now() * 0.012) * 0.4;
                ctx.fillStyle = `rgba(255,${Math.round(34 + pulse * 60)},0,1)`;
                ctx.shadowColor = '#FF2200';
                ctx.shadowBlur  = 6 + pulse * 6;
            } else {
                ctx.fillStyle = '#aaffaa';
                ctx.shadowBlur = 0;
            }
            ctx.fillText(`LIVES: ${'♥'.repeat(Math.max(0, lives))}`, rx, lifeY);
            ctx.shadowBlur = 0;

            // Torpedo budget
            if (hasBudget) {
                ctx.fillStyle = budget <= 2 ? '#FF6600' : '#aaffaa';
                ctx.fillText(`TORP BUDGET: ${budget}`, rx, lifeY + 16);
            }

            ctx.restore();
        }
    };
})();
