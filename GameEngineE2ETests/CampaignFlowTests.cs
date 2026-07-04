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

            // Satisfy objective immediately via injection, then clear the
            // board — campaign missions are one continuous stage, so this
            // completes the mission directly (no wave-clear interstitial).
            sim.InjectObjectiveSinks();
            sim.Engine.Update(GameSimulator.FrameDtPublic); // stops further spawning
            sim.ForceWaveClear();                           // clears the board → completes
            sim.RunFor(GameState.WaveClearPause + 0.1f);    // harmless extra settle time

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
        sim.Engine.Update(GameSimulator.FrameDtPublic); // stops further spawning
        sim.ForceWaveClear();                           // clears the board → completes
        sim.RunFor(GameState.WaveClearPause + 0.1f);    // harmless extra settle time

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

    // ── Continuous single-stage mission behaviour ────────────────────────────

    [Fact]
    public void Campaign_Mission1_ClearingBoardWithoutObjectiveMet_KeepsPlayingContinuously()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        sim.Engine.AdvanceMissionBriefing();

        // Mission 1's objective is NOT met. Clearing whatever's currently
        // on screen must not pause or end the mission — it should just
        // keep playing (and keep spawning), never dropping into a
        // wave-clear interstitial or otherwise interrupting the stage.
        sim.State.Ships.Clear();
        sim.Engine.Update(GameSimulator.FrameDtPublic);

        sim.AssertStatus(GameStatus.Playing);
        sim.AssertMission(1);
        Assert.False(sim.State.CampaignMissionFailed);
    }

    [Fact]
    public void Campaign_Mission2_WaveTierRampsWithShipsSpawned_CappedAtEndWave()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        sim.Engine.AdvanceMissionBriefing();
        sim.InjectObjectiveSinks();
        sim.Engine.Update(GameSimulator.FrameDtPublic);
        sim.Engine.AdvanceToNextMission(); // → mission 2 briefing

        sim.Engine.AdvanceMissionBriefing();
        sim.AssertWave(2); // mission 2 starts at wave 2

        var mission = CampaignManager.GetMission(2);

        // Isolate the wave-ramp behaviour from the lives/escape mechanic —
        // nothing in this test fires torpedoes, so ships will just escape;
        // unlimited lives keeps that from ending the mission early.
        sim.State.CampaignLives = 9999;

        // Let enough real ships spawn — without ever satisfying the
        // objective — to cross both of mission 2's ramp thresholds
        // (wave 2→3→4). The tier should climb but never exceed EndWave,
        // and the mission should just keep playing: no wave-clear pause,
        // no early completion, no mission change.
        sim.RunFor(mission.ShipsPerWave * 4 * DifficultyManager.GetWave(2).SpawnIntervalSeconds);

        sim.AssertStatus(GameStatus.Playing);
        Assert.Equal(mission.EndWave, sim.State.Wave);
        sim.AssertMission(2);
    }

    // ── Failure paths ─────────────────────────────────────────────────────────

    [Fact]
    public void Campaign_TooManyEscapes_DecrementsLives()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        sim.Engine.AdvanceMissionBriefing();

        int livesBefore = sim.State.CampaignLives;

        // Each escape costs one life immediately (defaults to an
        // objective-type ship — see GameSimulator.InjectEscapes)
        sim.InjectEscapes(1);

        Assert.Equal(livesBefore - 1, sim.State.CampaignLives);
    }

    [Fact]
    public void Campaign_NonObjectiveTypeEscape_DoesNotDecrementLives()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign(); // Mission 1 targets: Destroyer, PtBoat
        sim.Engine.AdvanceMissionBriefing();

        int livesBefore = sim.State.CampaignLives;

        // Tanker isn't a mission-1 objective type — must be free.
        sim.InjectEscapes(1, ShipType.Tanker);

        Assert.Equal(livesBefore, sim.State.CampaignLives);
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
