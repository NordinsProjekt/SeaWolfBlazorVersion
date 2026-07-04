using GameEngine.Engine;
using GameEngine.Engine.Models;
using GameEngine.Engine.Models.Enums;

namespace GameEngineTests;

public class CampaignTests
{
    // ── StartCampaign ────────────────────────────────────────────────────────

    [Fact]
    public void StartCampaign_SetsModeAndStatus()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        Assert.Equal(GameMode.Campaign, engine.State.Mode);
        Assert.Equal(GameStatus.MissionBriefing, engine.State.Status);
    }

    [Fact]
    public void StartCampaign_SetsMission1()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        Assert.Equal(1, engine.State.CampaignMission);
    }

    [Fact]
    public void StartCampaign_SetsLivesFromMissionConfig()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        var mission = CampaignManager.GetMission(1);
        Assert.Equal(mission.Lives, engine.State.CampaignLives);
    }

    [Fact]
    public void StartCampaign_StartsAtMission1Wave()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        var mission = CampaignManager.GetMission(1);
        Assert.Equal(mission.StartWave, engine.State.Wave);
    }

    // ── AdvanceMissionBriefing ───────────────────────────────────────────────

    [Fact]
    public void AdvanceMissionBriefing_TransitionsToPlaying()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.AdvanceMissionBriefing();
        Assert.Equal(GameStatus.Playing, engine.State.Status);
    }

    [Fact]
    public void AdvanceMissionBriefing_WhenNotBriefing_DoesNothing()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame(); // Arcade, status = Playing
        engine.AdvanceMissionBriefing();
        Assert.Equal(GameStatus.Playing, engine.State.Status);
    }

    [Fact]
    public void AdvanceMissionBriefing_ResetsTorpedoCount()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.AdvanceMissionBriefing();
        Assert.Equal(GameState.MaxTorpedoes, engine.State.TorpedoCount);
    }

    // ── Torpedo budget ───────────────────────────────────────────────────────

    [Fact]
    public void TorpedoBudget_Mission4_SetsToBudgetValue()
    {
        var engine = new GameEngine.Engine.GameEngine();
        // Mission 4 (IRON CURTAIN) has TorpedoBudget = 12
        var m = CampaignManager.GetMission(4);
        Assert.Equal(12, m.Objective.TorpedoBudget);
    }

    [Fact]
    public void TorpedoBudget_Decrements_OnFire()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        // Inject a budget manually
        engine.State.TorpedoBudgetLeft = 5;
        engine.AdvanceMissionBriefing();
        engine.FireTorpedoFromTube(2);
        Assert.Equal(4, engine.State.TorpedoBudgetLeft);
    }

    [Fact]
    public void TorpedoBudget_Zero_MeansUnlimited_InArcade()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        Assert.Equal(0, engine.State.TorpedoBudgetLeft); // unlimited
    }

    // ── Campaign objective tracking ──────────────────────────────────────────

    [Fact]
    public void SinksByType_TracksDestroyerKill()
    {
        var state = new GameState();
        state.Mode = GameMode.Campaign;
        var ship = new Ship
        {
            X = 500,
            Y = 400,
            Width = 130,
            Height = 45,
            BasePoints = 100,
            Type = ShipType.Destroyer
        };
        ship.DamageState = ShipDamageState.Healthy;
        state.Ships.Add(ship);
        float bottomEdge = ship.Y + ship.Height / 2f;
        state.Torpedoes.Add(new Torpedo { X = ship.X, Y = bottomEdge });

        CollisionDetector.Detect(state);

        Assert.Equal(1, state.SinksByType[(int)ShipType.Destroyer]);
    }

    [Fact]
    public void SinksByType_TracksBurningKill()
    {
        var state = new GameState();
        var ship = new Ship
        {
            X = 500,
            Y = 400,
            Width = 160,
            Height = 55,
            BasePoints = 150,
            Type = ShipType.Cargo
        };
        ship.DamageState = ShipDamageState.Burning;
        state.Ships.Add(ship);
        float bottomEdge = ship.Y + ship.Height / 2f;
        state.Torpedoes.Add(new Torpedo { X = ship.X, Y = bottomEdge });

        CollisionDetector.Detect(state);

        Assert.Equal(1, state.SinksByType[(int)ShipType.Cargo]);
    }

    [Fact]
    public void CountObjectiveSinks_AllTypes_CountsTotal()
    {
        var mission = CampaignManager.GetMission(5); // all types (DEEP STRIKE)
        var sinksByType = new Dictionary<ShipType, int>
        {
            [ShipType.Destroyer] = 5,
            [ShipType.Cargo] = 3,
            [ShipType.Cruiser] = 4,
        };
        int total = 5 + 3 + 4;
        int result = CampaignManager.CountObjectiveSinks(mission, total, sinksByType);
        Assert.Equal(total, result);
    }

    [Fact]
    public void CountObjectiveSinks_TargetTypes_OnlyCounts_MatchingTypes()
    {
        var mission = CampaignManager.GetMission(2); // Cargo + Tanker
        var sinksByType = new Dictionary<ShipType, int>
        {
            [ShipType.Destroyer] = 10,
            [ShipType.Cargo] = 3,
            [ShipType.Tanker] = 2,
        };
        int result = CampaignManager.CountObjectiveSinks(mission, 15, sinksByType);
        Assert.Equal(5, result); // 3 + 2 only
    }

    // ── Campaign lifecycle ───────────────────────────────────────────────────

    [Fact]
    public void CampaignManager_HasSixMissions()
    {
        Assert.Equal(6, CampaignManager.Missions.Count);
    }

    [Fact]
    public void CampaignManager_IsLastMission_ReturnsTrueForMission6()
    {
        Assert.True(CampaignManager.IsLastMission(6));
    }

    [Fact]
    public void CampaignManager_IsLastMission_ReturnsFalseForMission5()
    {
        Assert.False(CampaignManager.IsLastMission(5));
    }

    [Fact]
    public void CampaignManager_EachMission_HasUniqueCodeName()
    {
        var names = CampaignManager.Missions.Select(m => m.CodeName).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void CampaignManager_EachMission_HasNonEmptyBriefing()
    {
        foreach (var m in CampaignManager.Missions)
            Assert.False(string.IsNullOrWhiteSpace(m.Briefing),
                $"Mission {m.MissionNumber} has empty briefing");
    }

    [Fact]
    public void CampaignManager_EachMission_EndWave_GreaterThanOrEqualStartWave()
    {
        foreach (var m in CampaignManager.Missions)
            Assert.True(m.EndWave >= m.StartWave,
                $"Mission {m.MissionNumber}: EndWave ({m.EndWave}) < StartWave ({m.StartWave})");
    }

    // ── Lives / fail system ──────────────────────────────────────────────────

    [Fact]
    public void Campaign_ExceedEscapeLimit_DecrementsLives()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.AdvanceMissionBriefing();

        int initialLives = engine.State.CampaignLives;

        // Add a ship that has already escaped off-screen (Destroyer is one
        // of mission 1's objective types, so this must cost a life)
        engine.State.Ships.Add(new Ship
        {
            X = 1400, Y = 200, Width = 100, Height = 40,
            Direction = 1, BasePoints = 100, Type = ShipType.Destroyer
        });
        engine.Update(0.016f); // HandleEscape fires → CampaignLives--

        Assert.Equal(initialLives - 1, engine.State.CampaignLives);
    }

    [Fact]
    public void Campaign_NonObjectiveShipType_Escaping_DoesNotCostALife()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign(); // Mission 1 objective: Destroyer + PtBoat only
        engine.AdvanceMissionBriefing();

        int initialLives = engine.State.CampaignLives;

        // Tanker is NOT one of mission 1's target types — letting one
        // escape must not cost a life (this is the exact scenario reported:
        // a tanker escaping during a "destroy 4 destroyers" mission was
        // wrongly costing health).
        engine.State.Ships.Add(new Ship
        {
            X = 1400, Y = 200, Width = 185, Height = 60,
            Direction = 1, BasePoints = 400, Type = ShipType.Tanker
        });
        engine.Update(0.016f);

        Assert.Equal(initialLives, engine.State.CampaignLives);
        Assert.Equal(GameStatus.Playing, engine.State.Status);
        Assert.Equal(1, engine.State.ShipsEscaped); // still tracked for stats, just no life lost
    }

    [Fact]
    public void Campaign_OutOfLives_SetsMissionFailed()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.AdvanceMissionBriefing();

        // Set to 1 life, then let a ship escape → CampaignLives becomes 0
        engine.State.CampaignLives = 1;
        engine.State.Ships.Add(new Ship
        {
            X = 1400, Y = 200, Width = 100, Height = 40,
            Direction = 1, BasePoints = 100, Type = ShipType.Destroyer
        });
        engine.Update(0.016f);

        Assert.True(engine.State.CampaignMissionFailed);
        Assert.Equal(GameStatus.MissionComplete, engine.State.Status);
    }

    [Fact]
    public void Campaign_EscapeWithLivesRemaining_ContinuesPlaying()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.AdvanceMissionBriefing();

        engine.State.CampaignLives = 3;
        engine.State.Ships.Add(new Ship
        {
            X = 1400, Y = 200, Width = 100, Height = 40,
            Direction = 1, BasePoints = 100, Type = ShipType.Destroyer
        });
        engine.Update(0.016f);

        Assert.Equal(GameStatus.Playing, engine.State.Status);
        Assert.Equal(2, engine.State.CampaignLives);
    }

    // ── Mode separation ──────────────────────────────────────────────────────

    [Fact]
    public void StartGame_SetsArcadeMode()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.StartGame();
        Assert.Equal(GameMode.Arcade, engine.State.Mode);
    }

    [Fact]
    public void Arcade_ExceedEscapeLimit_SetsGameOver_NotMissionComplete()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartGame();
        engine.State.ShipsEscaped = GameState.MaxEscaped;
        engine.Update(0.016f);
        Assert.Equal(GameStatus.GameOver, engine.State.Status);
    }

    // ── GameState campaign fields serialise ──────────────────────────────────

    [Fact]
    public void GameState_CampaignMissions_ReturnsAllSix()
    {
        var state = new GameState();
        Assert.Equal(6, state.CampaignMissions.Count);
    }

    [Fact]
    public void GameState_CampaignMissions_FirstMission_HasCorrectCodeName()
    {
        var state = new GameState();
        Assert.Equal(CampaignManager.Missions[0].CodeName, state.CampaignMissions[0].CodeName);
    }

    [Fact]
    public void GameState_SinksByTypeDict_ReflectsArray()
    {
        var state = new GameState();
        state.SinksByType[(int)ShipType.Cruiser] = 3;
        var d = state.SinksByTypeDict();
        Assert.Equal(3, d[ShipType.Cruiser]);
    }

    // ── Objective completion ─────────────────────────────────────────────────

    [Fact]
    public void Campaign_ObjectiveMet_StopsSpawningNewShips()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.AdvanceMissionBriefing();

        var mission = CampaignManager.GetMission(1);
        int required = mission.Objective.RequiredSinks;

        // Distribute required sinks across target types (not required per type)
        int perType = required / mission.Objective.TargetTypes.Count;
        int remainder = required % mission.Objective.TargetTypes.Count;
        for (int i = 0; i < mission.Objective.TargetTypes.Count; i++)
        {
            int count = perType + (i == 0 ? remainder : 0);
            engine.State.SinksByType[(int)mission.Objective.TargetTypes[i]] = count;
            engine.State.TotalShipsSunk += count;
        }

        // First tick: CampaignSinks catches up to the injected total. The
        // mission is a single continuous stage now (no fixed ship quota),
        // so the only thing that should change is that spawning stops.
        engine.Update(0.016f);
        Assert.Equal(required, engine.State.CampaignSinks);
        Assert.Equal(GameStatus.Playing, engine.State.Status);

        int spawnedAfterObjectiveMet = engine.State.ShipsSpawnedThisWave;

        // Force past the spawn-interval gate so the only thing that could
        // still block a spawn is the objective-met check itself.
        engine.State.SpawnTimer = 999f;
        engine.Update(0.016f);

        Assert.Equal(spawnedAfterObjectiveMet, engine.State.ShipsSpawnedThisWave);
    }

    [Fact]
    public void Campaign_ObjectiveMet_ClearsShipsAndTorpedoes()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.AdvanceMissionBriefing();

        var mission = CampaignManager.GetMission(1);

        // Put a ship and a torpedo on screen
        engine.State.Ships.Add(new Ship { X = 300, Y = 400, Active = true });
        engine.State.Torpedoes.Add(new Torpedo { X = 300, Y = 200, Active = true });

        // Satisfy the objective — spawning stops, but the ship already on
        // screen must still resolve before the mission completes.
        foreach (var type in mission.Objective.TargetTypes)
            engine.State.SinksByType[(int)type] = mission.Objective.RequiredSinks;
        engine.State.TotalShipsSunk = mission.Objective.RequiredSinks;

        engine.Update(0.016f);
        Assert.Equal(GameStatus.Playing, engine.State.Status);

        // Simulate the remaining ship leaving the screen — the mission
        // completes immediately on the next tick, with no wave-clear
        // interstitial in between (campaign missions are one continuous
        // stage; see GameEngine.UpdateCampaignObjective).
        engine.State.Ships.Clear();
        engine.Update(0.016f);

        Assert.Equal(GameStatus.MissionComplete, engine.State.Status);
        Assert.False(engine.State.CampaignMissionFailed);
        Assert.Empty(engine.State.Ships);
        Assert.Empty(engine.State.Torpedoes);
    }

    // ── Civilian ship protection ─────────────────────────────────────────────

    [Fact]
    public void Mission3_HasZeroTolerance_ForCivilianSinks()
    {
        var mission = CampaignManager.GetMission(3); // SAFE PASSAGE
        Assert.Equal(0, mission.Objective.MaxCivilianSinks);
    }

    [Fact]
    public void OtherMissions_HaveNoRestriction_OnCivilianSinks()
    {
        foreach (var m in CampaignManager.Missions.Where(m => m.MissionNumber != 3))
            Assert.Equal(-1, m.Objective.MaxCivilianSinks);
    }

    [Fact]
    public void CivilianSink_Increments_CivilianSinksCounter()
    {
        var state = new GameState();
        var ship = new Ship
        {
            X = 500,
            Y = 400,
            Width = 95,
            Height = 32,
            BasePoints = 75,
            Type = ShipType.FishingBoat
        };
        ship.DamageState = ShipDamageState.Healthy;
        state.Ships.Add(ship);
        float bottomEdge = ship.Y + ship.Height / 2f;
        state.Torpedoes.Add(new Torpedo { X = ship.X, Y = bottomEdge });

        CollisionDetector.Detect(state);

        Assert.Equal(1, state.CivilianSinks);
    }

    [Fact]
    public void NonCivilianSink_DoesNotIncrement_CivilianSinksCounter()
    {
        var state = new GameState();
        var ship = new Ship
        {
            X = 500,
            Y = 400,
            Width = 130,
            Height = 45,
            BasePoints = 100,
            Type = ShipType.Destroyer
        };
        ship.DamageState = ShipDamageState.Healthy;
        state.Ships.Add(ship);
        float bottomEdge = ship.Y + ship.Height / 2f;
        state.Torpedoes.Add(new Torpedo { X = ship.X, Y = bottomEdge });

        CollisionDetector.Detect(state);

        Assert.Equal(0, state.CivilianSinks);
    }

    [Fact]
    public void Campaign_Mission3_SinkingCivilian_TriggersFailImmediately()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        // Advance to mission 3
        engine.State.CampaignMission = 3;
        engine.State.Status = GameStatus.MissionBriefing;
        engine.AdvanceMissionBriefing();

        // Drain to 1 life so HandleCampaignOrArcadeFail produces MissionComplete (out-of-lives path)
        engine.State.CampaignLives = 1;

        // Sink a civilian — CivilianSinks goes to 1 which exceeds MaxCivilianSinks (0)
        engine.State.CivilianSinks = 1;

        engine.Update(0.016f);

        // With 1 life, out-of-lives path sets MissionComplete + CampaignMissionFailed
        Assert.True(engine.State.CampaignMissionFailed);
        Assert.Equal(GameStatus.MissionComplete, engine.State.Status);
    }

    [Fact]
    public void Campaign_Mission1_SinkingCivilian_DoesNotFail()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.AdvanceMissionBriefing();

        // Mission 1 has MaxCivilianSinks = -1 (no restriction)
        engine.State.CivilianSinks = 5;

        engine.Update(0.016f);

        // Should still be Playing (no ships escaped, no objective met yet)
        Assert.Equal(GameStatus.Playing, engine.State.Status);
    }

    [Fact]
    public void LoadMission_Resets_CivilianSinksToZero()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.State.CivilianSinks = 3;
        engine.StartCampaign(); // restarts → LoadMission(1)
        Assert.Equal(0, engine.State.CivilianSinks);
    }

    // ── Mission failure returns to start screen ──────────────────────────────

    [Fact]
    public void AdvanceToNextMission_WhenMissionFailed_ReturnsToStartScreen()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.AdvanceMissionBriefing();

        // Force mission failure: set lives to 1 and let a ship escape
        engine.State.CampaignLives = 1;
        engine.State.Ships.Add(new Ship
        {
            X = 1400, Y = 200, Width = 100, Height = 40,
            Direction = 1, BasePoints = 100, Type = ShipType.Destroyer
        });
        engine.Update(0.016f); // escape → CampaignLives 0 → MissionComplete + Failed

        Assert.True(engine.State.CampaignMissionFailed);
        Assert.Equal(GameStatus.MissionComplete, engine.State.Status);

        // Dismissing the screen must send the player back to the start screen
        engine.AdvanceToNextMission();

        Assert.Equal(GameStatus.StartScreen, engine.State.Status);
    }

    [Fact]
    public void AdvanceToNextMission_WhenMissionSucceeded_DoesNotGoToStartScreen()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.AdvanceMissionBriefing();

        // Satisfy mission 1 objective — spawn is capped, ships must clear naturally
        var mission = CampaignManager.GetMission(1);
        foreach (var type in mission.Objective.TargetTypes)
            engine.State.SinksByType[(int)type] = mission.Objective.RequiredSinks;
        engine.State.TotalShipsSunk = mission.Objective.RequiredSinks;

        engine.Update(0.016f);

        // Simulate ships leaving the screen, then drive through WaveClear pause
        engine.State.Ships.Clear();
        engine.Update(0.016f);
        engine.Update(GameState.WaveClearPause + 0.1f);

        Assert.False(engine.State.CampaignMissionFailed);
        Assert.Equal(GameStatus.MissionComplete, engine.State.Status);

        // Dismissing a successful mission must advance to the next mission briefing
        engine.AdvanceToNextMission();

        Assert.Equal(GameStatus.MissionBriefing, engine.State.Status);
        Assert.Equal(2, engine.State.CampaignMission);
    }

    // ── Per-mission ShipsPerWave ──────────────────────────────────────────────

    [Fact]
    public void Mission_ShipsPerWave_OverridesDifficultyManagerCount()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.AdvanceMissionBriefing();

        var mission = CampaignManager.GetMission(1);
        // WaveTotalShips must equal the mission's ShipsPerWave, not DifficultyManager's value
        Assert.Equal(mission.ShipsPerWave, engine.State.WaveTotalShips);
    }

    [Fact]
    public void AllMissions_HaveShipsPerWave_GreaterThanZero()
    {
        foreach (var m in CampaignManager.Missions)
            Assert.True(m.ShipsPerWave > 0,
                $"Mission {m.MissionNumber} has ShipsPerWave = {m.ShipsPerWave}");
    }

    [Fact]
    public void Campaign_WaveTier_RampsEvery_ShipsPerWave_Spawned_CappedAtEndWave()
    {
        var engine = new GameEngine.Engine.GameEngine();
        engine.StartCampaign();
        engine.AdvanceMissionBriefing();

        var mission = CampaignManager.GetMission(1); // StartWave 1, EndWave 2, ShipsPerWave 8
        Assert.Equal(mission.StartWave, engine.State.Wave);

        // Give plenty of simulated time for many ships to spawn without
        // ever meeting the objective — the wave tier should climb by one
        // every ShipsPerWave ships spawned, but never exceed EndWave, and
        // the mission must keep running the whole time (no pause, no
        // early completion).
        engine.State.CampaignLives = 9999; // isolate from the escape/lives mechanic
        for (int i = 0; i < 4000; i++)
            engine.Update(0.016f);

        Assert.Equal(GameStatus.Playing, engine.State.Status);
        Assert.Equal(mission.EndWave, engine.State.Wave);
    }
}
