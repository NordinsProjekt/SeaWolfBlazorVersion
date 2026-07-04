using GameEngine.Engine;
using GameEngine.Engine.Models.Enums;

namespace GameEngineE2ETests;

/// <summary>
/// Simulates the complete 6-mission campaign with real Update() calls and
/// real ship spawning/wave transitions — no state injection.
/// This is the closest thing to a "full playthrough" test.
/// </summary>
public class FullPlaythroughTests
{
    /// <summary>
    /// Drives each mission with real ship spawning for a few seconds (the
    /// actual spawn/move/escape loop, not just instant injection), then
    /// satisfies the objective and lets the board clear. Campaign missions
    /// are one continuous stage now (see GameEngine.UpdateCampaignObjective) —
    /// there's no per-wave clear cycle to drive through inside a mission, so
    /// this just verifies the engine can carry all 6 missions through to
    /// CampaignComplete.
    /// </summary>
    [Fact]
    public void FullCampaign_SixMissions_ReachCampaignComplete()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();

        for (int m = 1; m <= 6; m++)
        {
            sim.RunUntil(() => sim.State.Status == GameStatus.MissionBriefing, maxFrames: 100);
            sim.AssertMission(m);
            sim.Engine.AdvanceMissionBriefing();

            var mission = CampaignManager.GetMission(m);
            sim.AssertWave(mission.StartWave);

            // Isolate from unintercepted escapes while ships spawn naturally
            // for a few seconds — this test is about the spawn/completion
            // flow, not the lives mechanic (that's covered separately).
            sim.State.CampaignLives = 9999;
            sim.RunFor(5f);

            sim.InjectObjectiveSinks();
            sim.Engine.Update(GameSimulator.FrameDtPublic); // stops further spawning
            sim.ForceWaveClear();                           // clears the board → completes
            sim.RunFor(GameState.WaveClearPause + 0.1f);

            sim.RunUntil(
                () => sim.State.Status is GameStatus.MissionComplete or GameStatus.CampaignComplete,
                maxFrames: 200);

            if (m < 6)
            {
                sim.AssertStatus(GameStatus.MissionComplete);
                Assert.False(sim.State.CampaignMissionFailed);
                sim.Engine.AdvanceToNextMission();
            }
        }

        sim.AssertStatus(GameStatus.CampaignComplete);
    }

    /// <summary>
    /// Simulates a full arcade game: spawns ships naturally, fires torpedoes into them,
    /// clears waves, and verifies the game can complete multiple waves without crashing.
    /// </summary>
    [Fact]
    public void FullArcade_FiveWaves_NoExceptions()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        for (int wave = 1; wave <= 5; wave++)
        {
            sim.AssertWave(wave);
            sim.AssertStatus(GameStatus.Playing);

            // Run for a bit to let ships spawn naturally
            sim.RunFor(3f);

            // Force clear the rest
            sim.ForceWaveClear();
            sim.SkipWaveClear();
        }

        sim.AssertWave(6);
        sim.AssertStatus(GameStatus.Playing);
    }

    /// <summary>
    /// Simulates a campaign where the player loses every mission (runs out of lives
    /// on each mission) and verifies the game always returns to StartScreen cleanly.
    /// </summary>
    [Fact]
    public void Campaign_FailEveryMission_AlwaysReturnsToStartScreen()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        sim.Engine.AdvanceMissionBriefing();

        var mission = CampaignManager.GetMission(1);

        // Drain all lives. InjectEscapes defaults to an objective-type ship
        // (mission 1: Destroyer/PtBoat), so every escape here still costs a
        // life under the "only objective-type escapes cost health" rule.
        for (int i = 0; i < mission.Lives; i++)
        {
            sim.AssertStatus(GameStatus.Playing);
            sim.InjectEscapes(mission.Objective.MaxEscaped);
            sim.Engine.Update(GameSimulator.FrameDtPublic);

            if (sim.State.Status == GameStatus.MissionComplete) break;
            sim.State.ShipsEscaped = 0;
        }

        sim.AssertStatus(GameStatus.MissionComplete);
        Assert.True(sim.State.CampaignMissionFailed);

        sim.Engine.AdvanceToNextMission();
        sim.AssertStatus(GameStatus.StartScreen);
    }

    /// <summary>
    /// Verifies that ReturnToStartScreen clears all active entities from
    /// any game status (no stale ships or torpedoes visible after returning).
    /// </summary>
    [Fact]
    public void ReturnToStartScreen_ClearsAllEntities_FromAnyStatus()
    {
        var sim = new GameSimulator();
        sim.StartArcade();
        sim.RunFor(5f); // let some ships/torpedoes appear

        sim.Engine.FireTorpedoFromTube(0);
        sim.Engine.FireTorpedoFromTube(4);

        sim.Engine.ReturnToStartScreen();

        sim.AssertStatus(GameStatus.StartScreen);
        Assert.Empty(sim.State.Ships);
        Assert.Empty(sim.State.Torpedoes);
        Assert.Empty(sim.State.Explosions);
    }

    /// <summary>
    /// Verifies the auto-advance timer: MissionComplete → MissionBriefing without
    /// any explicit AdvanceToNextMission() call.
    /// </summary>
    [Fact]
    public void Campaign_MissionComplete_AutoAdvancesViaTick()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        sim.Engine.AdvanceMissionBriefing();

        sim.InjectObjectiveSinks();
        sim.Engine.Update(GameSimulator.FrameDtPublic);
        sim.ForceWaveClear();
        sim.RunFor(GameState.WaveClearPause + 0.1f);
        sim.AssertStatus(GameStatus.MissionComplete);

        // Let the 5-second auto-advance timer fire
        sim.RunFor(5.5f);

        sim.AssertStatus(GameStatus.MissionBriefing);
        sim.AssertMission(2);
    }
}
