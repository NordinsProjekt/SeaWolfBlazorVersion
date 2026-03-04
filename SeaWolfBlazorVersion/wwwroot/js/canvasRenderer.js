window.SeaWolfRenderer = (() => {
    let canvas = null;
    let ctx = null;
    let waveOffset = 0; // animated wave phase
    let boatImage  = null;
    let boatSprite  = null; // cropped + fringe-cleaned offscreen canvas
    let cargoSprite = null; // dedicated cargo-ship sprite
    let cruiserSprite = null; // dedicated cruiser sprite
    let fishingSprite = null; // dedicated fishing-boat sprite
    let tankerSprite  = null; // dedicated oil-tanker sprite
    let carrierSprite = null; // dedicated aircraft-carrier sprite

    // ── Screen images ───────────────────────────────────────────────────────
    let startScreenImage = null;
    const victoryImages  = [];          // loaded victory images
    let _victoryImageIdx  = 0;          // which image is shown this wave-clear
    let _lastVictoryWave  = -1;         // tracks when to pick a new image

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

            // Start screen background
            startScreenImage = new Image();
            startScreenImage.src = 'images/Startscreen/MainPage.png';

            // Victory images (cycled/randomised between waves)
            ['Victory1', 'Victory2', 'Victory3', 'victory4'].forEach(name => {
                const img = new Image();
                img.src = `images/Victory/${name}.png`;
                victoryImages.push(img);
            });

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

            // Cruiser sprite — 1536×1024, hull y≈450–750, dark ocean bg removed.
            const cruiserImg = new Image();
            cruiserImg.onload = () => { cruiserSprite = _prepareSprite(cruiserImg, 800, true); };
            cruiserImg.src = 'images/cruiser.png';

            // Fishing-boat sprite — 1536×1024, cabin y≈480, hull y≈820–870.
            const fishingImg = new Image();
            fishingImg.onload = () => { fishingSprite = _prepareSprite(fishingImg, 900, true); };
            fishingImg.src = 'images/fishing.png';

            // Oil-tanker sprite — 1536×1024, hull y≈400–700, dark ocean bg removed.
            const tankerImg = new Image();
            tankerImg.onload = () => { tankerSprite = _prepareSprite(tankerImg, 750, true); };
            tankerImg.src = 'images/tanker.png';

            // Aircraft-carrier sprite — 1536×1024, hull y≈750, dark ocean bg removed.
            const carrierImg = new Image();
            carrierImg.onload = () => { carrierSprite = _prepareSprite(carrierImg, 790, true); };
            carrierImg.src = 'images/carrier.png';
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
            // Sort back-to-front so far-lane (small, high Y) ships render under near-lane ships
            [...s.ships].sort((a, b) => a.y - b.y).forEach(ship => this._drawShip(ship));
            s.torpedoes.forEach(t => this._drawTorpedo(t));
            s.explosions.forEach(ex => this._drawExplosion(ex));
            ctx.restore();

            // Fixed-position UI (no shake)
            if (s.floatingTexts) s.floatingTexts.forEach(ft => this._drawFloatingText(ft));
            this._drawTubeSpread(s);
            this._drawPeriscopeVignette();
            this._drawHUD(s);

            if (s.status === 'WaveClear') {
                // Pick a new random victory image once per wave transition
                if (s.wave !== _lastVictoryWave && victoryImages.length > 0) {
                    _lastVictoryWave = s.wave;
                    // Exclude the previous index so we never see the same image twice in a row
                    let next;
                    do { next = Math.floor(Math.random() * victoryImages.length); }
                    while (victoryImages.length > 1 && next === _victoryImageIdx);
                    _victoryImageIdx = next;
                }
                this._drawWaveClear(s.wave, s.lastWaveBonus, s.waveClearTimer, s.lastAccuracy, s.accuracyBonus);
            }
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

            // Horizon position scales proportionally with canvas height
            const scaleY   = canvas.height / 600;
            const horizonY = Math.round(276 * scaleY);

            // Dark water fill below horizon — makes ocean zone distinct
            ctx.fillStyle = 'rgba(0, 20, 55, 0.55)';
            ctx.fillRect(0, horizonY + 2, canvas.width, canvas.height - horizonY - 2);

            // Animated ocean surface waves — 14 rows for dense water texture
            for (let row = 0; row < 14; row++) {
                const y = (horizonY - 4) + row * (22 * scaleY);
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
            ctx.moveTo(0, horizonY);
            ctx.lineTo(canvas.width, horizonY);
            ctx.stroke();
        },

        // ── Ships ────────────────────────────────────────────────────────────

        _drawShip(ship) {
            const sinkProgress = ship.damageState === 'Sinking'
                ? Math.min(ship.sinkTimer / 1.5, 1)
                : 0;
            const depthScale = ship.depthScale ?? 1.0;

            ctx.save();
            ctx.translate(ship.x, ship.y);
            // Mirror ships coming from the right so they always face their direction of travel
            if ((ship.direction ?? 1) === -1) ctx.scale(-1, 1);
            if (sinkProgress > 0) {
                ctx.rotate(sinkProgress * 0.4);
                ctx.globalAlpha = (depthScale < 1.0 ? 0.82 : 1.0) * (1 - sinkProgress * 0.6);
            } else if (depthScale < 1.0) {
                ctx.globalAlpha = 0.82;
            }

            const w = ship.width, h = ship.height;
            const type = ship.type ? ship.type.toLowerCase() : 'destroyer';

            // Cargo → dedicated sprite; Cruiser → dedicated sprite; FishingBoat → dedicated sprite; Tanker → dedicated sprite; others → boatSprite
            const sprite = type === 'cargo'       && cargoSprite   ? cargoSprite   :
                           type === 'cruiser'     && cruiserSprite ? cruiserSprite :
                           type === 'fishingboat' && fishingSprite ? fishingSprite :
                           type === 'tanker'      && tankerSprite  ? tankerSprite  :
                           type === 'carrier'     && carrierSprite ? carrierSprite :
                           boatSprite;

            if (sprite) {
                const drawH = w * (sprite.height / sprite.width);
                ctx.drawImage(sprite, -w / 2, -drawH / 2, w, drawH);

                // All non-cargo, non-fishing, non-tanker types get a colour tint to stay distinct.
                if (type !== 'cargo' && type !== 'fishingboat' && type !== 'tanker' && type !== 'carrier') {
                    const tints = {
                        destroyer: 'rgba(60,100,180,0.28)',
                        ptboat:    'rgba(40,120,40, 0.28)',
                        cruiser:   'rgba(150,70,50, 0.25)',
                    };
                    ctx.globalCompositeOperation = 'source-atop';
                    ctx.fillStyle = tints[type] ?? 'rgba(60,100,180,0.28)';
                    ctx.fillRect(-w / 2, -drawH / 2, w, drawH);
                    ctx.globalCompositeOperation = 'source-over';
                }

                if (ship.damageState === 'Burning') this._drawBurnDamage(w, drawH);

                // Atmospheric distance haze — blue-grey wash for far-lane ships
                if (depthScale < 1.0) {
                    ctx.globalCompositeOperation = 'source-atop';
                    ctx.fillStyle = `rgba(100, 150, 210, ${(1 - depthScale) * 0.45})`;
                    ctx.fillRect(-w / 2, -drawH / 2, w, drawH);
                    ctx.globalCompositeOperation = 'source-over';
                }
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
            const angle = Math.atan2(t.vx ?? 0, -(t.vy ?? -8));
            ctx.save();
            ctx.translate(t.x, t.y);
            ctx.rotate(angle);
            ctx.fillStyle = COLORS.torpedo;
            ctx.shadowColor = COLORS.torpedo;
            ctx.shadowBlur = 6;
            // Nose points in direction of travel (local negative-Y)
            ctx.beginPath();
            ctx.moveTo(0, -t.height / 2);
            ctx.lineTo(-t.width / 2, t.height / 2);
            ctx.lineTo(t.width / 2, t.height / 2);
            ctx.closePath();
            ctx.fill();
            // Bubble trail (behind torpedo)
            ctx.shadowBlur = 0;
            ctx.fillStyle = 'rgba(200,255,255,0.3)';
            for (let i = 1; i <= 4; i++) {
                ctx.beginPath();
                ctx.arc((Math.random() - 0.5) * 4,
                        t.height / 2 + i * 5,
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

        // ── Torpedo tube spread ───────────────────────────────────────────────

        _drawTubeSpread(s) {
            const TUBE_ANGLES_DEG = [-55, -25, 0, 25, 55];
            const LAUNCH_Y = 680;
            const LINE_LEN = 530;
            const aimX    = canvas.width / 2;
            const selected = s.selectedTube ?? 2;

            ctx.save();

            // Draw each tube line
            TUBE_ANGLES_DEG.forEach((angleDeg, i) => {
                const rad  = angleDeg * Math.PI / 180;
                const endX = aimX + Math.sin(rad) * LINE_LEN;
                const endY = LAUNCH_Y - Math.cos(rad) * LINE_LEN;
                const isActive = i === selected;

                ctx.beginPath();
                ctx.moveTo(aimX, LAUNCH_Y);
                ctx.lineTo(endX, endY);
                ctx.strokeStyle  = isActive ? COLORS.crosshair : COLORS.hudDim;
                ctx.lineWidth    = isActive ? 1.5 : 1;
                ctx.globalAlpha  = isActive ? 0.85 : 0.35;
                ctx.shadowColor  = isActive ? COLORS.crosshair : 'transparent';
                ctx.shadowBlur   = isActive ? 5 : 0;
                ctx.setLineDash(isActive ? [] : [6, 5]);
                ctx.stroke();
            });

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

            // Aim indicator — small crosshair where active tube meets ship zone (y ≈ 420)
            const aRad   = TUBE_ANGLES_DEG[selected] * Math.PI / 180;
            const targetY = 420;
            const dist    = (LAUNCH_Y - targetY) / Math.cos(aRad);
            const targetX = aimX + Math.sin(aRad) * dist;
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

        // ── Periscope vignette ────────────────────────────────────────────────

        _drawPeriscopeVignette() {
            const cx = canvas.width / 2, cy = canvas.height / 2;
            const r  = canvas.height / 1.44;
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

            // Background image (cover-fit)
            if (startScreenImage && startScreenImage.complete && startScreenImage.naturalWidth > 0) {
                const imgAr  = startScreenImage.naturalWidth / startScreenImage.naturalHeight;
                const canAr  = canvas.width / canvas.height;
                let sw, sh, sx, sy;
                if (imgAr > canAr) {
                    sh = canvas.height; sw = sh * imgAr;
                    sx = (canvas.width - sw) / 2; sy = 0;
                } else {
                    sw = canvas.width; sh = sw / imgAr;
                    sx = 0; sy = (canvas.height - sh) / 2;
                }
                ctx.drawImage(startScreenImage, sx, sy, sw, sh);
                // Dark overlay so text stays readable
                ctx.fillStyle = 'rgba(0,0,0,0.52)';
                ctx.fillRect(0, 0, canvas.width, canvas.height);
            }

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
            ctx.fillText('Move mouse to aim along a tube line  |  CLICK or SPACE to fire', cx, cy + 20);
            ctx.fillText('← → Arrow keys pan aim  |  P to pause', cx, cy + 40);
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

            // Victory image — centred, letterboxed, max 55% of canvas height
            const vImg = victoryImages[_victoryImageIdx];
            if (vImg && vImg.complete && vImg.naturalWidth > 0) {
                const maxH  = canvas.height * 0.55;
                const maxW  = canvas.width  * 0.75;
                const scale = Math.min(maxW / vImg.naturalWidth, maxH / vImg.naturalHeight);
                const dw = vImg.naturalWidth  * scale;
                const dh = vImg.naturalHeight * scale;
                const dx = cx - dw / 2;
                const dy = cy - dh / 2 - 20;   // shift slightly up to leave room for text below

                // Subtle dark frame behind image
                ctx.fillStyle = 'rgba(0,0,0,0.55)';
                ctx.fillRect(dx - 8, dy - 8, dw + 16, dh + 16);
                ctx.drawImage(vImg, dx, dy, dw, dh);

                // Green border
                ctx.strokeStyle = 'rgba(0,255,0,0.55)';
                ctx.lineWidth = 2;
                ctx.strokeRect(dx - 2, dy - 2, dw + 4, dh + 4);
            }

            // Overlay text below (or over) the image
            const textY = cy + (canvas.height * 0.34);
            ctx.textAlign = 'center';
            ctx.shadowColor = '#00FF00';
            ctx.shadowBlur = 18;
            ctx.fillStyle = '#00FF00';
            ctx.font = 'bold 36px "Courier New", monospace';
            ctx.fillText(`WAVE ${wave} CLEAR!`, cx, textY);
            ctx.font = 'bold 20px "Courier New", monospace';
            ctx.fillStyle = '#FFD700';
            ctx.shadowColor = '#FFD700';
            ctx.fillText(`+${bonus} pts`, cx, textY + 38);
            if (accuracy != null && accuracy > 0) {
                const aBonusStr = accuracyBonus > 0 ? `  +${accuracyBonus} ACCURACY BONUS` : '';
                const aColor = accuracy >= 80 ? '#00FF88' : accuracy >= 60 ? '#FFD700' : '#888';
                ctx.font = '14px "Courier New", monospace';
                ctx.fillStyle = aColor;
                ctx.shadowColor = aColor;
                ctx.shadowBlur = 6;
                ctx.fillText(`ACCURACY ${accuracy}%${aBonusStr}`, cx, textY + 64);
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
