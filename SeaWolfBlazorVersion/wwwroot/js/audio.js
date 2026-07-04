window.SeaWolfAudio = (() => {
    let ctx = null;
    function getCtx() {
        if (!ctx) ctx = new (window.AudioContext || window.webkitAudioContext)();
        // Autoplay policies leave a freshly-created context "suspended"
        // until a user gesture resumes it. Every call here happens from a
        // click/keypress handler, so this is always a safe, in-gesture resume.
        if (ctx.state === 'suspended') ctx.resume().catch(() => {});
        return ctx;
    }

    // ── Background music (generative — no audio files needed) ───────────────
    // A slow, sparse minor-key bass pulse with an occasional soft arpeggio
    // note layered on top. Deliberately understated: it should sit well
    // under the sound effects, not compete with them.
    const MUSIC_BASS_HZ  = [55.0, 61.74, 55.0, 73.42];   // A1, B1, A1, D2 — slow ominous walk
    const MUSIC_ARP_HZ   = [220.0, 261.63, 293.66, 329.63]; // A3, C4, D4, E4
    const BEAT_MS = 1450;

    const music = {
        playing: false,
        ducked: false,
        step: 0,
        gain: null,
        timer: null
    };

    function _scheduleMusicStep() {
        if (!music.playing) return;
        const ac = getCtx();

        const bassFreq = MUSIC_BASS_HZ[music.step % MUSIC_BASS_HZ.length];
        _playManagedTone(ac, music.gain, bassFreq, 1.1, 'sine', 0.5);

        // Every other beat, layer a quiet, longer arpeggio note on top.
        if (music.step % 2 === 1) {
            const arpFreq = MUSIC_ARP_HZ[(music.step >> 1) % MUSIC_ARP_HZ.length];
            _playManagedTone(ac, music.gain, arpFreq, 1.8, 'triangle', 0.22);
        }

        music.step++;
        music.timer = setTimeout(_scheduleMusicStep, BEAT_MS);
    }

    // Like playTone, but routes through a caller-supplied gain node (so the
    // whole music bed can be ducked/stopped together) instead of straight to
    // destination.
    function _playManagedTone(ac, destGain, frequency, duration, type, gain) {
        try {
            const osc = ac.createOscillator();
            const g = ac.createGain();
            osc.connect(g);
            g.connect(destGain);
            osc.type = type;
            osc.frequency.value = frequency;
            g.gain.setValueAtTime(0.0001, ac.currentTime);
            g.gain.exponentialRampToValueAtTime(gain, ac.currentTime + 0.08);
            g.gain.exponentialRampToValueAtTime(0.0001, ac.currentTime + duration);
            osc.start();
            osc.stop(ac.currentTime + duration + 0.05);
        } catch { /* silent fail */ }
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
        },

        // ── New sound effects ────────────────────────────────────────────────

        // Urgent double-blip klaxon — plays when a ship you needed slips away.
        playEscapeAlarm() {
            [700, 550].forEach((f, i) => {
                setTimeout(() => this.playTone(f, 0.16, 'square', 0.22), i * 130);
            });
        },

        // Soft, short click for menu/button interactions — deliberately
        // understated so it doesn't compete with the meatier gameplay sounds.
        playUIClick() {
            this.playTone(1100, 0.045, 'square', 0.08);
        },

        // Classic sonar "ping" — a clean tone with a couple of quiet, delayed
        // echoes standing in for reverb, used as ambience while Playing.
        playSonarPing() {
            try {
                const ac = getCtx();
                const osc = ac.createOscillator();
                const g = ac.createGain();
                osc.connect(g);
                g.connect(ac.destination);
                osc.type = 'sine';
                osc.frequency.setValueAtTime(1300, ac.currentTime);
                osc.frequency.exponentialRampToValueAtTime(900, ac.currentTime + 0.8);
                g.gain.setValueAtTime(0.001, ac.currentTime);
                g.gain.exponentialRampToValueAtTime(0.09, ac.currentTime + 0.02);
                g.gain.exponentialRampToValueAtTime(0.001, ac.currentTime + 0.9);
                osc.start();
                osc.stop(ac.currentTime + 0.9);
            } catch { /* silent fail */ }
            [0.35, 0.65].forEach((delay, i) => {
                setTimeout(() => this.playTone(950, 0.5, 'sine', 0.03 / (i + 1)), delay * 1000);
            });
        },

        // Triumphant, bright — distinct from the plain wave-clear chord.
        playMissionSuccess() {
            [523, 659, 784, 1047].forEach((f, i) => {
                setTimeout(() => this.playTone(f, 0.32, 'triangle', 0.24), i * 110);
            });
        },

        // Somber descending minor run — the counterpart to playMissionSuccess.
        playMissionFailed() {
            [392, 349, 311, 261].forEach((f, i) => {
                setTimeout(() => this.playTone(f, 0.4, 'sawtooth', 0.18), i * 180);
            });
        },

        // Grander two-layer fanfare for finishing the entire campaign.
        playCampaignComplete() {
            [523, 659, 784, 1047, 1319].forEach((f, i) => {
                setTimeout(() => this.playTone(f, 0.4, 'triangle', 0.22), i * 130);
            });
            [261, 329, 392].forEach((f, i) => {
                setTimeout(() => this.playTone(f, 0.7, 'sine', 0.14), 300 + i * 130);
            });
        },

        // ── Background music controls ────────────────────────────────────────

        // Safe to call as often as you like — only actually starts once per
        // page session (and requires a user gesture to have already
        // happened, same as every other sound here).
        ensureMusicStarted() {
            if (music.playing) return;
            try {
                const ac = getCtx();
                music.gain = ac.createGain();
                music.gain.gain.value = music.ducked ? 0.35 : 1.0;
                music.gain.connect(ac.destination);
                music.playing = true;
                music.step = 0;
                _scheduleMusicStep();
            } catch { /* silent fail */ }
        },

        stopMusic() {
            music.playing = false;
            if (music.timer) { clearTimeout(music.timer); music.timer = null; }
        },

        // Lowers (or restores) the music bed's volume — used while paused so
        // the "PAUSED" moment feels distinct without cutting music entirely.
        setMusicDucked(ducked) {
            music.ducked = ducked;
            if (!music.gain) return;
            try {
                const ac = getCtx();
                music.gain.gain.linearRampToValueAtTime(ducked ? 0.35 : 1.0, ac.currentTime + 0.4);
            } catch { /* silent fail */ }
        }
    };
})();
