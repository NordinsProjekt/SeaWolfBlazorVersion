window.SeaWolfInput = {
    _dotnet: null,

    init(dotNetRef, canvasId) {
        this._dotnet = dotNetRef;
        const canvas = document.getElementById(canvasId);

        function scalePos(e) {
            const r  = canvas.getBoundingClientRect();
            const sx = canvas.width  / r.width;
            const sy = canvas.height / r.height;
            return [(e.clientX - r.left) * sx, (e.clientY - r.top) * sy];
        }

        canvas.addEventListener('mousemove', e => {
            const [x, y] = scalePos(e);
            this._dotnet.invokeMethodAsync('OnMouseMove', x, y);
        });

        canvas.addEventListener('click', e => {
            const [x, y] = scalePos(e);
            this._dotnet.invokeMethodAsync('OnClick', x, y);
        });

        document.addEventListener('keydown', e => {
            if (['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', ' '].includes(e.key))
                e.preventDefault();
            this._dotnet.invokeMethodAsync('OnKeyDown', e.key);
        });
    }
};
