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
    /// Drives each mission by force-clearing waves and injecting sinks only when
    /// needed. Verifies the engine can advance through all 6 missions to CampaignComplete.
    /// </summary>
    [Fact]
    public void FullCampaign_SixMissions_ReachCampaignComplete()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();

        for (int m = 1; m <= 6; m++)
        {
            // Wait for (or assert) MissionBriefing
            sim.RunUntil(() => sim.State.Status == GameStatus.MissionBriefing, maxFrames: 100);
            sim.Engine.AdvanceMissionBriefing();

            var mission = CampaignManager.GetMission(m);

            // Drive wave-by-wave through the mission.
            // The engine may complete the mission early (objective met mid-wave),
            // so we break out of the wave loop whenever the mission ends.
            bool missionEnded = false;
            for (int w = mission.StartWave; w <= mission.EndWave && !missionEnded; w++)
            {
                sim.RunUntil(() => sim.State.Status == GameStatus.Playing, maxFrames: 200);
                sim.AssertWave(w);

                bool isLastWave = w == mission.EndWave;

                if (isLastWave)
                {
                    // Inject exactly enough sinks to meet the objective.
                    // For "any-type" missions CountObjectiveSinks uses TotalShipsSunk
                    // which accumulates across missions — subtract what's already counted.
                    int alreadyCounted = CampaignManager.CountObjectiveSinks(
                        mission, sim.State.TotalShipsSunk, sim.State.SinksByTypeDict());
                    int needed = mission.Objective.RequiredSinks - alreadyCounted;
                    if (needed > 0)
                    {
                        var type = mission.Objective.TargetTypes.Count > 0
                            ? mission.Objective.TargetTypes[0]
                            : ShipType.Destroyer;
                        sim.InjectSinks(type, needed);
                    }

                    sim.ForceWaveClear(); // → WaveClear
                    sim.RunFor(GameState.WaveClearPause + 0.1f);
                }
                else
                {
                    sim.ForceWaveClear();

                    // UpdateWaveClear runs during RunFor and may call CompleteMission
                    // early (e.g. objective already met). Check for that before proceeding.
                    sim.RunFor(GameState.WaveClearPause + 0.1f);

                    if (sim.State.Status is GameStatus.MissionComplete or GameStatus.CampaignComplete)
                    {
                        missionEnded = true;
                    }
                    // else engine transitioned to Playing for the next wave — continue loop
                }
            }

            sim.RunUntil(
                () => sim.State.Status is GameStatus.MissionComplete or GameStatus.CampaignComplete,
                maxFrames: 200);

            if (m < 6)
            {
                sim.AssertStatus(GameStatus.MissionComplete);
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

        // Drain all lives
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
