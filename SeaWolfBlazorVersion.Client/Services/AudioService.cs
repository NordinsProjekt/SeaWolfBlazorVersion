using Microsoft.JSInterop;

namespace SeaWolfBlazorVersion.Client.Services;

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

    // ── New sound effects ────────────────────────────────────────────────────

    public ValueTask PlayEscapeAlarmAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playEscapeAlarm");

    public ValueTask PlayUIClickAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playUIClick");

    public ValueTask PlaySonarPingAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playSonarPing");

    public ValueTask PlayMissionSuccessAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playMissionSuccess");

    public ValueTask PlayMissionFailedAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playMissionFailed");

    public ValueTask PlayCampaignCompleteAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.playCampaignComplete");

    // ── Background music ─────────────────────────────────────────────────────

    public ValueTask EnsureMusicStartedAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.ensureMusicStarted");

    public ValueTask StopMusicAsync()
        => js.InvokeVoidAsync("SeaWolfAudio.stopMusic");

    public ValueTask SetMusicDuckedAsync(bool ducked)
        => js.InvokeVoidAsync("SeaWolfAudio.setMusicDucked", ducked);
}
