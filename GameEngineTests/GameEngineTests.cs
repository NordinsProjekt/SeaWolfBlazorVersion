using GameEngine.Engine;
using GameEngine.Engine.Models;
using GameEngine.Engine.Models.Enums;

namespace GameEngineTests;

public class GameEngineTests
{
    // ── StartGame ────────────────────────────────────────────────────────────

    [Fact]
    public void StartGame_SetsStatusToPlaying()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        Assert.Equal(GameStatus.Playing, engine.State.Status);
    }

    [Fact]
    public void StartGame_ResetsScoreAndWave()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.State.Score = 9999;
        engine.State.Wave  = 7;
        engine.StartGame();
        Assert.Equal(0, engine.State.Score);
        Assert.Equal(1, engine.State.Wave);
    }

    [Fact]
    public void StartGame_ClearsAllEntityLists()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.State.Ships.Add(Ship.Create(ShipType.Destroyer, 1.0f));
        engine.State.Torpedoes.Add(new Torpedo());
        engine.StartGame();
        Assert.Empty(engine.State.Ships);
        Assert.Empty(engine.State.Torpedoes);
        Assert.Empty(engine.State.Explosions);
        Assert.Empty(engine.State.FloatingTexts);
    }

    [Fact]
    public void StartGame_RestoresTorpedoCount()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.State.TorpedoCount = 0;
        engine.StartGame();
        Assert.Equal(GameState.MaxTorpedoes, engine.State.TorpedoCount);
        Assert.False(engine.State.IsReloading);
    }

    [Fact]
    public void StartGame_ResetsTotalsAndCombo()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.State.TotalShipsSunk = 10;
        engine.State.ComboCount     = 5;
        engine.StartGame();
        Assert.Equal(0, engine.State.TotalShipsSunk);
        Assert.Equal(0, engine.State.ComboCount);
    }

    // ── FireTorpedoFromTube ───────────────────────────────────────────────────

    [Fact]
    public void FireTorpedoFromTube_WhenPlaying_ReturnsTrueAndAddsTorpedo()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();

        bool result = engine.FireTorpedoFromTube(2);

        Assert.True(result);
        Assert.Single(engine.State.Torpedoes);
    }

    [Fact]
    public void FireTorpedoFromTube_DecrementsTorpedoCount()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();

        engine.FireTorpedoFromTube(2);

        Assert.Equal(GameState.MaxTorpedoes - 1, engine.State.TorpedoCount);
    }

    [Fact]
    public void FireTorpedoFromTube_IncrementsFiredCounters()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();

        engine.FireTorpedoFromTube(2);

        Assert.Equal(1, engine.State.TorpedosFired);
        Assert.Equal(1, engine.State.TotalTorpedosFired);
    }

    [Fact]
    public void FireTorpedoFromTube_LastTorpedo_TriggersReload()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.State.TorpedoCount = 1;

        engine.FireTorpedoFromTube(2);

        Assert.Equal(0, engine.State.TorpedoCount);
        Assert.True(engine.State.IsReloading);
        Assert.Equal(0, engine.State.ReloadTimer);
    }

    [Fact]
    public void FireTorpedoFromTube_WhenNoTorpedoes_ReturnsFalse()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.State.TorpedoCount = 0;

        bool result = engine.FireTorpedoFromTube(2);

        Assert.False(result);
        Assert.Empty(engine.State.Torpedoes);
    }

    [Fact]
    public void FireTorpedoFromTube_WhenReloading_ReturnsFalse()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.State.IsReloading = true;

        bool result = engine.FireTorpedoFromTube(2);

        Assert.False(result);
    }

    [Fact]
    public void FireTorpedoFromTube_NotPlayingStatus_ReturnsFalse()
    {
        var engine = new GameEngine.Engine.GameEngine();
        // Status is StartScreen by default

        bool result = engine.FireTorpedoFromTube(2);

        Assert.False(result);
        Assert.Empty(engine.State.Torpedoes);
    }

    [Theory]
    [InlineData(2, 640f)]   // tube 2 = centre, Vx should be ~0
    [InlineData(0, 640f)]   // tube 0 = far-left
    [InlineData(4, 640f)]   // tube 4 = far-right
    public void FireTorpedoFromTube_LaunchesFromCorrectOrigin(int tube, float expectedX)
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.FireTorpedoFromTube(tube);

        var t = engine.State.Torpedoes[0];
        Assert.Equal(expectedX, t.X);
        Assert.Equal(680f,      t.Y);
    }

    [Fact]
    public void FireTorpedoFromTube_CentreTube_HasNearZeroVx()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.FireTorpedoFromTube(2);

        Assert.Equal(0f, engine.State.Torpedoes[0].Vx, precision: 4);
    }

    [Fact]
    public void FireTorpedoFromTube_LeftTube_HasNegativeVx()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.FireTorpedoFromTube(0);

        Assert.True(engine.State.Torpedoes[0].Vx < 0);
    }

    [Fact]
    public void FireTorpedoFromTube_RightTube_HasPositiveVx()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.FireTorpedoFromTube(4);

        Assert.True(engine.State.Torpedoes[0].Vx > 0);
    }

    // ── ComputeAimedTube ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(0f,    0)]   // far left
    [InlineData(128f,  0)]   // still tube 0
    [InlineData(256f,  1)]
    [InlineData(640f,  2)]   // centre
    [InlineData(896f,  3)]
    [InlineData(1150f, 4)]
    [InlineData(1279f, 4)]   // far right
    public void ComputeAimedTube_ReturnsCorrectZone(float mouseX, int expectedTube)
    {
        var engine = new GameEngine.Engine.GameEngine();
        Assert.Equal(expectedTube, engine.ComputeAimedTube(mouseX, 300f));
    }

    [Theory]
    [InlineData(-10f, 0)]    // clamped to 0
    [InlineData(1290f, 4)]   // clamped to 4
    public void ComputeAimedTube_ClampsOutOfBounds(float mouseX, int expectedTube)
    {
        var engine = new GameEngine.Engine.GameEngine();
        Assert.Equal(expectedTube, engine.ComputeAimedTube(mouseX, 300f));
    }

    // ── TogglePause ───────────────────────────────────────────────────────────

    [Fact]
    public void TogglePause_WhenPlaying_SetsPaused()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();

        engine.TogglePause();

        Assert.Equal(GameStatus.Paused, engine.State.Status);
    }

    [Fact]
    public void TogglePause_WhenPaused_ResumesPlaying()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.TogglePause();

        engine.TogglePause();

        Assert.Equal(GameStatus.Playing, engine.State.Status);
    }

    [Fact]
    public void TogglePause_WhenStartScreen_DoesNothing()
    {
        var engine = new GameEngine.Engine.GameEngine();
        // Status is StartScreen by default
        engine.TogglePause();
        Assert.Equal(GameStatus.StartScreen, engine.State.Status);
    }

    // ── Update — torpedo movement ────────────────────────────────────────────

    [Fact]
    public void Update_Playing_AdvancesTorpedoPosition()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.FireTorpedoFromTube(2);

        float yBefore = engine.State.Torpedoes[0].Y;
        engine.Update(0.016f);

        Assert.True(engine.State.Torpedoes[0].Y < yBefore);
    }

    [Fact]
    public void Update_Playing_RemovesTorpedoWhenOutOfBounds()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.State.Torpedoes.Add(new Torpedo { X = 640, Y = -100 });

        engine.Update(0.016f);

        Assert.Empty(engine.State.Torpedoes);
    }

    // ── Update — reload ───────────────────────────────────────────────────────

    [Fact]
    public void Update_Reload_CompletesAfterReloadDuration()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.State.TorpedoCount = 1;
        engine.FireTorpedoFromTube(2);

        Assert.True(engine.State.IsReloading);
        engine.Update(GameState.ReloadDuration + 0.1f);

        Assert.False(engine.State.IsReloading);
        Assert.Equal(GameState.MaxTorpedoes, engine.State.TorpedoCount);
    }

    // ── Update — combo timer ─────────────────────────────────────────────────

    [Fact]
    public void Update_ComboTimer_DecrementsAndResetsCombo()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.State.ComboCount = 3;
        engine.State.ComboTimer = 0.1f;

        engine.Update(0.2f);

        Assert.Equal(0, engine.State.ComboCount);
    }

    // ── Update — game over on exceeded escapes ────────────────────────────────

    [Fact]
    public void Update_ShipsExceedEscapeLimit_SetsGameOver()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.State.ShipsEscaped = GameState.MaxEscaped;

        engine.Update(0.016f);

        Assert.Equal(GameStatus.GameOver, engine.State.Status);
    }

    // ── Update — wave clear ───────────────────────────────────────────────────

    [Fact]
    public void Update_AllWaveShipsGone_TransitionsToWaveClear()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();

        var wave = DifficultyManager.GetWave(engine.State.Wave);
        engine.State.ShipsSpawnedThisWave = wave.TotalShips;
        // No ships and no torpedoes active → wave clear should trigger
        engine.State.Ships.Clear();

        engine.Update(0.016f);

        Assert.Equal(GameStatus.WaveClear, engine.State.Status);
    }

    [Fact]
    public void Update_WaveClear_AdvancesToNextWaveAfterPause()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();

        var wave = DifficultyManager.GetWave(engine.State.Wave);
        engine.State.ShipsSpawnedThisWave = wave.TotalShips;
        engine.State.Ships.Clear();
        engine.Update(0.016f);

        Assert.Equal(GameStatus.WaveClear, engine.State.Status);

        engine.Update(GameState.WaveClearPause + 0.1f);

        Assert.Equal(GameStatus.Playing, engine.State.Status);
        Assert.Equal(2, engine.State.Wave);
    }

    // ── Update — screen shake decay ──────────────────────────────────────────

    [Fact]
    public void Update_ShakeTimer_Decays()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.State.ShakeTimer = 0.5f;

        engine.Update(0.2f);

        Assert.True(engine.State.ShakeTimer < 0.5f);
        Assert.True(engine.State.ShakeTimer >= 0f);
    }

    // ── Update — not playing ─────────────────────────────────────────────────

    [Fact]
    public void Update_WhenNotPlaying_DoesNotMoveEntities()
    {
        var engine = new GameEngine.Engine.GameEngine();
        // Status is StartScreen — update should be a no-op
        engine.State.Torpedoes.Add(new Torpedo { X = 640, Y = 300, Vy = -8 });

        engine.Update(1.0f);

        Assert.Equal(300f, engine.State.Torpedoes[0].Y);
    }
}
