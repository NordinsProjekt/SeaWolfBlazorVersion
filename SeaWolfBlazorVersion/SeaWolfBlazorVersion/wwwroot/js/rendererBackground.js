// rendererBackground.js — ocean, sky, stars, moon, waves, depth bands
window.SeaWolfRendererBackground = (() => {
    return {
        draw(core) {
            const canvas = core.getCanvas();
            const ctx    = core.getCtx();

            // Sky / deep ocean gradient
            const grad = ctx.createLinearGradient(0, 0, 0, canvas.height);
            grad.addColorStop(0,    '#000a14');
            grad.addColorStop(0.35, '#001428');
            grad.addColorStop(0.5,  '#001e3c');
            grad.addColorStop(1,    '#003060');
            ctx.fillStyle = grad;
            ctx.fillRect(0, 0, canvas.width, canvas.height);

            const scaleY   = canvas.height / 600;
            const horizonY = Math.round(276 * scaleY);

            // Moon
            const moonX = canvas.width * 0.82, moonY = horizonY * 0.28;
            const moonG = ctx.createRadialGradient(moonX, moonY, 0, moonX, moonY, 30);
            moonG.addColorStop(0,   'rgba(255,255,210,0.95)');
            moonG.addColorStop(0.55,'rgba(220,220,170,0.55)');
            moonG.addColorStop(1,   'rgba(200,200,150,0)');
            ctx.fillStyle = moonG;
            ctx.beginPath();
            ctx.arc(moonX, moonY, 30, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillStyle = 'rgba(255,255,190,0.055)';
            ctx.fillRect(moonX - 12, horizonY, 24, canvas.height - horizonY);

            // Starfield (generated once, reused every frame)
            let stars = core.getStars();
            if (!stars) {
                stars = [];
                for (let i = 0; i < 90; i++) {
                    stars.push({
                        x: Math.random() * canvas.width,
                        y: Math.random() * (horizonY - 14),
                        r: 0.4 + Math.random() * 1.3,
                        base: 0.28 + Math.random() * 0.62,
                        phase: Math.random() * Math.PI * 2
                    });
                }
                core.setStars(stars);
            }
            const now = Date.now();
            stars.forEach(star => {
                const a = star.base * (0.75 + 0.25 * Math.sin(now * 0.0008 + star.phase));
                ctx.fillStyle = `rgba(255,255,255,${a.toFixed(2)})`;
                ctx.beginPath();
                ctx.arc(star.x, star.y, star.r, 0, Math.PI * 2);
                ctx.fill();
            });

            // Dark water fill below horizon
            ctx.fillStyle = 'rgba(0, 20, 55, 0.55)';
            ctx.fillRect(0, horizonY + 2, canvas.width, canvas.height - horizonY - 2);

            // Animated ocean surface waves
            const waveOffset = core.getWaveOffset();
            for (let row = 0; row < 14; row++) {
                const y = (horizonY - 4) + row * (22 * scaleY);
                const alpha = 0.15 + (row / 14) * 0.45;
                const blue  = Math.max(60, 160 - row * 8);
                ctx.strokeStyle = `rgba(0,${blue},200,${alpha})`;
                ctx.lineWidth   = row < 2 ? 1.8 : 1;
                ctx.beginPath();
                for (let x = 0; x <= canvas.width; x += 4) {
                    const amp  = 3 + row * 0.4;
                    const freq = 0.022 - row * 0.001;
                    const dy   = Math.sin((x * freq) + waveOffset * 0.05 + row * 0.8) * amp;
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

            // Depth-zone reference bands
            ctx.setLineDash([6, 10]);
            ctx.lineWidth = 1;
            ctx.strokeStyle = 'rgba(0,160,200,0.10)';
            ctx.beginPath();
            ctx.moveTo(0, 355); ctx.lineTo(canvas.width, 355);
            ctx.stroke();
            ctx.strokeStyle = 'rgba(0,120,160,0.10)';
            ctx.beginPath();
            ctx.moveTo(0, 475); ctx.lineTo(canvas.width, 475);
            ctx.stroke();
            ctx.setLineDash([]);
        }
    };
})();
