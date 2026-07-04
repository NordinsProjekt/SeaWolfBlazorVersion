using GameEngine.Engine.Models;
using GameEngine.Engine.Models.Enums;

namespace GameEngine.Engine;

public class GameEngine
{
    public GameState State { get; } = new();

    public void Update(float deltaSeconds)
    {
        if (State.Status == GameStatus.Playing)
            UpdatePlaying(deltaSeconds);
        else if (State.Status == GameStatus.WaveClear)
            UpdateWaveClear(deltaSeconds);
        else if (State.Status == GameStatus.MissionBriefing)
            UpdateMissionBriefing(deltaSeconds);
        else if (State.Status == GameStatus.MissionComplete)
            UpdateMissionComplete(deltaSeconds);
    }

    private void UpdatePlaying(float dt)
    {
        var wave = DifficultyManager.GetWave(State.Wave);

        TickTimers(dt);
        TickFloatingTexts(dt);
        TrySpawnShip(wave, dt);
        UpdateShips(dt);

        if (State.Mode == GameMode.Campaign)
        {
            if (State.CampaignLives <= 0)
            {
                State.CampaignMissionFailed = true;
                State.Status = GameStatus.MissionComplete;
                State.MissionScreenTimer = 0;
                return;
            }
        }
        else
        {
            if (State.ShipsEscaped >= GameState.MaxEscaped)
            {
                State.Status = GameStatus.GameOver;
                return;
            }
        }

        UpdateTorpedoes(dt);
        UpdateReload(dt);
        CollisionDetector.Detect(State);
        UpdateExplosions(dt);

        if (State.Mode == GameMode.Campaign)
        {
            var mission = CampaignManager.GetMission(State.CampaignMission);
            if (mission.Objective.MaxCivilianSinks >= 0
                && State.CivilianSinks > mission.Objective.MaxCivilianSinks)
            {
                HandleCampaignOrArcadeFail();
                return;
            }
            UpdateCampaignObjective();
        }

        CheckWaveClear(wave);
    }

    private void TickTimers(float dt)
    {
        if (State.ComboTimer > 0)
        {
            State.ComboTimer -= dt;
            if (State.ComboTimer <= 0) State.ComboCount = 0;
        }
        if (State.WaveStartTimer < 2.5f) State.WaveStartTimer += dt;
        if (State.ShakeTimer > 0) State.ShakeTimer = MathF.Max(0f, State.ShakeTimer - dt);
    }

    private void TickFloatingTexts(float dt)
    {
        foreach (var ft in State.FloatingTexts)
        {
            ft.Y -= 45f * dt;
            ft.Life -= dt;
            if (ft.Life <= 0) ft.Active = false;
        }
        State.FloatingTexts.RemoveAll(ft => !ft.Active);
    }

    private void TrySpawnShip(WaveConfig wave, float dt)
    {
        State.SpawnTimer += dt;
        if (State.SpawnTimer < wave.SpawnIntervalSeconds) return;
        if (State.ShipsSpawnedThisWave >= State.WaveTotalShips) return;

        var type = DifficultyManager.PickShipType(wave);
        var direction = Random.Shared.NextSingle() > 0.5f ? 1 : -1;
        bool farLane = wave.WaveNumber >= 3
            && Random.Shared.NextSingle() < MathF.Min(0.05f * (wave.WaveNumber - 2), 0.40f);
        State.Ships.Add(Ship.Create(type, wave.SpeedMultiplier, direction, farLane));
        State.ShipsSpawnedThisWave++;
        State.SpawnTimer = 0;
    }

    private void UpdateShips(float dt)
    {
        foreach (var ship in State.Ships)
        {
            AdvanceShip(ship, dt);
            if (HasEscaped(ship)) HandleEscape(ship);
            if (ship.DamageState == ShipDamageState.Burning) UpdateFireParticles(ship, dt);
        }
        State.Ships.RemoveAll(s => !s.Active);
    }

