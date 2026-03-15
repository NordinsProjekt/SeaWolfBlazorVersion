using GameEngine.Engine;
using GameEngine.Engine.Models.Enums;

namespace GameEngineE2ETests;

/// <summary>
/// End-to-end tests that drive the full 6-mission campaign from start to CampaignComplete.
/// </summary>
public class CampaignFlowTests
{
    // ── Full campaign completion ──────────────────────────────────────────────

    [Fact]
    public void Campaign_AllSixMissions_CanBeCompleted_InSequence()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();

        for (int m = 1; m <= 6; m++)
        {
            sim.AssertMission(m);
            sim.AssertStatus(GameStatus.MissionBriefing);

            sim.Engine.AdvanceMissionBriefing();
            sim.AssertStatus(GameStatus.Playing);

            // Satisfy objective immediately via injection
            sim.InjectObjectiveSinks();
            sim.Engine.Update(GameSimulator.FrameDtPublic); // caps spawning
            sim.ForceWaveClear();                           // → WaveClear
            sim.RunFor(GameState.WaveClearPause + 0.1f);    // → CompleteMission

            if (m < 6)
            {
                sim.AssertStatus(GameStatus.MissionComplete);
                sim.Engine.AdvanceToNextMission();
            }
            else
            {
                sim.AssertStatus(GameStatus.CampaignComplete);
            }
        }
    }

    [Fact]
    public void Campaign_Mission1_ObjectiveMet_SetsMissionComplete()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        sim.Engine.AdvanceMissionBriefing();

        sim.InjectObjectiveSinks();
        sim.Engine.Update(GameSimulator.FrameDtPublic); // caps spawning
        sim.ForceWaveClear();                           // → WaveClear
        sim.RunFor(GameState.WaveClearPause + 0.1f);    // → MissionComplete

        sim.AssertStatus(GameStatus.MissionComplete);
        Assert.False(sim.State.CampaignMissionFailed);
    }

    [Fact]
    public void Campaign_Mission6_ObjectiveMet_SetsCampaignComplete()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();

        // Fast-forward through missions 1–5
        for (int m = 1; m <= 5; m++)
        {
            sim.Engine.AdvanceMissionBriefing();
            sim.InjectObjectiveSinks();
            sim.Engine.Update(GameSimulator.FrameDtPublic);
            sim.ForceWaveClear();
            sim.RunFor(GameState.WaveClearPause + 0.1f);
            sim.Engine.AdvanceToNextMission();
        }

        // Mission 6
        sim.Engine.AdvanceMissionBriefing();
        sim.InjectObjectiveSinks();
        sim.Engine.Update(GameSimulator.FrameDtPublic);
        sim.ForceWaveClear();
        sim.RunFor(GameState.WaveClearPause + 0.1f);

        sim.AssertStatus(GameStatus.CampaignComplete);
    }

    // ── Wave progression inside a mission ────────────────────────────────────

    [Fact]
    public void Campaign_Mission1_Wave1Clear_AdvancesToWave2_NotComplete()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        sim.Engine.AdvanceMissionBriefing();

        // Mission 1: waves 1–2. Clearing wave 1 should not end the mission.
        sim.ForceWaveClear();
        sim.AssertStatus(GameStatus.WaveClear);
        sim.SkipWaveClear();

        sim.AssertStatus(GameStatus.Playing);
        sim.AssertWave(2);
    }

    [Fact]
    public void Campaign_Mission2_SpansThreeWaves_WaveClearAdvances()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        // skip to mission 2
        sim.Engine.AdvanceMissionBriefing();
        sim.InjectObjectiveSinks();
        sim.Engine.Update(GameSimulator.FrameDtPublic);
        sim.Engine.AdvanceToNextMission(); // → mission 2 briefing

        sim.Engine.AdvanceMissionBriefing();
        sim.AssertWave(2); // mission 2 starts at wave 2

        sim.ForceWaveClear();
        sim.SkipWaveClear();
        sim.AssertWave(3);

        sim.ForceWaveClear();
        sim.SkipWaveClear();
        sim.AssertWave(4); // still inside mission 2 (EndWave=4)
    }

    // ── Failure paths ─────────────────────────────────────────────────────────

    [Fact]
    public void Campaign_TooManyEscapes_DecrementsLives()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        sim.Engine.AdvanceMissionBriefing();

        int livesBefore = sim.State.CampaignLives;

        // Each escape costs one life immediately
        sim.InjectEscapes(1);

        Assert.Equal(livesBefore - 1, sim.State.CampaignLives);
    }

    [Fact]
    public void Campaign_OutOfLives_SetsMissionFailed_ThenStartScreen()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        sim.Engine.AdvanceMissionBriefing();

        var mission = CampaignManager.GetMission(1);

        // Drain all lives via escapes (each escape costs 1 life)
        sim.InjectEscapes(mission.Lives);
        sim.Engine.Update(GameSimulator.FrameDtPublic); // detects CampaignLives <= 0

        sim.AssertStatus(GameStatus.MissionComplete);
        Assert.True(sim.State.CampaignMissionFailed);

        sim.Engine.AdvanceToNextMission();
        sim.AssertStatus(GameStatus.StartScreen);
    }

    [Fact]
    public void Campaign_Mission3_CivilianSunk_ImmediateFailure()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        // advance to mission 3
        for (int m = 1; m <= 2; m++)
        {
            sim.Engine.AdvanceMissionBriefing();
            sim.InjectObjectiveSinks();
            sim.Engine.Update(GameSimulator.FrameDtPublic);
            sim.Engine.AdvanceToNextMission();
        }

        sim.Engine.AdvanceMissionBriefing();
        sim.AssertMission(3);

        int livesBefore = sim.State.CampaignLives;
        sim.InjectCivilianSink();
        sim.Engine.Update(GameSimulator.FrameDtPublic);

        // Life deducted (or mission failed immediately if only 1 life)
        Assert.True(sim.State.CampaignLives < livesBefore
                    || sim.State.CampaignMissionFailed);
    }

    [Fact]
    public void Campaign_Mission4_TorpedoBudget_ExhaustsCorrectly()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();

        // Advance to mission 4
        for (int m = 1; m <= 3; m++)
        {
            sim.Engine.AdvanceMissionBriefing();
            sim.InjectObjectiveSinks();
            sim.Engine.Update(GameSimulator.FrameDtPublic);
            sim.Engine.AdvanceToNextMission();
        }

        sim.Engine.AdvanceMissionBriefing();
        sim.AssertMission(4);

        int budget = CampaignManager.GetMission(4).Objective.TorpedoBudget;
        Assert.Equal(budget, sim.State.TorpedoBudgetLeft);

        // Exhaust the entire torpedo budget without running the spawn/collision loop:
        // fire all current torpedoes, then manually skip the reload by resetting
        // ReloadTimer so UpdateReload grants the next batch on one Update tick.
        while (sim.State.TorpedoBudgetLeft > 0 || sim.State.TorpedoCount > 0)
        {
            if (sim.State.TorpedoCount > 0)
            {
                sim.Engine.FireTorpedoFromTube(2);
            }
            else if (sim.State.IsReloading && sim.State.TorpedoBudgetLeft > 0)
            {
                // Skip reload timer — jump straight to completion
                sim.State.ReloadTimer = GameState.ReloadDuration;
                sim.Engine.Update(GameSimulator.FrameDtPublic); // triggers reload grant
            }
            else
                break;
        }

        Assert.Equal(0, sim.State.TorpedoBudgetLeft);
        // Confirm no reload fires after budget is fully exhausted
        sim.State.ReloadTimer = GameState.ReloadDuration;
        sim.Engine.Update(GameSimulator.FrameDtPublic);
        Assert.Equal(0, sim.State.TorpedoCount);
    }

    // ── Lives / retry ─────────────────────────────────────────────────────────

    [Fact]
    public void Campaign_LoseLife_ContinuesPlaying()
    {
        var sim     = new GameSimulator();
        var mission = CampaignManager.GetMission(1);

        sim.Engine.StartCampaign();
        sim.Engine.AdvanceMissionBriefing();

        // One escape costs one life; game continues Playing
        sim.InjectEscapes(1);
        sim.Engine.Update(GameSimulator.FrameDtPublic);

        sim.AssertStatus(GameStatus.Playing);
        Assert.Equal(mission.Lives - 1, sim.State.CampaignLives);
    }

    // ── CampaignComplete → StartScreen ───────────────────────────────────────

    [Fact]
    public void Campaign_CampaignComplete_ReturnToStartScreen()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();

        for (int m = 1; m <= 5; m++)
        {
            sim.Engine.AdvanceMissionBriefing();
            sim.InjectObjectiveSinks();
            sim.Engine.Update(GameSimulator.FrameDtPublic);
            sim.ForceWaveClear();
            sim.RunFor(GameState.WaveClearPause + 0.1f);
            sim.Engine.AdvanceToNextMission();
        }
        sim.Engine.AdvanceMissionBriefing();
        sim.InjectObjectiveSinks();
        sim.Engine.Update(GameSimulator.FrameDtPublic);
        sim.ForceWaveClear();
        sim.RunFor(GameState.WaveClearPause + 0.1f);
        sim.AssertStatus(GameStatus.CampaignComplete);

        sim.Engine.ReturnToStartScreen();
        sim.AssertStatus(GameStatus.StartScreen);
        Assert.Empty(sim.State.Ships);
        Assert.Empty(sim.State.Torpedoes);
    }

    // ── Auto-advance timers ───────────────────────────────────────────────────

    [Fact]
    public void Campaign_MissionComplete_AutoAdvances_After5Seconds()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        sim.Engine.AdvanceMissionBriefing();

        sim.InjectObjectiveSinks();
        sim.Engine.Update(GameSimulator.FrameDtPublic);
        sim.ForceWaveClear();
        sim.RunFor(GameState.WaveClearPause + 0.1f);
        sim.AssertStatus(GameStatus.MissionComplete);

        sim.RunFor(5.1f); // auto-advance fires at 5s

        // Should now be MissionBriefing for mission 2
        sim.AssertStatus(GameStatus.MissionBriefing);
        sim.AssertMission(2);
    }

    [Fact]
    public void Campaign_MissionBriefing_AutoAdvances_After30Seconds()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        sim.AssertStatus(GameStatus.MissionBriefing);

        sim.RunFor(30.1f);

        sim.AssertStatus(GameStatus.Playing);
    }
}
