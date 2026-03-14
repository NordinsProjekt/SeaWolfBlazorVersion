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
    }

    private void UpdatePlaying(float dt)
    {
        var wave = DifficultyManager.GetWave(State.Wave);

        TickTimers(dt);
        TickFloatingTexts(dt);
        TrySpawnShip(wave, dt);
        UpdateShips(dt);

        if (State.ShipsEscaped >= GameState.MaxEscaped)
        {
            State.Status = GameStatus.GameOver;
            return;
        }

        UpdateTorpedoes();
        UpdateReload(dt);
        CollisionDetector.Detect(State);
        UpdateExplosions(dt);
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
        if (State.ShipsSpawnedThisWave >= wave.TotalShips) return;

        var type      = DifficultyManager.PickShipType(wave);
        var direction = Random.Shared.NextSingle() > 0.5f ? 1 : -1;
        bool farLane  = wave.WaveNumber >= 3
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
            ship.X += ship.CurrentSpeed * ship.Direction;
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
        State.FloatingTexts.Add(new FloatingText
        {
            X       = ship.Direction == 1 ? 1150f : 130f,
            Y       = ship.Y - 20f,
            Text    = "ESCAPED!",
            Color   = "#FF2200",
            Life    = 2.0f,
            MaxLife = 2.0f
        });
    }

    private void UpdateTorpedoes()
    {
        foreach (var t in State.Torpedoes)
        {
            t.X += t.Vx;
            t.Y += t.Vy;
            if (t.Y < -50 || t.X < -50 || t.X > 1380) t.Active = false;
        }
        State.Torpedoes.RemoveAll(t => !t.Active);
    }

    private void UpdateReload(float dt)
    {
        if (!State.IsReloading) return;
        State.ReloadTimer += dt;
        if (State.ReloadTimer >= GameState.ReloadDuration)
        {
            State.TorpedoCount = GameState.MaxTorpedoes;
            State.IsReloading  = false;
            State.ReloadTimer  = 0;
        }
    }

    private void UpdateExplosions(float dt)
    {
        foreach (var ex in State.Explosions)
        {
            ex.Radius  += 60f  * dt;
            ex.Opacity -= 1.5f * dt;
            foreach (var spark in ex.Sparks)
            {
                spark.X    += spark.Vx * dt;
                spark.Y    += spark.Vy * dt;
                spark.Vy   += 80f * dt; // gravity
                spark.Life -= dt;
            }
            ex.Sparks.RemoveAll(s => s.Life <= 0);
            if (ex.Radius >= ex.MaxRadius) ex.Active = false;
        }
        State.Explosions.RemoveAll(e => !e.Active);
    }

    private void CheckWaveClear(WaveConfig wave)
    {
        if (State.ShipsSpawnedThisWave < wave.TotalShips || State.Ships.Count > 0) return;

        State.LastAccuracy = State.TorpedosFired > 0
            ? (int)(100f * State.TorpedosHit / State.TorpedosFired)
            : 0;
        State.AccuracyBonus = State.LastAccuracy switch
        {
            >= 80 => wave.WaveBonusPoints / 2,
            >= 60 => wave.WaveBonusPoints / 4,
            _     => 0
        };
        State.Score               += wave.WaveBonusPoints + State.AccuracyBonus;
        State.LastWaveBonus        = wave.WaveBonusPoints;
        State.WaveBonusDisplayTimer = GameState.WaveClearPause;
        State.Status               = GameStatus.WaveClear;
        State.WaveClearTimer       = 0;
    }

    private void UpdateWaveClear(float dt)
    {
        State.WaveClearTimer += dt;
        if (State.WaveClearTimer >= GameState.WaveClearPause)
        {
            State.Wave++;
            State.ShipsSpawnedThisWave = 0;
            State.ShipsSunkThisWave = 0;
            State.WaveTotalShips = DifficultyManager.GetWave(State.Wave).TotalShips;
            State.SpawnTimer = DifficultyManager.GetWave(State.Wave).SpawnIntervalSeconds;
            State.TorpedoCount = GameState.MaxTorpedoes;
            State.IsReloading = false;
            State.TorpedosFired = 0;
            State.TorpedosHit = 0;
            State.ComboCount = 0;
            State.ComboTimer = 0;
            State.WaveStartTimer = 0;
            State.Status = GameStatus.Playing;
        }
    }

    private static void UpdateFireParticles(Ship ship, float dt)
    {
        for (int i = 0; i < 2; i++)
        {
            ship.FireParticles.Add(new FireParticle
            {
                X    = ship.X + (Random.Shared.NextSingle() - 0.5f) * ship.Width * 0.6f,
                Y    = ship.Y - ship.Height / 2f,
                Vx   = (Random.Shared.NextSingle() - 0.5f) * 20f,
                Vy   = -(30f + Random.Shared.NextSingle() * 40f),
                Life = 0.8f + Random.Shared.NextSingle() * 0.4f,
                Size = (4f + Random.Shared.NextSingle() * 6f) * ship.DepthScale
            });
        }
        foreach (var p in ship.FireParticles)
        {
            p.X    += p.Vx * dt;
            p.Y    += p.Vy * dt;
            p.Life -= dt * 1.2f;
        }
        ship.FireParticles.RemoveAll(p => p.Life <= 0);
    }

    // Input handlers called by Game.razor

    private static readonly float[] TubeAngles = [-55f, -25f, 0f, 25f, 55f];
    private const float LaunchY = 680f;

    private const float LaunchX = 640f;

    public int ComputeAimedTube(float mouseX, float mouseY)
    {
        // Divide the 1280-px canvas into 5 equal zones (256 px each)
        const float zoneWidth = 1280f / 5f;
        return Math.Clamp((int)(mouseX / zoneWidth), 0, 4);
    }

    public bool FireTorpedoFromTube(int tubeIndex)
    {
        if (State.Status != GameStatus.Playing) return false;
        if (State.TorpedoCount <= 0 || State.IsReloading) return false;

        float angleRad = TubeAngles[Math.Clamp(tubeIndex, 0, 4)] * MathF.PI / 180f;
        const float speed = 5f;
        State.Torpedoes.Add(new Torpedo
        {
            X  = LaunchX,
            Y  = LaunchY,
            Vx = MathF.Sin(angleRad) * speed,
            Vy = -MathF.Cos(angleRad) * speed
        });
        State.TorpedoCount--;
        State.TorpedosFired++;
        State.TotalTorpedosFired++;

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
            GameStatus.Paused  => GameStatus.Playing,
            _                  => State.Status
        };
    }

    public void StartGame()
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
        State.ComboCount = 0;
        State.ComboTimer = 0;
        State.ShakeTimer = 0;
        State.WaveStartTimer = 0;
        State.AimX = 640f;
        State.SelectedTube = 2;
        State.WaveTotalShips = DifficultyManager.GetWave(1).TotalShips;
        State.Status = GameStatus.Playing;
    }
}