    private static void AdvanceShip(Ship ship, float dt)
    {
        if (ship.DamageState == ShipDamageState.Sinking)
        {
            ship.SinkTimer += dt;
            ship.Y += 25f * dt;
            if (ship.SinkTimer >= Ship.SinkDuration) ship.Active = false;
        }
        else
        {
            ship.X += ship.CurrentSpeed * ship.Direction * dt;
        }
    }

    private static bool HasEscaped(Ship ship) =>
        ship.DamageState != ShipDamageState.Sinking &&
        (ship.Direction == 1 ? ship.X > 1380 : ship.X < -ship.Width);

    private void HandleEscape(Ship ship)
    {
        ship.Active = false;
        State.ShipsEscaped++;
        State.ComboCount = 0;
        State.ComboTimer = 0;

        if (State.Mode == GameMode.Campaign)
            State.CampaignLives--;

        State.FloatingTexts.Add(new FloatingText
        {
            X = ship.Direction == 1 ? 1150f : 130f,
            Y = ship.Y - 20f,
            Text = "ESCAPED!",
            Color = "#FF2200",
            Life = 2.0f,
            MaxLife = 2.0f
        });
    }

    private void UpdateTorpedoes(float dt)
    {
        foreach (var t in State.Torpedoes)
        {
            t.X += t.Vx * dt;
            t.Y += t.Vy * dt;
            if (t.Y < -50 || t.X < -50 || t.X > 1380) t.Active = false;
        }
        State.Torpedoes.RemoveAll(t => !t.Active);
    }

    private void UpdateReload(float dt)
    {
        if (!State.IsReloading) return;

        // If campaign budget is exhausted, stay empty — no reload
        if (State.Mode == GameMode.Campaign && State.TorpedoBudgetLeft == 0
            && CampaignManager.GetMission(State.CampaignMission).Objective.TorpedoBudget > 0)
        {
            State.IsReloading = false;
            return;
        }

        State.ReloadTimer += dt;
        if (State.ReloadTimer >= GameState.ReloadDuration)
        {
            int restore = State.TorpedoBudgetLeft > 0
                ? Math.Min(GameState.MaxTorpedoes, State.TorpedoBudgetLeft)
                : GameState.MaxTorpedoes;
            State.TorpedoCount = restore;
            State.IsReloading = false;
            State.ReloadTimer = 0;
        }
    }

    private void UpdateExplosions(float dt)
    {
        foreach (var ex in State.Explosions)
        {
            ex.Radius += 60f * dt;
            ex.Opacity -= 1.5f * dt;
            foreach (var spark in ex.Sparks)
            {
                spark.X += spark.Vx * dt;
                spark.Y += spark.Vy * dt;
                spark.Vy += 80f * dt; // gravity
                spark.Life -= dt;
            }
            ex.Sparks.RemoveAll(s => s.Life <= 0);
            if (ex.Radius >= ex.MaxRadius) ex.Active = false;
        }
        State.Explosions.RemoveAll(e => !e.Active);
    }

    private void CheckWaveClear(WaveConfig wave)
    {
        if (State.ShipsSpawnedThisWave < State.WaveTotalShips || State.Ships.Count > 0) return;

        State.LastAccuracy = State.TorpedosFired > 0
            ? (int)(100f * State.TorpedosHit / State.TorpedosFired)
            : 0;
        State.AccuracyBonus = State.LastAccuracy switch
        {
            >= 80 => wave.WaveBonusPoints / 2,
            >= 60 => wave.WaveBonusPoints / 4,
            _ => 0
        };
        State.Score += wave.WaveBonusPoints + State.AccuracyBonus;
        State.LastWaveBonus = wave.WaveBonusPoints;
        State.WaveBonusDisplayTimer = GameState.WaveClearPause;
        State.Status = GameStatus.WaveClear;
        State.WaveClearTimer = 0;
    }

