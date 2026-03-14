using Microsoft.JSInterop;

namespace SeaWolfBlazorVersion.Services;

public class AudioService(IJSRuntime js)
{
    public ValueTask PlayTorpedoAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playTorpedoLaunch");

    public ValueTask PlaySmallExplosionAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playTone", 200, 0.25, "square", 0.4);

    public ValueTask PlayLargeExplosionAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playTone", 80, 0.5, "sawtooth", 0.5);

    public ValueTask PlayBurnAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playTone", 400, 0.15, "triangle", 0.2);

    public ValueTask PlayReloadCompleteAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playTone", 520, 0.12, "sine", 0.22);

    public ValueTask PlayWaveClearAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playChord");

    public ValueTask PlayGameOverAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playGameOver");
}
