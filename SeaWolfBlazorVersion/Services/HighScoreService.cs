using Microsoft.JSInterop;

namespace SeaWolfBlazorVersion.Services;

public class HighScoreService(IJSRuntime js)
{
    public async Task<int> LoadAsync()
    {
        var raw = await js.InvokeAsync<string?>("localStorage.getItem", "seaWolfHighScore");
        return int.TryParse(raw, out var v) ? v : 0;
    }

    public async Task SaveAsync(int score)
        => await js.InvokeVoidAsync("localStorage.setItem", "seaWolfHighScore", score.ToString());
}