    private void UpdateWaveClear(float dt)
    {
        State.WaveClearTimer += dt;
        if (State.WaveClearTimer < GameState.WaveClearPause) return;

        if (State.Mode == GameMode.Campaign)
        {
            var mission = CampaignManager.GetMission(State.CampaignMission);
            if (State.Wave >= mission.EndWave ||
                State.CampaignSinks >= mission.Objective.RequiredSinks)
            {
                CompleteMission(mission);
                return;
            }
        }

        AdvanceToNextWave();
    }

    private static void UpdateFireParticles(Ship ship, float dt)
    {
        // Probabilistic single spawn instead of a fixed 2-per-frame. At 60fps,
        // "2 per frame" is ~120 particles/sec — with an ~1s average lifetime
        // that's roughly 120 particles alive at once on a ship that's only
        // 130-220px wide, so densely overlapping that the pixel-art blocks
        // merge into a solid rectangle instead of reading as flame. ~45%
        // chance of 1 particle/frame (~27/sec) keeps individual flame licks
        // visually distinct.
        if (Random.Shared.NextSingle() < 0.45f)
        {
            ship.FireParticles.Add(new FireParticle
            {
                X = ship.X + (Random.Shared.NextSingle() - 0.5f) * ship.Width * 0.6f,
                Y = ship.Y - ship.Height / 2f,
                Vx = (Random.Shared.NextSingle() - 0.5f) * 20f,
                Vy = -(30f + Random.Shared.NextSingle() * 40f),
                Life = 0.8f + Random.Shared.NextSingle() * 0.4f,
                Size = (4f + Random.Shared.NextSingle() * 6f) * ship.DepthScale
            });
        }
        foreach (var p in ship.FireParticles)
        {
            p.X += p.Vx * dt;
            p.Y += p.Vy * dt;
            p.Life -= dt * 1.2f;
        }
        ship.FireParticles.RemoveAll(p => p.Life <= 0);
    }

    // Input handlers called by Game.razor

    public int ComputeAimedTube(float mouseX, float mouseY)
    {
        // Pick whichever tube's line passes closest to the cursor at the
        // main ship-lane depth. See TorpedoTubes for why they're spaced
        // this way instead of evenly by raw screen-X zones.
        var targets = TorpedoTubes.TargetX;
        int best = 0;
        float bestDist = MathF.Abs(mouseX - targets[0]);
        for (int i = 1; i < targets.Length; i++)
        {
            float dist = MathF.Abs(mouseX - targets[i]);
            if (dist < bestDist) { bestDist = dist; best = i; }
        }
        return best;
    }

    public bool FireTorpedoFromTube(int tubeIndex)
    {
        if (State.Status != GameStatus.Playing) return false;
        if (State.TorpedoCount <= 0 || State.IsReloading) return false;
        if (State.TorpedoBudgetLeft == 1 && State.Mode == GameMode.Campaign) { /* last shot allowed */ }

        int tube = Math.Clamp(tubeIndex, 0, TorpedoTubes.Count - 1);
        float angleRad = TorpedoTubes.AngleDeg[tube] * MathF.PI / 180f;
        const float speed = 300f;
        State.Torpedoes.Add(new Torpedo
        {
            X = TorpedoTubes.LaunchX,
            Y = TorpedoTubes.LaunchY,
            Vx = MathF.Sin(angleRad) * speed,
            Vy = -MathF.Cos(angleRad) * speed
        });
        State.TorpedoCount--;
        State.TorpedosFired++;
        State.TotalTorpedosFired++;

        if (State.TorpedoBudgetLeft > 0)
            State.TorpedoBudgetLeft--;

        if (State.TorpedoCount == 0)
        {
            State.IsReloading = true;
            State.ReloadTimer = 0;
        }

        return true;
    }

    public void TogglePause()
    {
        State.Status = State.Status switch
        {
            GameStatus.Playing => GameStatus.Paused,
            GameStatus.Paused => GameStatus.Playing,
            _ => State.Status
        };
    }

