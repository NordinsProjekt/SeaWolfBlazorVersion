// rendererProjectiles.js — torpedoes, explosions, floating score text
window.SeaWolfRendererProjectiles = (() => {
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
