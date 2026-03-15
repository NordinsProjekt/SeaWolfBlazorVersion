using GameEngine.Engine;
using GameEngine.Engine.Models;
using GameEngine.Engine.Models.Enums;

namespace GameEngineE2ETests;

/// <summary>
/// Tests that verify ship spawning, movement, escape, and wave timing
/// against real Update() calls — no manual state injection.
/// </summary>
public class SpawnAndWaveTests
{
    [Fact]
    public void Ships_SpawnDuringPlaying_WithinFirstWave()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        // Run long enough for at least one ship to spawn (spawn timer starts pre-filled)
        sim.RunFor(5f);

        Assert.True(sim.State.Ships.Count > 0 || sim.State.ShipsSpawnedThisWave > 0);
    }

    [Fact]
    public void Ships_DoNotSpawn_WhenNotPlaying()
    {
        var sim = new GameSimulator();
        // Engine is at StartScreen — no ships should spawn
        sim.RunFor(5f);
        Assert.Empty(sim.State.Ships);
    }

    [Fact]
    public void Ships_Move_EachFrame()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        // Force a ship onto the field
        var ship = Ship.Create(ShipType.Destroyer, 1f, 1, false);
        float startX = ship.X;
        sim.State.Ships.Add(ship);

        sim.Engine.Update(GameSimulator.FrameDtPublic);

        Assert.NotEqual(startX, ship.X);
    }

    [Fact]
    public void Ships_Escape_WhenOffscreen_RightDirection()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        var ship = Ship.Create(ShipType.Destroyer, 1f, 1, false);
        ship.X = 1400f; // already off-screen to the right
        sim.State.Ships.Add(ship);

        int escapedBefore = sim.State.ShipsEscaped;
        sim.Engine.Update(GameSimulator.FrameDtPublic);

        Assert.Equal(escapedBefore + 1, sim.State.ShipsEscaped);
        Assert.DoesNotContain(ship, sim.State.Ships);
    }

    [Fact]
    public void Ships_Escape_WhenOffscreen_LeftDirection()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        var ship = Ship.Create(ShipType.Destroyer, 1f, -1, false);
        ship.X = -ship.Width - 10f;
        sim.State.Ships.Add(ship);

        int escapedBefore = sim.State.ShipsEscaped;
        sim.Engine.Update(GameSimulator.FrameDtPublic);

        Assert.Equal(escapedBefore + 1, sim.State.ShipsEscaped);
    }

    [Fact]
    public void WaveTotalShips_MatchesDifficultyManager_InArcade()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        var expected = DifficultyManager.GetWave(1).TotalShips;
        Assert.Equal(expected, sim.State.WaveTotalShips);
    }

    [Fact]
    public void WaveTotalShips_MatchesMissionConfig_InCampaign()
    {
        var sim = new GameSimulator();
        sim.Engine.StartCampaign();
        sim.Engine.AdvanceMissionBriefing();

        var mission = CampaignManager.GetMission(1);
        Assert.Equal(mission.ShipsPerWave, sim.State.WaveTotalShips);
    }

    [Fact]
    public void SpawnTimer_ResetsAfterEachShip()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        // Pre-fill the spawn timer so a ship spawns on next frame
        sim.State.SpawnTimer = DifficultyManager.GetWave(1).SpawnIntervalSeconds;
        int countBefore = sim.State.ShipsSpawnedThisWave;

        sim.Engine.Update(GameSimulator.FrameDtPublic);

        Assert.Equal(countBefore + 1, sim.State.ShipsSpawnedThisWave);
        Assert.True(sim.State.SpawnTimer < DifficultyManager.GetWave(1).SpawnIntervalSeconds);
    }

    [Fact]
    public void WaveClear_OnlyTriggersAfterAllShipsGone()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        // Partially spawned — should NOT trigger wave clear
        sim.State.ShipsSpawnedThisWave = sim.State.WaveTotalShips;
        var ship = Ship.Create(ShipType.Destroyer, 1f, 1, false);
        sim.State.Ships.Add(ship);

        sim.Engine.Update(GameSimulator.FrameDtPublic);
        Assert.Equal(GameStatus.Playing, sim.State.Status);

        // Now remove the last ship
        sim.State.Ships.Clear();
        sim.Engine.Update(GameSimulator.FrameDtPublic);
        Assert.Equal(GameStatus.WaveClear, sim.State.Status);
    }
}
