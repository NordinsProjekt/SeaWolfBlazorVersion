// rendererShips.js — ship sprites, vector fallbacks, burn damage, fire particles, bow wake
window.SeaWolfRendererShips = (() => {
    return {
        drawShip(core, ship) {
            const ctx    = core.getCtx();
            const COLORS = core.getColors();

            const sinkProgress = ship.damageState === 'Sinking'
                ? Math.min(ship.sinkTimer / 1.5, 1)
                : 0;
            const depthScale = ship.depthScale ?? 1.0;

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

            // Bow wave + V-wake (world space)
            if (ship.damageState !== 'Sinking') {
                const bowX  = ship.x + (ship.direction ?? 1) * (w / 2 + 6);
                const wakeAlpha = depthScale < 1.0 ? 0.12 : 0.22;
                ctx.save();
                ctx.globalAlpha = wakeAlpha;
                ctx.fillStyle = 'rgba(180,225,255,0.85)';
                ctx.beginPath();
                ctx.ellipse(bowX, ship.y + h * 0.25, 10, 3.5, (ship.direction ?? 1) * 0.4, 0, Math.PI * 2);
                ctx.fill();
                const sternX = ship.x - (ship.direction ?? 1) * (w / 2);
                ctx.strokeStyle = 'rgba(150,200,240,0.7)';
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                ctx.moveTo(sternX, ship.y + h * 0.12);
                ctx.lineTo(sternX - (ship.direction ?? 1) * 26, ship.y - h * 0.18);
                ctx.stroke();
                ctx.beginPath();
                ctx.moveTo(sternX, ship.y + h * 0.36);
                ctx.lineTo(sternX - (ship.direction ?? 1) * 26, ship.y + h * 0.56);
                ctx.stroke();
                ctx.restore();
            }

            if (ship.damageState === 'Burning' && ship.fireParticles)
                this._drawFireParticles(ctx, ship.fireParticles);
        },

        _drawBurnDamage(ctx, w, h) {
            ctx.globalCompositeOperation = 'source-atop';
            ctx.fillStyle = 'rgba(180,60,0,0.25)';
            ctx.fillRect(-w / 2, -h / 2, w, h);
            ctx.globalCompositeOperation = 'source-over';
        },

        _drawFireParticles(ctx, particles) {
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
