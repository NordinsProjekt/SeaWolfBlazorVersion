using Microsoft.JSInterop;

namespace SeaWolfBlazorVersion.Services;

public class AudioService(IJSRuntime js)
{
    public ValueTask PlayTorpedoAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playTone", 800, 0.1, "sine", 0.3);

    public ValueTask PlaySmallExplosionAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playTone", 200, 0.25, "square", 0.4);

    public ValueTask PlayLargeExplosionAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playTone", 80, 0.5, "sawtooth", 0.5);

    public ValueTask PlayBurnAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playTone", 400, 0.15, "triangle", 0.2);

    public ValueTask PlayWaveClearAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playChord");
}