    public void StartGame()
    {
        ResetSharedState();
        State.Mode = GameMode.Arcade;
        State.CampaignMission = 1;
        State.CampaignLives = 3;
        State.CampaignSinks = 0;
        State.TorpedoBudgetLeft = 0;
        State.CampaignMissionFailed = false;
        State.WaveTotalShips = DifficultyManager.GetWave(1).TotalShips;
        State.Status = GameStatus.Playing;
    }

    public void StartCampaign()
    {
        ResetSharedState();
        State.Mode = GameMode.Campaign;
        State.CampaignMission = 1;
        State.CampaignMissionFailed = false;
        LoadMission(CampaignManager.GetMission(1));
    }

    private void LoadMission(MissionConfig mission)
    {
        State.CampaignLives = mission.Lives;
        State.CampaignSinks = 0;
        State.CivilianSinks = 0;
        State.SinksByType = new int[7];
        State.ShipsEscaped = 0;
        State.Wave = mission.StartWave;
        State.TorpedoBudgetLeft = mission.Objective.TorpedoBudget;
        State.CampaignMissionFailed = false;
        State.MissionScreenTimer = 0;
        State.WaveTotalShips = mission.ShipsPerWave;
        State.Status = GameStatus.MissionBriefing;
    }

    public void AdvanceMissionBriefing()
    {
        if (State.Status != GameStatus.MissionBriefing) return;
        int shipOverride = State.Mode == GameMode.Campaign
            ? CampaignManager.GetMission(State.CampaignMission).ShipsPerWave
            : 0;
        ResetWave(State.Wave, shipOverride);
        State.TorpedoCount = State.TorpedoBudgetLeft > 0
            ? Math.Min(GameState.MaxTorpedoes, State.TorpedoBudgetLeft)
            : GameState.MaxTorpedoes;
        State.Status = GameStatus.Playing;
    }

    private void ResetSharedState()
    {
        State.Score = 0;
        State.Wave = 1;
        State.TorpedoCount = GameState.MaxTorpedoes;
        State.IsReloading = false;
        State.ShipsSpawnedThisWave = 0;
        State.ShipsSunkThisWave = 0;
        State.SpawnTimer = DifficultyManager.GetWave(1).SpawnIntervalSeconds;
        State.Ships.Clear();
        State.Torpedoes.Clear();
        State.Explosions.Clear();
        State.FloatingTexts.Clear();
        State.ShipsEscaped = 0;
        State.TorpedosFired = 0;
        State.TorpedosHit = 0;
        State.TotalTorpedosFired = 0;
        State.TotalTorpedosHit = 0;
        State.TotalShipsSunk = 0;
        State.SinksByType = new int[7];
        State.ComboCount = 0;
        State.ComboTimer = 0;
        State.ShakeTimer = 0;
        State.WaveStartTimer = 0;
        State.AimX = 640f;
        State.SelectedTube = TorpedoTubes.DefaultTube;
    }

    private void ResetWave(int waveNumber, int shipCountOverride = 0)
    {
        var waveConfig = DifficultyManager.GetWave(waveNumber);
        State.ShipsSpawnedThisWave = 0;
        State.ShipsSunkThisWave = 0;
        State.SpawnTimer = waveConfig.SpawnIntervalSeconds;
        State.WaveTotalShips = shipCountOverride > 0 ? shipCountOverride : waveConfig.TotalShips;
        State.TorpedosFired = 0;
        State.TorpedosHit = 0;
        State.ComboCount = 0;
        State.ComboTimer = 0;
        State.WaveStartTimer = 0;
        State.Ships.Clear();
        State.Torpedoes.Clear();
        State.Explosions.Clear();
        State.FloatingTexts.Clear();
        State.IsReloading = false;
    }

    private void AdvanceToNextWave()
    {
        State.Wave++;
        int shipOverride = State.Mode == GameMode.Campaign
            ? CampaignManager.GetMission(State.CampaignMission).ShipsPerWave
            : 0;
        ResetWave(State.Wave, shipOverride);
        State.TorpedoCount = State.TorpedoBudgetLeft > 0
            ? Math.Min(GameState.MaxTorpedoes, State.TorpedoBudgetLeft)
            : GameState.MaxTorpedoes;
        State.Status = GameStatus.Playing;
    }

