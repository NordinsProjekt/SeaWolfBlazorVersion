// rendererShips.js — ship sprites, vector fallbacks, burn damage, fire
// particles, wakes, smoke, and one-shot hit/kill splash bursts.
window.SeaWolfRendererShips = (() => {
    // Per-ship trailing effects, keyed by ship.id so each ship's wake/smoke
    // persists smoothly across frames instead of resetting every draw call.
    const _wakeParticles   = new Map(); // id -> [{x,y,vx,vy,life}]
    const _smokeParticles  = new Map(); // id -> [{x,y,vx,vy,life,size}]
    const _prevDamageState = new Map(); // id -> last seen damageState string

    // One-shot splash bursts triggered by a hit/kill. Flat pool (not keyed by
    // ship id) so a burst keeps animating even after the ship that spawned
    // it sinks and is removed from state.
    let _splashes = [];

    return {
        drawShip(core, ship) {
            const ctx    = core.getCtx();
            const COLORS = core.getColors();

            const sinkProgress = ship.damageState === 'Sinking'
                ? Math.min(ship.sinkTimer / 1.5, 1)
                : 0;
            const depthScale = ship.depthScale ?? 1.0;

            this._trackDamageTransition(core, ship);

            ctx.save();
            ctx.translate(ship.x, ship.y);
            if ((ship.direction ?? 1) === -1) ctx.scale(-1, 1);
            if (sinkProgress > 0) {
                ctx.rotate(sinkProgress * 0.4);
                ctx.globalAlpha = (depthScale < 1.0 ? 0.82 : 1.0) * (1 - sinkProgress * 0.6);
            } else if (depthScale < 1.0) {
                ctx.globalAlpha = 0.82;
            }

            const w = ship.width, h = ship.height;
            const type = ship.type ? ship.type.toLowerCase() : 'destroyer';

            const sprite = core.getSprite(type);

            if (sprite) {
                const drawH = w * (sprite.height / sprite.width);
                ctx.drawImage(sprite, -w / 2, -drawH / 2, w, drawH);

                if (ship.damageState === 'Burning') this._drawBurnDamage(ctx, w, drawH);

                if (depthScale < 1.0) {
                    ctx.globalCompositeOperation = 'source-atop';
                    ctx.fillStyle = `rgba(100, 150, 210, ${(1 - depthScale) * 0.45})`;
                    ctx.fillRect(-w / 2, -drawH / 2, w, drawH);
                    ctx.globalCompositeOperation = 'source-over';
                }
            } else {
                if (type === 'destroyer')   this._drawDestroyer(ctx, COLORS, w, h, ship.damageState);
                else if (type === 'ptboat') this._drawPtBoat(ctx, COLORS, w, h, ship.damageState);
                else if (type === 'cargo')  this._drawCargo(ctx, COLORS, w, h, ship.damageState);
            }

            ctx.restore();

            if (ship.damageState !== 'Sinking') {
                this._updateAndDrawWake(ctx, ship, w, h, depthScale);
            }

            if (ship.damageState === 'Burning') {
                if (ship.fireParticles) this._drawFireParticles(ctx, ship.fireParticles);
                this._updateAndDrawSmoke(ctx, ship, w, h);
            }
        },

        // Detects Healthy→Burning and (Healthy|Burning)→Sinking transitions
        // per ship id and spawns a one-shot splash burst — a bigger one plus
        // a screen flash for the killing blow. Mirrors the same id-keyed
        // transition tracking Game.razor already uses for hit/kill audio.
        _trackDamageTransition(core, ship) {
            const prev = _prevDamageState.get(ship.id);
            if (prev !== undefined && prev !== ship.damageState) {
                if (ship.damageState === 'Burning') {
                    this._spawnSplash(ship.x, ship.y, 9, 1.0);
                } else if (ship.damageState === 'Sinking') {
                    this._spawnSplash(ship.x, ship.y, 16, 1.5);
                    core.triggerImpactFlash();
                }
            }
            _prevDamageState.set(ship.id, ship.damageState);
        },

        _spawnSplash(x, y, count, scale) {
            for (let i = 0; i < count; i++) {
                const angle = Math.random() * Math.PI * 2;
                const speed = (30 + Math.random() * 70) * scale;
                _splashes.push({
                    x, y,
                    vx: Math.cos(angle) * speed,
                    vy: Math.sin(angle) * speed * 0.6 - 40, // biased upward
                    life: 1,
                    size: (2 + Math.random() * 2.5) * scale
                });
            }
        },

        // Called once per frame (not per-ship) from canvasRenderer so bursts
        // keep animating even after their ship has fully sunk and vanished.
        updateAndDrawSplashes(core) {
            const ctx = core.getCtx();
            if (_splashes.length === 0) return;

            ctx.save();
            ctx.globalCompositeOperation = 'lighter';
            _splashes.forEach(p => {
                p.x += p.vx * 0.016;
                p.y += p.vy * 0.016;
                p.vy += 4; // gravity, pulls the spray back down
                p.life -= 0.045;
                if (p.life <= 0) return;
                ctx.globalAlpha = Math.max(0, p.life) * 0.8;
                ctx.fillStyle = 'rgba(210,235,255,0.9)';
                ctx.beginPath();
                ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
                ctx.fill();
            });
            ctx.restore();
            _splashes = _splashes.filter(p => p.life > 0);
        },

        // Drops stale per-ship effect state once a ship leaves State.Ships
        // (sunk-and-removed or escaped) so these maps don't grow forever.
        cleanupStaleShipEffects(activeIds) {
            for (const map of [_wakeParticles, _smokeParticles, _prevDamageState]) {
                for (const id of map.keys()) {
                    if (!activeIds.has(id)) map.delete(id);
                }
            }
        },

        _updateAndDrawWake(ctx, ship, w, h, depthScale) {
            let particles = _wakeParticles.get(ship.id);
            if (!particles) { particles = []; _wakeParticles.set(ship.id, particles); }

            const dir    = ship.direction ?? 1;
            const sternX = ship.x - dir * (w / 2 - 4);
            const sternY = ship.y + h * 0.32;

            // Spawn a fresh foam puff at the stern every frame while moving.
            particles.push({
                x: sternX,
                y: sternY + (Math.random() - 0.5) * h * 0.25,
                vx: -dir * (8 + Math.random() * 6),
                vy: (Math.random() - 0.5) * 4,
                life: 1
            });
            if (particles.length > 24) particles.shift();

            const alphaScale = depthScale < 1.0 ? 0.5 : 1.0;
            ctx.save();
            particles.forEach(p => {
                p.x += p.vx * 0.09;
                p.y += p.vy * 0.09;
                p.life -= 0.028;
                if (p.life <= 0) return;
                ctx.globalAlpha = p.life * 0.4 * alphaScale;
                ctx.fillStyle = 'rgba(200,230,255,0.9)';
                ctx.beginPath();
                ctx.ellipse(p.x, p.y, 3.2 * p.life + 1, 1.4 * p.life + 0.6, 0, 0, Math.PI * 2);
                ctx.fill();
            });
            ctx.restore();
            _wakeParticles.set(ship.id, particles.filter(p => p.life > 0));

            // Small bow splash at the leading edge — a steady accent rather
            // than a trailing particle, since it's a constant push of water
            // rather than something drifting away.
            const bowX = ship.x + dir * (w / 2 + 6);
            ctx.save();
            ctx.globalAlpha = (depthScale < 1.0 ? 0.12 : 0.22);
            ctx.fillStyle = 'rgba(180,225,255,0.85)';
            ctx.beginPath();
            ctx.ellipse(bowX, ship.y + h * 0.25, 10, 3.5, dir * 0.4, 0, Math.PI * 2);
            ctx.fill();
            ctx.restore();
        },

        _updateAndDrawSmoke(ctx, ship, w, h) {
            let particles = _smokeParticles.get(ship.id);
            if (!particles) { particles = []; _smokeParticles.set(ship.id, particles); }

            if (Math.random() < 0.35) {
                particles.push({
                    x: ship.x + (Math.random() - 0.5) * w * 0.4,
                    y: ship.y - h * 0.55,
                    vx: (Math.random() - 0.5) * 8,
                    vy: -(18 + Math.random() * 14),
                    life: 1,
                    size: 5 + Math.random() * 5
                });
            }
            if (particles.length > 40) particles.shift();

            ctx.save();
            particles.forEach(p => {
                p.x += p.vx * 0.016;
                p.y += p.vy * 0.016;
                p.vx *= 1.01;
                p.life -= 0.012;
                if (p.life <= 0) return;
                const grow = 1 + (1 - p.life) * 1.8;
                ctx.globalAlpha = p.life * 0.35;
                ctx.fillStyle = `rgba(${40 + p.life * 30}, ${40 + p.life * 30}, ${40 + p.life * 30}, 1)`;
                ctx.beginPath();
                ctx.arc(p.x, p.y, p.size * grow, 0, Math.PI * 2);
                ctx.fill();
            });
            ctx.restore();
            _smokeParticles.set(ship.id, particles.filter(p => p.life > 0));
        },

        _drawBurnDamage(ctx, w, h) {
            ctx.globalCompositeOperation = 'source-atop';
            ctx.fillStyle = 'rgba(180,60,0,0.25)';
            ctx.fillRect(-w / 2, -h / 2, w, h);
            ctx.globalCompositeOperation = 'source-over';
        },

        // One flat pixel-art block per particle, additively blended so
        // overlapping flames brighten into hotspots instead of stacking into
        // a solid opaque rectangle (the earlier version drew 3 nested
        // squares per particle at ~120 particles/sec, which is dense enough
        // that it read as a flat painted box sitting on the ship rather than
        // individual flickering flame licks).
        _drawFireParticles(ctx, particles) {
            ctx.save();
            ctx.globalCompositeOperation = 'lighter';
            const GRID = 4;

            particles.forEach(p => {
                const alpha = Math.max(0, Math.min(1, p.life));
                if (alpha <= 0) return;

                // Snap to a coarse fixed grid (not tied to particle size) so
                // blocks read as deliberate pixel-art chunks. Block size still
                // varies per-particle for a bit of texture.
                const gx    = Math.round(p.x / GRID) * GRID;
                const gy    = Math.round(p.y / GRID) * GRID;
                const block = Math.max(4, Math.round(p.size / 2) * 2);

                // p.life is each particle's own remaining seconds (spawned at
                // ~0.8-1.2s, not normalised), but since that range is narrow
                // it works fine as a rough "how fresh is this flame" signal:
                // bright core when young, cooling through orange to ember red
                // as it dies out.
                const heat  = Math.min(1, alpha);
                const color = heat > 0.65 ? '#ffe066' : heat > 0.35 ? '#ff7a1a' : '#c2340a';

                ctx.globalAlpha = 0.35 + heat * 0.35;
                ctx.fillStyle = color;
                ctx.fillRect(gx - block / 2, gy - block / 2, block, block);
            });

            ctx.restore();
        },

        _drawDestroyer(ctx, COLORS, w, h, state) {
            const c = COLORS.destroyer;
            const hw = w / 2, hh = h / 2;

            ctx.fillStyle = c.hull;
            ctx.beginPath();
            ctx.moveTo(-hw, hh);
            ctx.lineTo( hw * 0.85, hh);
            ctx.lineTo( hw, 0);
            ctx.lineTo( hw * 0.85, -hh + 8);
            ctx.lineTo(-hw, -hh + 8);
            ctx.closePath();
            ctx.fill();

            ctx.fillStyle = '#4a6080';
            ctx.fillRect(-hw, hh - 5, w, 5);

            ctx.fillStyle = c.super;
            ctx.fillRect(-hw * 0.2, -hh + 8, hw * 0.55, hh - 4);

            ctx.fillStyle = c.accent;
            ctx.beginPath();
            ctx.arc(hw * 0.55, -hh + 14, 6, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillRect(hw * 0.55, -hh + 11, 14, 3);

            ctx.beginPath();
            ctx.arc(-hw * 0.3, -hh + 14, 5, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillRect(-hw * 0.3, -hh + 11, -12, 3);

            ctx.fillStyle = '#a0b0c0';
            ctx.beginPath();
            ctx.arc(-hw * 0.05, -hh + 3, 4, Math.PI, 0);
            ctx.fill();

            ctx.fillStyle = '#1e2a38';
            ctx.fillRect(-hw * 0.12, -hh + 8, 8, 10);
            ctx.fillStyle = 'rgba(255,255,255,0.12)';
            ctx.beginPath();
            ctx.arc(-hw * 0.12 + 4, -hh + 2, 7, 0, Math.PI * 2);
            ctx.fill();

            if (state === 'Burning') this._drawBurnDamage(ctx, w, h);
        },

        _drawPtBoat(ctx, COLORS, w, h, state) {
            const c = COLORS.ptBoat;
            const hw = w / 2, hh = h / 2;

            ctx.fillStyle = c.hull;
            ctx.beginPath();
            ctx.moveTo(-hw, hh);
            ctx.lineTo( hw * 0.9, hh);
            ctx.lineTo( hw, hh * 0.3);
            ctx.lineTo( hw * 0.8, -hh + 6);
            ctx.lineTo(-hw, -hh + 6);
            ctx.closePath();
            ctx.fill();

            ctx.fillStyle = c.accent;
            ctx.fillRect(-hw * 0.8, -hh + 6, w * 0.8, 5);

            ctx.fillStyle = c.super;
            ctx.fillRect(-hw * 0.1, -hh + 2, hw * 0.5, hh - 4);

            ctx.fillStyle = '#556b2f';
            ctx.fillRect(-hw * 0.45, -hh + 8, 22, 4);
            ctx.fillRect(-hw * 0.45, -hh + 14, 22, 4);

            ctx.fillStyle = 'rgba(255,255,255,0.25)';
            ctx.beginPath();
            ctx.ellipse(-hw - 10, hh * 0.5, 15, 4, 0, 0, Math.PI * 2);
            ctx.fill();

            if (state === 'Burning') this._drawBurnDamage(ctx, w, h);
        },

        _drawCargo(ctx, COLORS, w, h, state) {
            const c = COLORS.cargo;
            const hw = w / 2, hh = h / 2;

            ctx.fillStyle = c.hull;
            ctx.beginPath();
            ctx.moveTo(-hw, hh);
            ctx.lineTo( hw * 0.8, hh);
            ctx.lineTo( hw, hh * 0.4);
            ctx.lineTo( hw * 0.7, -hh + 12);
            ctx.lineTo(-hw, -hh + 12);
            ctx.closePath();
            ctx.fill();

            const containerColors = ['#d4a853', '#4a6a8a', '#d4a853', '#4a6a8a', '#d4a853'];
            const cw = 22, ch = 14;
            for (let i = 0; i < 5; i++) {
                ctx.fillStyle = containerColors[i % 2];
                ctx.fillRect(-hw * 0.55 + i * (cw + 2), -hh + 12, cw, ch);
                if (i < 3) {
                    ctx.fillStyle = containerColors[(i + 1) % 2];
                    ctx.fillRect(-hw * 0.55 + i * (cw + 2), -hh + 12 - ch - 1, cw, ch);
                }
            }

            ctx.fillStyle = c.super;
            ctx.fillRect(-hw * 0.9, -hh + 4, hw * 0.35, h - 16);

            ctx.fillStyle = '#2a1a10';
            ctx.fillRect(-hw * 0.75, -hh - 4, 10, 16);
            ctx.fillStyle = 'rgba(150,150,150,0.15)';
            ctx.beginPath();
            ctx.arc(-hw * 0.75 + 5, -hh - 8, 9, 0, Math.PI * 2);
            ctx.fill();

            ctx.strokeStyle = '#8b7355';
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.moveTo(-hw * 0.55, -hh + 12);
            ctx.lineTo(-hw * 0.2, -hh - 6);
            ctx.stroke();

            if (state === 'Burning') this._drawBurnDamage(ctx, w, h);
        }
    };
})();
