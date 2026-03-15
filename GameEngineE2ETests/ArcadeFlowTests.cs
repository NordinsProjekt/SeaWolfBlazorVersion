using GameEngine.Engine;
using GameEngine.Engine.Models.Enums;

namespace GameEngineE2ETests;

/// <summary>
/// End-to-end tests that simulate complete game flows without a human player.
/// Each test drives the engine through real state transitions from start to finish.
/// </summary>
public class ArcadeFlowTests
{
    // ── Full arcade game: wave 1 → wave 2 → game over ────────────────────────

    [Fact]
    public void Arcade_Wave1_ClearsAndAdvancesToWave2()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        sim.ForceWaveClear();
        sim.AssertStatus(GameStatus.WaveClear);

        sim.SkipWaveClear();
        sim.AssertStatus(GameStatus.Playing);
        sim.AssertWave(2);
    }

    [Fact]
    public void Arcade_ScoreIncreases_AfterWaveClear()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        int scoreBefore = sim.State.Score;
        sim.ForceWaveClear();
        sim.SkipWaveClear();

        Assert.True(sim.State.Score > scoreBefore);
    }

    [Fact]
    public void Arcade_WaveAdvances_ThroughMultipleWaves()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        for (int wave = 1; wave <= 4; wave++)
        {
            sim.AssertWave(wave);
            sim.ForceWaveClear();
            sim.SkipWaveClear();
        }
        sim.AssertWave(5);
        sim.AssertStatus(GameStatus.Playing);
    }

    [Fact]
    public void Arcade_TooManyEscapes_TriggersGameOver()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        sim.InjectEscapes(GameState.MaxEscaped);
        sim.Engine.Update(GameSimulator.FrameDtPublic);

        sim.AssertStatus(GameStatus.GameOver);
    }

    [Fact]
    public void Arcade_GameOver_ThenReturnToStartScreen()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        sim.InjectEscapes(GameState.MaxEscaped);
        sim.Engine.Update(GameSimulator.FrameDtPublic);
        sim.AssertStatus(GameStatus.GameOver);

        sim.Engine.ReturnToStartScreen();
        sim.AssertStatus(GameStatus.StartScreen);
    }

    [Fact]
    public void Arcade_Torpedo_FireAndReload_Cycle()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        Assert.Equal(2, sim.State.TorpedoCount);

        sim.Engine.FireTorpedoFromTube(2);
        sim.Engine.FireTorpedoFromTube(2);
        Assert.Equal(0, sim.State.TorpedoCount);
        Assert.True(sim.State.IsReloading);

        // Run reload timer to completion
        sim.RunFor(GameState.ReloadDuration + 0.1f);
        Assert.Equal(2, sim.State.TorpedoCount);
        Assert.False(sim.State.IsReloading);
    }

    [Fact]
    public void Arcade_Combo_ResetsOnEscape()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        sim.State.ComboCount = 5;
        sim.InjectEscapes(1);
        sim.Engine.Update(GameSimulator.FrameDtPublic);

        Assert.Equal(0, sim.State.ComboCount);
    }

    [Fact]
    public void Arcade_HighScore_NotDecreased_OnRestart()
    {
        var sim = new GameSimulator();
        sim.StartArcade();
        sim.State.HighScore = 9999;
        sim.State.Score     = 500;

        sim.Engine.StartGame();

        Assert.Equal(9999, sim.State.HighScore);
    }
}
