window.SeaWolfGameLoop = {
    _rafId: null,
    _dotnet: null,
    _busy: false,
    // Matches GameState's default status, so the loop starts out assuming
    // idle (throttled) until the first real frame reports otherwise.
    _lastStatus: 'StartScreen',
    _lastInvokeTime: 0,

    // Called from canvasRenderer.js after each frame is parsed, so the loop
    // knows whether the game is actually being played right now.
    reportStatus(status) {
        this._lastStatus = status;
    },

    start(dotNetRef) {
        this._dotnet = dotNetRef;
        this._busy = false;
        this._lastStatus = 'StartScreen';
        this._lastInvokeTime = 0;

        // Only menus/timers/static screens use this — actual gameplay
        // physics only run while Playing (see GameEngine.Update), so
        // throttling everything else to ~20Hz doesn't cost any real
        // simulation accuracy, and cuts a large chunk of the JSON-serialize
        // + JS-interop round trip that otherwise runs at full 60fps from the
        // moment the page loads, even while just sitting at the start screen.
        const IDLE_INTERVAL_MS = 50;

        const loop = (now) => {
            if (!this._dotnet) return;
            this._rafId = requestAnimationFrame(loop);
            if (this._busy) return;

            if (this._lastStatus !== 'Playing' && (now - this._lastInvokeTime) < IDLE_INTERVAL_MS) {
                return;
            }
            this._lastInvokeTime = now;

            this._busy = true;
            this._dotnet.invokeMethodAsync('OnFrame')
                .then(() => { this._busy = false; })
                .catch(() => { this._busy = false; });
        };
        this._rafId = requestAnimationFrame(loop);
    },

    stop() {
        if (this._rafId) {
            cancelAnimationFrame(this._rafId);
            this._rafId = null;
        }
        this._dotnet = null;
        this._busy = false;
    }
};
