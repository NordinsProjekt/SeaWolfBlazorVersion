window.SeaWolfRenderer = (() => {
    let canvas = null;
    let ctx = null;
    let waveOffset = 0; // animated wave phase
    let boatImage  = null;
    let boatSprite  = null; // cropped + fringe-cleaned offscreen canvas
    let cargoSprite = null; // dedicated cargo-ship sprite

    // ── Colour palette ──────────────────────────────────────────────────────
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
        // ── Public API ───────────────────────────────────────────────────────

        init(canvasId) {
            canvas = document.getElementById(canvasId);
            ctx = canvas.getContext('2d');

            // General boat sprite (destroyer & PT boat) — crop row 640+
            // to remove the embedded ocean backdrop.
            boatImage = new Image();
            boatImage.onload = () => { boatSprite = _prepareSprite(boatImage, null, true); };
            boatImage.src = 'images/boat.png';

            // Dedicated cargo-ship sprite — crop at row 820 to keep
            // the hull waterline but drop the deepest wake pixels.
            const cargoImg = new Image();
            cargoImg.onload = () => { cargoSprite = _prepareSprite(cargoImg, 820); };
            cargoImg.src = 'images/cargo.png';
        },

        renderFrame(stateJson) {
            if (!ctx) return;
            const s = JSON.parse(stateJson);
            waveOffset += 0.5;

            ctx.clearRect(0, 0, canvas.width, canvas.height);
            this._drawBackground();

            if (s.status === 'StartScreen') {
                this._drawStartScreen();
                this._drawPeriscopeVignette();
                return;
            }

            if (s.status === 'GameOver') {
                this._drawGameOver(s.score, s.highScore, s.score >= s.highScore && s.score > 0);
                this._drawPeriscopeVignette();
                return;
            }

            // World — wrapped in screen-shake transform
            ctx.save();
            if ((s.shakeTimer ?? 0) > 0) {
                const mag = s.shakeTimer * 10;
                ctx.translate((Math.random() - 0.5) * mag, (Math.random() - 0.5) * mag);
            }
            s.ships.forEach(ship => this._drawShip(ship));
            s.torpedoes.forEach(t => this._drawTorpedo(t));
            s.explosions.forEach(ex => this._drawExplosion(ex));
            ctx.restore();

            // Fixed-position UI (no shake)
            if (s.floatingTexts) s.floatingTexts.forEach(ft => this._drawFloatingText(ft));
            this._drawCrosshair(s.mouseX, s.mouseY);
            this._drawPeriscopeVignette();
            this._drawHUD(s);

            if (s.status === 'WaveClear')
                this._drawWaveClear(s.wave, s.lastWaveBonus, s.waveClearTimer, s.lastAccuracy, s.accuracyBonus);
            if (s.status === 'Paused')
                this._drawPaused();
        },

        // ── Background ───────────────────────────────────────────────────────

        _drawBackground() {
            // Sky / deep ocean gradient
            const grad = ctx.createLinearGradient(0, 0, 0, canvas.height);
            grad.addColorStop(0,    '#000a14');
            grad.addColorStop(0.35, '#001428');
            grad.addColorStop(0.5,  '#001e3c');
            grad.addColorStop(1,    '#003060');
            ctx.fillStyle = grad;
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            // Dark water fill below horizon — makes ocean zone distinct
            ctx.fillStyle = 'rgba(0, 20, 55, 0.55)';
            ctx.fillRect(0, 278, canvas.width, canvas.height - 278);

            // Animated ocean surface waves — 14 rows for dense water texture
            for (let row = 0; row < 14; row++) {
                const y = 272 + row * 22;
                const alpha = 0.15 + (row / 14) * 0.45;
                const blue  = Math.max(60, 160 - row * 8);
                ctx.strokeStyle = `rgba(0,${blue},200,${alpha})`;
                ctx.lineWidth   = row < 2 ? 1.8 : 1;
                ctx.beginPath();
                for (let x = 0; x <= canvas.width; x += 4) {
                    const amp = 3 + row * 0.4;
                    const freq = 0.022 - row * 0.001;
                    const dy = Math.sin((x * freq) + waveOffset * 0.05 + row * 0.8) * amp;
                    x === 0 ? ctx.moveTo(x, y + dy) : ctx.lineTo(x, y + dy);
                }
                ctx.stroke();
            }

            // Horizon shimmer line
            ctx.strokeStyle = 'rgba(0,200,255,0.20)';
            ctx.lineWidth = 1.5;
            ctx.beginPath();
            ctx.moveTo(0, 276);
            ctx.lineTo(canvas.width, 276);
            ctx.stroke();
        },

        // ── Ships ────────────────────────────────────────────────────────────

        _drawShip(ship) {
            const sinkProgress = ship.damageState === 'Sinking'
                ? Math.min(ship.sinkTimer / 1.5, 1)
                : 0;

            ctx.save();
            ctx.translate(ship.x, ship.y);
            // Mirror ships coming from the right so they always face their direction of travel
            if ((ship.direction ?? 1) === -1) ctx.scale(-1, 1);
            if (sinkProgress > 0) {
                ctx.rotate(sinkProgress * 0.4);
                ctx.globalAlpha = 1 - sinkProgress * 0.6;
            }

            const w = ship.width, h = ship.height;
            const type = ship.type ? ship.type.toLowerCase() : 'destroyer';

            // Cargo ships get their dedicated sprite; destroyer & PT boat share boatSprite
            const sprite = (type === 'cargo' && cargoSprite) ? cargoSprite : boatSprite;

            if (sprite) {
                const drawH = w * (sprite.height / sprite.width);
                ctx.drawImage(sprite, -w / 2, -drawH / 2, w, drawH);

                // Destroyer/PT boat get a colour tint to stay distinct;
                // cargo uses its natural Copilot-generated colours.
                if (type !== 'cargo') {
                    const tints = {
                        destroyer: 'rgba(60,100,180,0.28)',
                        ptboat:    'rgba(40,120,40, 0.28)',
                    };
                    ctx.globalCompositeOperation = 'source-atop';
                    ctx.fillStyle = tints[type] ?? 'rgba(60,100,180,0.28)';
                    ctx.fillRect(-w / 2, -drawH / 2, w, drawH);
                    ctx.globalCompositeOperation = 'source-over';
                }

                if (ship.damageState === 'Burning') this._drawBurnDamage(w, drawH);
            } else {
                // Vector fallback while sprites load
                if (type === 'destroyer')   this._drawDestroyer(w, h, ship.damageState);
                else if (type === 'ptboat') this._drawPtBoat(w, h, ship.damageState);
                else if (type === 'cargo')  this._drawCargo(w, h, ship.damageState);
            }

            ctx.restore();

            // Fire particles drawn in world space
            if (ship.damageState === 'Burning' && ship.fireParticles)
                this._drawFireParticles(ship.fireParticles);
        },

        _drawDestroyer(w, h, state) {
            const c = COLORS.destroyer;
            const hw = w / 2, hh = h / 2;

            // Hull
            ctx.fillStyle = c.hull;
            ctx.beginPath();
            ctx.moveTo(-hw, hh);          // stern bottom
            ctx.lineTo( hw * 0.85, hh);  // bow bottom
            ctx.lineTo( hw, 0);           // bow tip
            ctx.lineTo( hw * 0.85, -hh + 8);
            ctx.lineTo(-hw, -hh + 8);
            ctx.closePath();
            ctx.fill();

            // Waterline strip
            ctx.fillStyle = '#4a6080';
            ctx.fillRect(-hw, hh - 5, w, 5);

            // Superstructure / bridge
            ctx.fillStyle = c.super;
            ctx.fillRect(-hw * 0.2, -hh + 8, hw * 0.55, hh - 4);

            // Gun turret fore
            ctx.fillStyle = c.accent;
            ctx.beginPath();
            ctx.arc(hw * 0.55, -hh + 14, 6, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillRect(hw * 0.55, -hh + 11, 14, 3); // barrel

            // Gun turret aft
            ctx.beginPath();
            ctx.arc(-hw * 0.3, -hh + 14, 5, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillRect(-hw * 0.3, -hh + 11, -12, 3);

            // Radar dome
            ctx.fillStyle = '#a0b0c0';
            ctx.beginPath();
            ctx.arc(-hw * 0.05, -hh + 3, 4, Math.PI, 0);
            ctx.fill();

            // Smokestack
            ctx.fillStyle = '#1e2a38';
            ctx.fillRect(-hw * 0.12, -hh + 8, 8, 10);
            // Smoke puff
            ctx.fillStyle = 'rgba(255,255,255,0.12)';
            ctx.beginPath();
            ctx.arc(-hw * 0.12 + 4, -hh + 2, 7, 0, Math.PI * 2);
            ctx.fill();

            if (state === 'Burning') this._drawBurnDamage(w, h);
        },

        _drawPtBoat(w, h, state) {
            const c = COLORS.ptBoat;
            const hw = w / 2, hh = h / 2;

            // Low sleek hull
            ctx.fillStyle = c.hull;
            ctx.beginPath();
            ctx.moveTo(-hw, hh);
            ctx.lineTo( hw * 0.9, hh);
            ctx.lineTo( hw, hh * 0.3);
            ctx.lineTo( hw * 0.8, -hh + 6);
            ctx.lineTo(-hw, -hh + 6);
            ctx.closePath();
            ctx.fill();

            // Wood deck strip
            ctx.fillStyle = c.accent;
            ctx.fillRect(-hw * 0.8, -hh + 6, w * 0.8, 5);

            // Pilothouse
            ctx.fillStyle = c.super;
            ctx.fillRect(-hw * 0.1, -hh + 2, hw * 0.5, hh - 4);

            // Twin torpedo tubes
            ctx.fillStyle = '#556b2f';
            ctx.fillRect(-hw * 0.45, -hh + 8, 22, 4);
            ctx.fillRect(-hw * 0.45, -hh + 14, 22, 4);

            // Wake effect
            ctx.fillStyle = 'rgba(255,255,255,0.25)';
            ctx.beginPath();
            ctx.ellipse(-hw - 10, hh * 0.5, 15, 4, 0, 0, Math.PI * 2);
            ctx.fill();

            if (state === 'Burning') this._drawBurnDamage(w, h);
        },

        _drawCargo(w, h, state) {
            const c = COLORS.cargo;
            const hw = w / 2, hh = h / 2;

            // Large rounded hull
            ctx.fillStyle = c.hull;
            ctx.beginPath();
            ctx.moveTo(-hw, hh);
            ctx.lineTo( hw * 0.8, hh);
            ctx.lineTo( hw, hh * 0.4);
            ctx.lineTo( hw * 0.7, -hh + 12);
            ctx.lineTo(-hw, -hh + 12);
            ctx.closePath();
            ctx.fill();

            // Container stacks (alternating gold + blue)
            const containerColors = ['#d4a853', '#4a6a8a', '#d4a853', '#4a6a8a', '#d4a853'];
            const cw = 22, ch = 14;
            for (let i = 0; i < 5; i++) {
                ctx.fillStyle = containerColors[i % 2];
                ctx.fillRect(-hw * 0.55 + i * (cw + 2), -hh + 12, cw, ch);
                // Second row (some containers)
                if (i < 3) {
                    ctx.fillStyle = containerColors[(i + 1) % 2];
                    ctx.fillRect(-hw * 0.55 + i * (cw + 2), -hh + 12 - ch - 1, cw, ch);
                }
            }

            // Bridge / accommodation block at stern
            ctx.fillStyle = c.super;
            ctx.fillRect(-hw * 0.9, -hh + 4, hw * 0.35, h - 16);

            // Smokestack
            ctx.fillStyle = '#2a1a10';
            ctx.fillRect(-hw * 0.75, -hh - 4, 10, 16);
            ctx.fillStyle = 'rgba(150,150,150,0.15)';
            ctx.beginPath();
            ctx.arc(-hw * 0.75 + 5, -hh - 8, 9, 0, Math.PI * 2);
            ctx.fill();

            // Cargo boom (crane arm)
            ctx.strokeStyle = '#8b7355';
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.moveTo(-hw * 0.55, -hh + 12);
            ctx.lineTo(-hw * 0.2, -hh - 6);
            ctx.stroke();

            if (state === 'Burning') this._drawBurnDamage(w, h);
        },

        _drawBurnDamage(w, h) {
            // Scorch marks overlay — source-atop so it only paints over opaque sprite pixels
            ctx.globalCompositeOperation = 'source-atop';
            ctx.fillStyle = 'rgba(180,60,0,0.25)';
            ctx.fillRect(-w / 2, -h / 2, w, h);
            ctx.globalCompositeOperation = 'source-over';
        },

        // ── Fire particles ────────────────────────────────────────────────────

        _drawFireParticles(particles) {
            particles.forEach(p => {
                const alpha = Math.max(0, p.life);
                const grad = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, p.size);
                grad.addColorStop(0,   `rgba(255, 255, 200, ${alpha})`);
                grad.addColorStop(0.4, `rgba(255, 140,   0, ${alpha * 0.8})`);
                grad.addColorStop(1,   `rgba(180,   0,   0, 0)`);
                ctx.fillStyle = grad;
                ctx.beginPath();
                ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
                ctx.fill();
            });
        },

        // ── Floating score text ───────────────────────────────────────────────

        _drawFloatingText(ft) {
            const alpha = Math.max(0, ft.life / ft.maxLife);
            ctx.save();
            ctx.globalAlpha = alpha;
            ctx.textAlign = 'center';
            ctx.font = 'bold 17px "Courier New", monospace';
            ctx.fillStyle = ft.color ?? '#FFD700';
            ctx.shadowColor = ft.color ?? '#FFD700';
            ctx.shadowBlur = 10;
            ctx.fillText(ft.text, ft.x, ft.y);
            ctx.restore();
        },

        // ── Torpedo ───────────────────────────────────────────────────────────

        _drawTorpedo(t) {
            ctx.save();
            ctx.fillStyle = COLORS.torpedo;
            ctx.shadowColor = COLORS.torpedo;
            ctx.shadowBlur = 6;
            // Nose
            ctx.beginPath();
            ctx.moveTo(t.x, t.y - t.height / 2);
            ctx.lineTo(t.x - t.width / 2, t.y + t.height / 2);
            ctx.lineTo(t.x + t.width / 2, t.y + t.height / 2);
            ctx.closePath();
            ctx.fill();
            // Bubble trail
            ctx.shadowBlur = 0;
            ctx.fillStyle = 'rgba(200,255,255,0.3)';
            for (let i = 1; i <= 4; i++) {
                ctx.beginPath();
                ctx.arc(t.x + (Math.random() - 0.5) * 4,
                        t.y + t.height / 2 + i * 5,
                        2 - i * 0.3, 0, Math.PI * 2);
                ctx.fill();
            }
            ctx.restore();
        },

        // ── Explosion ─────────────────────────────────────────────────────────

        _drawExplosion(ex) {
            ctx.save();
            const r = ex.radius;
            // Outer ring
            ctx.globalAlpha = ex.opacity * 0.6;
            ctx.fillStyle = '#FF4400';
            ctx.beginPath();
            ctx.arc(ex.x, ex.y, r, 0, Math.PI * 2);
            ctx.fill();
            // Mid ring
            ctx.fillStyle = '#FF8800';
            ctx.beginPath();
            ctx.arc(ex.x, ex.y, r * 0.7, 0, Math.PI * 2);
            ctx.fill();
            // Core
            ctx.fillStyle = '#FFFF88';
            ctx.beginPath();
            ctx.arc(ex.x, ex.y, r * 0.45, 0, Math.PI * 2);
            ctx.fill();
            // Sparks
            ctx.globalAlpha = 1;
            ex.sparks.forEach(spark => {
                ctx.fillStyle = `rgba(255, 200, 50, ${Math.max(0, spark.life)})`;
                ctx.fillRect(spark.x - 2, spark.y - 2, 4, 4);
            });
            ctx.restore();
        },

        // ── Crosshair ─────────────────────────────────────────────────────────

        _drawCrosshair(mx, my) {
            const size = 20;
            ctx.save();
            ctx.strokeStyle = COLORS.crosshair;
            ctx.lineWidth = 1.5;
            ctx.shadowColor = COLORS.crosshair;
            ctx.shadowBlur = 4;
            // Horizontal
            ctx.beginPath();
            ctx.moveTo(mx - size, my);
            ctx.lineTo(mx + size, my);
            ctx.stroke();
            // Vertical
            ctx.beginPath();
            ctx.moveTo(mx, my - size);
            ctx.lineTo(mx, my + size);
            ctx.stroke();
            // Centre dot
            ctx.beginPath();
            ctx.arc(mx, my, 2, 0, Math.PI * 2);
            ctx.fillStyle = COLORS.crosshair;
            ctx.fill();
            ctx.restore();
        },

        // ── Periscope vignette ────────────────────────────────────────────────

        _drawPeriscopeVignette() {
            const cx = canvas.width / 2, cy = canvas.height / 2;
            const r  = canvas.width * 0.52;
            const grad = ctx.createRadialGradient(cx, cy, r * 0.7, cx, cy, r * 1.1);
            grad.addColorStop(0, 'rgba(0,0,0,0)');
            grad.addColorStop(1, 'rgba(0,0,0,0.88)');
            ctx.fillStyle = grad;
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            // Periscope circle border
            ctx.save();
            ctx.strokeStyle = 'rgba(0,255,0,0.15)';
            ctx.lineWidth = 3;
            ctx.beginPath();
            ctx.arc(cx, cy, r * 0.72, 0, Math.PI * 2);
            ctx.stroke();
            ctx.restore();
        },

        // ── HUD ───────────────────────────────────────────────────────────────

        _drawHUD(s) {
            ctx.save();
            ctx.font = 'bold 16px "Courier New", monospace';
            ctx.fillStyle = COLORS.hud;
            ctx.shadowColor = COLORS.hud;
            ctx.shadowBlur = 6;

            // Top-left: score
            ctx.textAlign = 'left';
            ctx.fillText(`SCORE: ${s.score}`, 18, 28);
            if (s.highScore > 0) {
                ctx.font = '12px "Courier New", monospace';
                ctx.fillStyle = COLORS.hudDim;
                ctx.fillText(`BEST: ${s.highScore}`, 18, 46);
            }

            // Top-center: wave
            ctx.font = 'bold 18px "Courier New", monospace';
            ctx.fillStyle = COLORS.hud;
            ctx.textAlign = 'center';
            ctx.fillText(`WAVE ${s.wave}`, canvas.width / 2, 28);

            // Combo multiplier badge
            const combo = s.comboCount ?? 0;
            if (combo >= 2) {
                const mult = combo >= 7 ? 4 : combo >= 4 ? 3 : 2;
                const clr = ['', '', '#00FF88', '#FFD700', '#FF6600'][mult];
                const pulse = 1 + Math.sin(Date.now() * 0.009) * 0.13;
                ctx.font = `bold ${Math.floor(19 * pulse)}px "Courier New", monospace`;
                ctx.fillStyle = clr;
                ctx.shadowColor = clr;
                ctx.shadowBlur = 14;
                ctx.fillText(`×${mult} COMBO`, canvas.width / 2, 52);
            }

            // Top-right: torpedo icons
            ctx.textAlign = 'right';
            ctx.font = '12px "Courier New", monospace';
            ctx.fillText('TORPEDOES', canvas.width - 18, 18);
            for (let i = 0; i < 5; i++) {
                const x = canvas.width - 18 - i * 20;
                const y = 28;
                ctx.beginPath();
                ctx.arc(x, y, 7, 0, Math.PI * 2);
                if (i < s.torpedoCount) {
                    ctx.fillStyle = COLORS.hud;
                    ctx.shadowBlur = 8;
                    ctx.fill();
                } else {
                    ctx.strokeStyle = COLORS.hudDim;
                    ctx.lineWidth = 1.5;
                    ctx.shadowBlur = 0;
                    ctx.stroke();
                }
            }

            // Reload bar — bottom center
            if (s.isReloading) {
                ctx.shadowBlur = 0;
                ctx.textAlign = 'center';
                ctx.font = 'bold 14px "Courier New", monospace';
                ctx.fillStyle = '#ffcc00';
                ctx.fillText('RELOADING...', canvas.width / 2, canvas.height - 30);
                // Progress bar
                const barW = 200, barH = 8;
                const bx = (canvas.width - barW) / 2, by = canvas.height - 22;
                ctx.fillStyle = '#333';
                ctx.fillRect(bx, by, barW, barH);
                const progress = Math.min(s.reloadTimer / 2.0, 1);
                ctx.fillStyle = '#ffcc00';
                ctx.fillRect(bx, by, barW * progress, barH);
                ctx.strokeStyle = '#ffcc00';
                ctx.lineWidth = 1;
                ctx.strokeRect(bx, by, barW, barH);
            }

            // Bottom-left: escaped ships (5 = game over)
            const maxEsc = 5;
            const escaped = s.shipsEscaped || 0;
            ctx.shadowBlur = 0;
            ctx.textAlign = 'left';
            ctx.font = '11px "Courier New", monospace';
            ctx.fillStyle = escaped >= 3 ? '#FF4400' : '#886600';
            ctx.fillText('ESCAPED', 18, canvas.height - 38);
            for (let i = 0; i < maxEsc; i++) {
                const ex = 22 + i * 18, ey = canvas.height - 24;
                ctx.beginPath();
                ctx.arc(ex, ey, 6, 0, Math.PI * 2);
                if (i < escaped) {
                    ctx.fillStyle = '#FF2200';
                    ctx.shadowColor = '#FF2200';
                    ctx.shadowBlur = 6;
                    ctx.fill();
                    ctx.shadowBlur = 0;
                } else {
                    ctx.strokeStyle = '#444';
                    ctx.lineWidth = 1.5;
                    ctx.stroke();
                }
            }

            ctx.restore();
        },

        // ── Screens ───────────────────────────────────────────────────────────

        _drawStartScreen() {
            const cx = canvas.width / 2, cy = canvas.height / 2;
            ctx.save();

            // Title
            ctx.textAlign = 'center';
            ctx.shadowColor = '#00FF00';
            ctx.shadowBlur = 20;
            ctx.fillStyle = '#00FF00';
            ctx.font = 'bold 56px "Courier New", monospace';
            ctx.fillText('SEA WOLF', cx, cy - 60);

            ctx.shadowBlur = 6;
            ctx.font = 'bold 16px "Courier New", monospace';
            ctx.fillStyle = '#aaffaa';
            ctx.fillText('PERISCOPE ATTACK SIMULATION', cx, cy - 25);

            ctx.shadowBlur = 0;
            ctx.fillStyle = '#888';
            ctx.font = '13px "Courier New", monospace';
            ctx.fillText('CLICK or SPACE to fire torpedoes  |  P to pause', cx, cy + 20);
            ctx.fillText('Cargo ships take two hits to sink', cx, cy + 40);
            ctx.fillText('Chain kills to build a COMBO multiplier', cx, cy + 60);

            // Blink prompt
            if (Math.floor(Date.now() / 600) % 2 === 0) {
                ctx.fillStyle = '#00FF00';
                ctx.font = 'bold 18px "Courier New", monospace';
                ctx.shadowColor = '#00FF00';
                ctx.shadowBlur = 10;
                ctx.fillText('[ CLICK TO DIVE IN ]', cx, cy + 100);
            }

            ctx.restore();
        },

        _drawGameOver(score, highScore, isNewHigh) {
            const cx = canvas.width / 2, cy = canvas.height / 2;
            ctx.save();
            ctx.textAlign = 'center';

            ctx.fillStyle = '#FF2200';
            ctx.shadowColor = '#FF2200';
            ctx.shadowBlur = 24;
            ctx.font = 'bold 52px "Courier New", monospace';
            ctx.fillText('GAME OVER', cx, cy - 50);

            ctx.shadowBlur = 6;
            ctx.fillStyle = '#00FF00';
            ctx.font = 'bold 20px "Courier New", monospace';
            ctx.fillText(`FINAL SCORE: ${score}`, cx, cy);

            if (isNewHigh) {
                ctx.fillStyle = '#FFD700';
                ctx.font = 'bold 18px "Courier New", monospace';
                ctx.shadowColor = '#FFD700';
                ctx.fillText('★  NEW HIGH SCORE!  ★', cx, cy + 30);
            } else if (highScore > 0) {
                ctx.fillStyle = '#888';
                ctx.font = '14px "Courier New", monospace';
                ctx.shadowBlur = 0;
                ctx.fillText(`Best: ${highScore}`, cx, cy + 30);
            }

            if (Math.floor(Date.now() / 600) % 2 === 0) {
                ctx.fillStyle = '#00FF00';
                ctx.font = 'bold 16px "Courier New", monospace';
                ctx.shadowColor = '#00FF00';
                ctx.shadowBlur = 8;
                ctx.fillText('[ CLICK TO PLAY AGAIN ]', cx, cy + 80);
            }

            ctx.restore();
        },

        _drawWaveClear(wave, bonus, timer, accuracy, accuracyBonus) {
            const progress = Math.min(timer / 3.0, 1);
            const alpha = progress < 0.7 ? 1 : 1 - (progress - 0.7) / 0.3;
            const cx = canvas.width / 2, cy = canvas.height / 2;
            ctx.save();
            ctx.globalAlpha = alpha;
            ctx.textAlign = 'center';
            ctx.shadowColor = '#00FF00';
            ctx.shadowBlur = 18;
            ctx.fillStyle = '#00FF00';
            ctx.font = 'bold 36px "Courier New", monospace';
            ctx.fillText(`WAVE ${wave} CLEAR!`, cx, cy - 30);
            ctx.font = 'bold 20px "Courier New", monospace';
            ctx.fillStyle = '#FFD700';
            ctx.shadowColor = '#FFD700';
            ctx.fillText(`+${bonus} pts`, cx, cy + 10);
            if (accuracy != null && accuracy > 0) {
                const aBonusStr = accuracyBonus > 0 ? `  +${accuracyBonus} ACCURACY BONUS` : '';
                const aColor = accuracy >= 80 ? '#00FF88' : accuracy >= 60 ? '#FFD700' : '#888';
                ctx.font = '14px "Courier New", monospace';
                ctx.fillStyle = aColor;
                ctx.shadowColor = aColor;
                ctx.shadowBlur = 6;
                ctx.fillText(`ACCURACY ${accuracy}%${aBonusStr}`, cx, cy + 36);
            }
            ctx.restore();
        },

        _drawPaused() {
            const cx = canvas.width / 2, cy = canvas.height / 2;
            ctx.save();
            ctx.fillStyle = 'rgba(0,0,0,0.5)';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.textAlign = 'center';
            ctx.fillStyle = '#00FF00';
            ctx.shadowColor = '#00FF00';
            ctx.shadowBlur = 16;
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
