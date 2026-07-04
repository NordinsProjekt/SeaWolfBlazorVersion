using System.Text.Json;
using Bunit;
using GameEngine.Engine;
using GameEngine.Engine.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using SeaWolfBlazorVersion.Client.Pages;
using SeaWolfBlazorVersion.Client.Services;

namespace SeaWolfBlazorTests;

/// <summary>
/// bUnit UI-tester för Game.razor.
/// JS-anrop mockas via bUnits inbyggda JSInterop (Loose-läge).
/// </summary>
public sealed class GameComponentTests : TestContext
{
    public GameComponentTests()
    {
        // Alla omockade JS-anrop returnerar default utan att kasta undantag.
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Registrera tjänster som komponenten injekterar.
        // bUnit injekterar sin mock-IJSRuntime när de löses upp.
        Services.AddScoped<HighScoreService>();
        Services.AddScoped<AudioService>();
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    [Fact]
    public void Game_RendersCanvasWithCorrectDimensions()
    {
        var cut = RenderComponent<Game>();

        var canvas = cut.Find("#gameCanvas");
        Assert.Equal("1280", canvas.GetAttribute("width"));
        Assert.Equal("720",  canvas.GetAttribute("height"));
    }

    [Fact]
    public void Game_RendersGameContainer()
    {
        var cut = RenderComponent<Game>();

        cut.Find("#game-container");
    }

    // ── Tangentbord: starta campaign ──────────────────────────────────────────

    [Fact]
    public async Task OnKeyDown_c_AtStartScreen_StartsCampaign()
    {
        var cut = RenderComponent<Game>();

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("c"));

        Assert.Equal(GameMode.Campaign,          cut.Instance.Engine.State.Mode);
        Assert.Equal(GameStatus.MissionBriefing, cut.Instance.Engine.State.Status);
    }

    [Fact]
    public async Task OnKeyDown_C_AtStartScreen_StartsCampaign()
    {
        var cut = RenderComponent<Game>();

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("C"));

