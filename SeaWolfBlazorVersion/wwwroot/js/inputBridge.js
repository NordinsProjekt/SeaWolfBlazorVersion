window.SeaWolfInput = {
    _dotnet: null,

    init(dotNetRef, canvasId) {
        this._dotnet = dotNetRef;
        const canvas = document.getElementById(canvasId);

        canvas.addEventListener('mousemove', e => {
            const r = canvas.getBoundingClientRect();
            this._dotnet.invokeMethodAsync('OnMouseMove',
                e.clientX - r.left, e.clientY - r.top);
        });

        canvas.addEventListener('click', e => {
            const r = canvas.getBoundingClientRect();
            this._dotnet.invokeMethodAsync('OnClick',
                e.clientX - r.left, e.clientY - r.top);
        });

        document.addEventListener('keydown', e => {
            this._dotnet.invokeMethodAsync('OnKeyDown', e.key);
        });
    }
};
