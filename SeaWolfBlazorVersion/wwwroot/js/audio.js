window.SeaWolfAudio = (() => {
    let ctx = null;
    function getCtx() {
        if (!ctx) ctx = new (window.AudioContext || window.webkitAudioContext)();
        return ctx;
    }

    return {
        playTone(frequency, duration, type, gain) {
            try {
                const ac = getCtx();
                const osc = ac.createOscillator();
                const g = ac.createGain();
                osc.connect(g);
                g.connect(ac.destination);
                osc.frequency.value = frequency;
                osc.type = type;
                g.gain.setValueAtTime(gain, ac.currentTime);
                g.gain.exponentialRampToValueAtTime(0.001, ac.currentTime + duration);
                osc.start();
                osc.stop(ac.currentTime + duration);
            } catch { /* silent fail */ }
        },

        playChord() {
            [523, 659, 784].forEach((f, i) => {
                setTimeout(() => this.playTone(f, 0.3, 'sine', 0.2), i * 120);
            });
        },

        playTorpedoLaunch() {
            try {
                const ac = getCtx();
                const osc = ac.createOscillator();
                const g = ac.createGain();
                osc.connect(g);
                g.connect(ac.destination);
                osc.type = 'sine';
                osc.frequency.setValueAtTime(880, ac.currentTime);
                osc.frequency.exponentialRampToValueAtTime(280, ac.currentTime + 0.35);
                g.gain.setValueAtTime(0.28, ac.currentTime);
                g.gain.exponentialRampToValueAtTime(0.001, ac.currentTime + 0.35);
                osc.start();
                osc.stop(ac.currentTime + 0.35);
            } catch { /* silent fail */ }
        },

        playGameOver() {
            [440, 392, 349, 330, 294, 261].forEach((f, i) => {
                setTimeout(() => this.playTone(f, 0.35, 'sawtooth', 0.15), i * 160);
            });
        }
    };
})();
