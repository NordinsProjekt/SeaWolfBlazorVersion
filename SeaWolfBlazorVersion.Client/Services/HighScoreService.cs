using System.Text.Json;
using Microsoft.JSInterop;

namespace SeaWolfBlazorVersion.Client.Services;

public record HighScoreEntry(string Name, int Score, string Mode);

public class HighScoreService(IJSRuntime js)
{
    private const string SingleKey = "seaWolfHighScore";
    private const string ListKey   = "seaWolfHighScoreList";
    private const int    MaxEntries = 10;

    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    // ── Single best score (legacy, still used by the engine state) ────────────

    public async Task<int> LoadAsync()
    {
        var raw = await js.InvokeAsync<string?>("localStorage.getItem", SingleKey);
        return int.TryParse(raw, out var v) ? v : 0;
    }

    public async Task SaveAsync(int score)
        => await js.InvokeVoidAsync("localStorage.setItem", SingleKey, score.ToString());

    // ── Top-10 list ───────────────────────────────────────────────────────────

    public async Task<List<HighScoreEntry>> LoadListAsync()
    {
        var raw = await js.InvokeAsync<string?>("localStorage.getItem", ListKey);
        if (string.IsNullOrWhiteSpace(raw)) return [];
        try { return JsonSerializer.Deserialize<List<HighScoreEntry>>(raw, _jsonOpts) ?? []; }
        catch { return []; }
    }

    public async Task AddEntryAsync(string name, int score, string mode)
    {
        var list = await LoadListAsync();
        list.Add(new HighScoreEntry(name.Trim().ToUpperInvariant(), score, mode));
        list = [.. list.OrderByDescending(e => e.Score).Take(MaxEntries)];
        await js.InvokeVoidAsync("localStorage.setItem", ListKey,
            JsonSerializer.Serialize(list, _jsonOpts));

        // Keep legacy single-score in sync
        if (score > await LoadAsync())
            await SaveAsync(score);
    }
}
