window.SeaWolfGameLoop = {
    _rafId: null,
    _dotnet: null,
    _busy: false,

    start(dotNetRef) {
        this._dotnet = dotNetRef;
        this._busy = false;
        const loop = () => {
            if (!this._dotnet) return;
            this._rafId = requestAnimationFrame(loop);
            if (this._busy) return;
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