    // ── Campaign helpers ──────────────────────────────────────────────────────

    private void HandleCampaignOrArcadeFail()
    {
        if (State.Mode == GameMode.Arcade)
        {
            State.Status = GameStatus.GameOver;
            return;
        }

        State.CampaignLives--;
        if (State.CampaignLives <= 0)
        {
            State.CampaignMissionFailed = true;
            State.Status = GameStatus.MissionComplete;
            State.MissionScreenTimer = 0;
        }
        else
        {
            // Retry the mission from its starting wave
            var mission = CampaignManager.GetMission(State.CampaignMission);
            State.ShipsEscaped = 0;
            ResetWave(mission.StartWave, mission.ShipsPerWave);
            State.Wave = mission.StartWave;
            State.TorpedoCount = State.TorpedoBudgetLeft > 0
                ? Math.Min(GameState.MaxTorpedoes, State.TorpedoBudgetLeft)
                : GameState.MaxTorpedoes;
            State.Status = GameStatus.Playing;
        }
    }

    private void UpdateCampaignObjective()
    {
        var mission = CampaignManager.GetMission(State.CampaignMission);
        State.CampaignSinks = CampaignManager.CountObjectiveSinks(
            mission, State.TotalShipsSunk, State.SinksByTypeDict());

        // When the objective is met, stop spawning new ships so the remaining
        // ones sail off or sink naturally. CheckWaveClear will fire once the
        // screen is empty and UpdateWaveClear will call CompleteMission.
        if (State.CampaignSinks >= mission.Objective.RequiredSinks)
            State.WaveTotalShips = State.ShipsSpawnedThisWave;
    }

    private void CompleteMission(MissionConfig mission)
    {
        State.CampaignSinks = CampaignManager.CountObjectiveSinks(
            mission, State.TotalShipsSunk, State.SinksByTypeDict());

        bool objectiveMet = State.CampaignSinks >= mission.Objective.RequiredSinks;
        State.CampaignMissionFailed = !objectiveMet;
        State.MissionScreenTimer = 0;

        if (!objectiveMet)
        {
            // Failed objective — lives penalty, then retry or game over
            HandleCampaignOrArcadeFail();
            return;
        }

        // Clear the battlefield so nothing lingers on the transition screen
        State.Ships.Clear();
        State.Torpedoes.Clear();
        State.Explosions.Clear();

        State.Status = CampaignManager.IsLastMission(mission.MissionNumber)
            ? GameStatus.CampaignComplete
            : GameStatus.MissionComplete;
    }

    private void UpdateMissionBriefing(float dt)
    {
        State.MissionScreenTimer += dt;
        // Auto-advance after 30 seconds in case the player doesn't click
        if (State.MissionScreenTimer >= 30f)
            AdvanceMissionBriefing();
    }

    private void UpdateMissionComplete(float dt)
    {
        State.MissionScreenTimer += dt;
        // Auto-advance to next mission after 5 seconds
        if (State.MissionScreenTimer >= 5f)
            AdvanceToNextMission();
    }

    public void AdvanceToNextMission()
    {
        if (State.CampaignMissionFailed)
        {
            State.Status = GameStatus.StartScreen;
            return;
        }

        int next = State.CampaignMission + 1;
        if (next > CampaignManager.Missions.Count)
        {
            State.Status = GameStatus.StartScreen;
            return;
        }

        State.CampaignMission = next;
        LoadMission(CampaignManager.GetMission(next));
    }

    public void ReturnToStartScreen()
    {
        State.Ships.Clear();
        State.Torpedoes.Clear();
        State.Explosions.Clear();
        State.FloatingTexts.Clear();
        State.Status = GameStatus.StartScreen;
    }
}
