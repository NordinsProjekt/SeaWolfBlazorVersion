// rendererProjectiles.js — torpedoes, explosions, floating score text
window.SeaWolfRendererProjectiles = (() => {
    // Flat pool of trailing bubble particles for all torpedoes combined.
    // Not keyed by torpedo id (torpedoes don't have one) — bubbles are
    // visually interchangeable, so we just seed new ones at each active
    // torpedo's tail every frame and let them age independently. That also
    // means a torpedo's last few bubbles keep drifting/fading for a moment
    // even after it hits something and disappears from state.
    let _bubbles = [];

    return {
        drawTorpedo(core, t) {
            const ctx    = core.getCtx();
            const COLORS = core.getColors();

            const angle = Math.atan2(t.vx ?? 0, -(t.vy ?? -8));
            ctx.save();
            ctx.translate(t.x, t.y);
            ctx.rotate(angle);
            ctx.fillStyle  = COLORS.torpedo;
            ctx.shadowColor = COLORS.torpedo;
            ctx.shadowBlur = 6;
            ctx.beginPath();
            ctx.moveTo(0, -t.height / 2);
            ctx.lineTo(-t.width / 2, t.height / 2);
            ctx.lineTo(t.width / 2, t.height / 2);
            ctx.closePath();
            ctx.fill();
            // Glowing wake trail
            for (let i = 1; i <= 8; i++) {
                const pct = 1 - i / 9;
                ctx.fillStyle   = `rgba(80,210,255,${(0.38 * pct).toFixed(2)})`;
                ctx.shadowColor = 'rgba(80,200,255,0.4)';
                ctx.shadowBlur  = 5 * pct;
                ctx.beginPath();
                ctx.arc((Math.random() - 0.5) * (2 + i * 0.6),
                         t.height / 2 + i * 6,
                         2.8 * pct, 0, Math.PI * 2);
                ctx.fill();
            }
            ctx.shadowBlur = 0;
            ctx.restore();

            // Seed 1-2 persisting bubbles at the tail for updateAndDrawBubbles
            // to animate — a bit of extra depth behind the procedural wake above.
            const speed = Math.hypot(t.vx ?? 0, t.vy ?? 0) || 1;
            const backX = t.x - (t.vx ?? 0) / speed * (t.height / 2);
            const backY = t.y - (t.vy ?? 0) / speed * (t.height / 2);
            _bubbles.push({
                x: backX + (Math.random() - 0.5) * 3,
                y: backY + (Math.random() - 0.5) * 3,
                vx: -(t.vx ?? 0) * 0.06 + (Math.random() - 0.5) * 6,
                vy: -(t.vy ?? 0) * 0.06 + (Math.random() - 0.5) * 6,
                life: 1,
                size: 1.2 + Math.random() * 1.6
            });
        },

        // Called once per frame from canvasRenderer (not per-torpedo) so the
        // pool ages/draws/culls exactly once regardless of how many
        // torpedoes are currently active.
        updateAndDrawBubbles(core) {
            const ctx = core.getCtx();
            if (_bubbles.length === 0) return;

            ctx.save();
            _bubbles.forEach(b => {
                b.x += b.vx * 0.016;
                b.y += b.vy * 0.016;
                b.life -= 0.06;
                if (b.life <= 0) return;
                ctx.globalAlpha = b.life * 0.5;
                ctx.fillStyle = 'rgba(190,235,255,0.9)';
                ctx.beginPath();
                ctx.arc(b.x, b.y, b.size, 0, Math.PI * 2);
                ctx.fill();
            });
            ctx.restore();
            if (_bubbles.length > 400) _bubbles = _bubbles.slice(-400); // hard safety cap
            _bubbles = _bubbles.filter(b => b.life > 0);
        },

        drawExplosion(core, ex) {
            const ctx = core.getCtx();

            ctx.save();
            const r = ex.radius;
            ctx.globalAlpha = ex.opacity * 0.6;
            ctx.fillStyle = '#FF4400';
            ctx.beginPath();
            ctx.arc(ex.x, ex.y, r, 0, Math.PI * 2);
            ctx.fill();

            ctx.fillStyle = '#FF8800';
            ctx.beginPath();
            ctx.arc(ex.x, ex.y, r * 0.7, 0, Math.PI * 2);
            ctx.fill();

            ctx.fillStyle = '#FFFF88';
            ctx.beginPath();
            ctx.arc(ex.x, ex.y, r * 0.45, 0, Math.PI * 2);
            ctx.fill();

            if (ex.maxRadius >= 40) {
                const swR     = r * 1.65;
                const swAlpha = ex.opacity * Math.max(0, 1 - swR / (ex.maxRadius * 1.65));
                ctx.globalAlpha = swAlpha * 0.75;
                ctx.strokeStyle = 'rgba(255,180,60,0.85)';
                ctx.lineWidth   = 3;
                ctx.shadowColor = '#FF8800';
                ctx.shadowBlur  = 14;
                ctx.beginPath();
                ctx.arc(ex.x, ex.y, swR, 0, Math.PI * 2);
                ctx.stroke();
                ctx.shadowBlur = 0;
            }

            ctx.globalAlpha = 1;
            ex.sparks.forEach(spark => {
                ctx.fillStyle = `rgba(255, 200, 50, ${Math.max(0, spark.life)})`;
                ctx.fillRect(spark.x - 2, spark.y - 2, 4, 4);
            });
            ctx.restore();
        },

        drawFloatingText(core, ft) {
            const ctx = core.getCtx();

            const alpha = Math.max(0, ft.life / ft.maxLife);
            ctx.save();
            ctx.globalAlpha = alpha;
            ctx.textAlign   = 'center';
            ctx.font        = 'bold 17px "Courier New", monospace';
            ctx.fillStyle   = ft.color ?? '#FFD700';
            ctx.shadowColor = ft.color ?? '#FFD700';
            ctx.shadowBlur  = 10;
            ctx.fillText(ft.text, ft.x, ft.y);
            ctx.restore();
        }
    };
})();