        Assert.Equal(GameMode.Campaign,          cut.Instance.Engine.State.Mode);
        Assert.Equal(GameStatus.MissionBriefing, cut.Instance.Engine.State.Status);
    }

    [Fact]
    public async Task OnKeyDown_c_NotAtStartScreen_DoesNotStartCampaign()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartGame(); // Status = Playing (Arcade)

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("c"));

        // Ska fortfarande vara Playing, inte MissionBriefing
        Assert.Equal(GameStatus.Playing, cut.Instance.Engine.State.Status);
    }

    // ── Tangentbord: paus ─────────────────────────────────────────────────────

    [Fact]
    public async Task OnKeyDown_p_WhenPlaying_PausesGame()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartGame();

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("p"));

        Assert.Equal(GameStatus.Paused, cut.Instance.Engine.State.Status);
    }

    [Fact]
    public async Task OnKeyDown_p_WhenPaused_ResumesGame()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartGame();
        cut.Instance.Engine.TogglePause(); // → Paused

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("p"));

        Assert.Equal(GameStatus.Playing, cut.Instance.Engine.State.Status);
    }

    // ── Tangentbord: MissionBriefing ──────────────────────────────────────────

    [Fact]
    public async Task OnKeyDown_Space_WhenMissionBriefing_AdvancesToPlaying()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartCampaign(); // → MissionBriefing

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown(" "));

        Assert.Equal(GameStatus.Playing, cut.Instance.Engine.State.Status);
    }

    [Fact]
    public async Task OnKeyDown_Enter_WhenMissionBriefing_AdvancesToPlaying()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartCampaign();

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("Enter"));

        Assert.Equal(GameStatus.Playing, cut.Instance.Engine.State.Status);
    }

    // ── Tangentbord: tubval ───────────────────────────────────────────────────

    [Fact]
    public async Task OnKeyDown_ArrowRight_WhenPlaying_IncrementsSelectedTube()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartGame();
        cut.Instance.Engine.State.SelectedTube = 2;

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("ArrowRight"));

        Assert.Equal(3, cut.Instance.Engine.State.SelectedTube);
    }

    [Fact]
    public async Task OnKeyDown_ArrowLeft_WhenPlaying_DecrementsSelectedTube()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartGame();
        cut.Instance.Engine.State.SelectedTube = 2;

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("ArrowLeft"));

        Assert.Equal(1, cut.Instance.Engine.State.SelectedTube);
    }

    [Fact]
    public async Task OnKeyDown_ArrowRight_AtMaxTube_StaysAtMax()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartGame();
        cut.Instance.Engine.State.SelectedTube = TorpedoTubes.Count - 1;

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("ArrowRight"));

        Assert.Equal(TorpedoTubes.Count - 1, cut.Instance.Engine.State.SelectedTube);
    }

    [Fact]
    public async Task OnKeyDown_ArrowLeft_AtMinTube_StaysAtMin()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartGame();
        cut.Instance.Engine.State.SelectedTube = 0;

        await cut.InvokeAsync(() => cut.Instance.OnKeyDown("ArrowLeft"));

        Assert.Equal(0, cut.Instance.Engine.State.SelectedTube);
    }

    // ── Musklick: startskärmens knappar ───────────────────────────────────────

    [Fact]
    public async Task OnClick_CampaignButton_AtStartScreen_StartsCampaign()
    {
        // Returnera knappositioner där (100,100) träffar "campaign"
        JSInterop.Setup<JsonElement>("SeaWolfHighScore.getStartScreenButtons")
            .SetResult(JsonSerializer.Deserialize<JsonElement>(
                "{\"campaign\":{\"x\":0,\"y\":0,\"w\":1280,\"h\":720}," +
                "\"arcade\":{\"x\":9999,\"y\":9999,\"w\":1,\"h\":1}," +
                "\"highScores\":{\"x\":9999,\"y\":9999,\"w\":1,\"h\":1}}"));

        var cut = RenderComponent<Game>();

        await cut.InvokeAsync(() => cut.Instance.OnClick(100, 100));

        Assert.Equal(GameMode.Campaign,          cut.Instance.Engine.State.Mode);
        Assert.Equal(GameStatus.MissionBriefing, cut.Instance.Engine.State.Status);
    }

    [Fact]
    public async Task OnClick_ArcadeButton_AtStartScreen_StartsArcade()
    {
        JSInterop.Setup<JsonElement>("SeaWolfHighScore.getStartScreenButtons")
            .SetResult(JsonSerializer.Deserialize<JsonElement>(
                "{\"arcade\":{\"x\":0,\"y\":0,\"w\":1280,\"h\":720}," +
                "\"campaign\":{\"x\":9999,\"y\":9999,\"w\":1,\"h\":1}," +
                "\"highScores\":{\"x\":9999,\"y\":9999,\"w\":1,\"h\":1}}"));

        var cut = RenderComponent<Game>();

        await cut.InvokeAsync(() => cut.Instance.OnClick(100, 100));

        Assert.Equal(GameMode.Arcade,    cut.Instance.Engine.State.Mode);
        Assert.Equal(GameStatus.Playing, cut.Instance.Engine.State.Status);
    }

    [Fact]
    public async Task OnClick_OutsideAllButtons_AtStartScreen_DoesNothing()
    {
        // Alla knappar utanför klickkoordinaten → ingen statusändring
        JSInterop.Setup<JsonElement>("SeaWolfHighScore.getStartScreenButtons")
            .SetResult(JsonSerializer.Deserialize<JsonElement>(
                "{\"arcade\":{\"x\":9999,\"y\":9999,\"w\":1,\"h\":1}," +
                "\"campaign\":{\"x\":9999,\"y\":9999,\"w\":1,\"h\":1}," +
                "\"highScores\":{\"x\":9999,\"y\":9999,\"w\":1,\"h\":1}}"));

        var cut = RenderComponent<Game>();

        await cut.InvokeAsync(() => cut.Instance.OnClick(100, 100));

        Assert.Equal(GameStatus.StartScreen, cut.Instance.Engine.State.Status);
    }

    // ── Musklick: campaign-skärmar ────────────────────────────────────────────

    [Fact]
    public async Task OnClick_WhenMissionBriefing_AdvancesToPlaying()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartCampaign(); // → MissionBriefing

        await cut.InvokeAsync(() => cut.Instance.OnClick(0, 0));

        Assert.Equal(GameStatus.Playing, cut.Instance.Engine.State.Status);
    }

    [Fact]
    public async Task OnClick_WhenMissionComplete_AdvancesToNextMissionBriefing()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartCampaign();          // Mission 1, MissionBriefing
        cut.Instance.Engine.AdvanceMissionBriefing(); // → Playing
        cut.Instance.Engine.State.Status = GameStatus.MissionComplete;

        await cut.InvokeAsync(() => cut.Instance.OnClick(0, 0));

        Assert.Equal(GameStatus.MissionBriefing, cut.Instance.Engine.State.Status);
        Assert.Equal(2, cut.Instance.Engine.State.CampaignMission);
    }

    [Fact]
    public async Task OnClick_WhenCampaignComplete_AfterLastMission_ReturnsToStartScreen()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartCampaign();
        cut.Instance.Engine.State.CampaignMission = 6; // sista uppdraget
        cut.Instance.Engine.State.Status = GameStatus.CampaignComplete;

        await cut.InvokeAsync(() => cut.Instance.OnClick(0, 0));

        Assert.Equal(GameStatus.StartScreen, cut.Instance.Engine.State.Status);
    }

    [Fact]
    public async Task OnClick_WhenMissionBriefing_SetsMission1Wave()
    {
        var cut = RenderComponent<Game>();
        cut.Instance.Engine.StartCampaign();

        await cut.InvokeAsync(() => cut.Instance.OnClick(0, 0));

        // Ska ha startvågen för uppdrag 1
        Assert.Equal(CampaignManager.GetMission(1).StartWave,
            cut.Instance.Engine.State.Wave);
    }
}
