using SeaWolfBlazorVersion.Engine.Models;
using SeaWolfBlazorVersion.Engine.Models.Enums;

namespace SeaWolfBlazorVersion.Engine;

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

        // Combo timeout
        if (State.ComboTimer > 0)
        {
            State.ComboTimer -= dt;
            if (State.ComboTimer <= 0) State.ComboCount = 0;
        }

        // Screen shake decay
        if (State.ShakeTimer > 0)
            State.ShakeTimer = MathF.Max(0f, State.ShakeTimer - dt);

        // Floating text animation
        foreach (var ft in State.FloatingTexts)
        {
            ft.Y -= 45f * dt;
            ft.Life -= dt;
            if (ft.Life <= 0) ft.Active = false;
        }
        State.FloatingTexts.RemoveAll(ft => !ft.Active);

        // Spawn
        State.SpawnTimer += dt;
        if (State.SpawnTimer >= wave.SpawnIntervalSeconds
            && State.ShipsSpawnedThisWave < wave.TotalShips)
        {
            var type = DifficultyManager.PickShipType(wave);
            var direction = Random.Shared.NextSingle() > 0.5f ? 1 : -1;
            State.Ships.Add(Ship.Create(type, wave.SpeedMultiplier, direction));
            State.ShipsSpawnedThisWave++;
            State.SpawnTimer = 0;
        }

        // Update ships
        foreach (var ship in State.Ships)
        {
            if (ship.DamageState == ShipDamageState.Sinking)
            {
                ship.SinkTimer += dt;
                ship.Y += 25f * dt;
                if (ship.SinkTimer >= Ship.SinkDuration)
                    ship.Active = false;
            }
            else
            {
                ship.X += ship.CurrentSpeed * ship.Direction;
            }

            bool escaped = ship.Direction == 1 ? ship.X > 900 : ship.X < -ship.Width;
            if (escaped && ship.DamageState != ShipDamageState.Sinking)
            {
                ship.Active = false;
                State.ShipsEscaped++;
                State.ComboCount = 0;
                State.ComboTimer = 0;
            }

            if (ship.DamageState == ShipDamageState.Burning)
                UpdateFireParticles(ship, dt);
        }
        State.Ships.RemoveAll(s => !s.Active);

        // Game over if too many ships escaped
        if (State.ShipsEscaped >= GameState.MaxEscaped)
        {
            State.Status = GameStatus.GameOver;
            return;
        }

        // Update torpedoes
        foreach (var t in State.Torpedoes)
        {
            t.Y -= t.Speed;
            if (t.Y < -50) t.Active = false;
        }
        State.Torpedoes.RemoveAll(t => !t.Active);

        // Reload
        if (State.IsReloading)
        {
            State.ReloadTimer += dt;
            if (State.ReloadTimer >= GameState.ReloadDuration)
            {
                State.TorpedoCount = GameState.MaxTorpedoes;
                State.IsReloading = false;
                State.ReloadTimer = 0;
            }
        }

        // Collisions
        CollisionDetector.Detect(State);

        // Update explosions
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

        // Wave clear check: all ships for this wave are gone (sunk or escaped)
        if (State.ShipsSpawnedThisWave >= wave.TotalShips && State.Ships.Count == 0)
        {
            State.LastAccuracy = State.TorpedosFired > 0
                ? (int)(100f * State.TorpedosHit / State.TorpedosFired)
                : 0;
            State.AccuracyBonus = State.LastAccuracy switch
            {
                >= 80 => wave.WaveBonusPoints / 2,
                >= 60 => wave.WaveBonusPoints / 4,
                _     => 0
            };
            State.Score += wave.WaveBonusPoints + State.AccuracyBonus;
            State.LastWaveBonus = wave.WaveBonusPoints;
            State.WaveBonusDisplayTimer = GameState.WaveClearPause;
            State.Status = GameStatus.WaveClear;
            State.WaveClearTimer = 0;
        }
    }

    private void UpdateWaveClear(float dt)
    {
        State.WaveClearTimer += dt;
        if (State.WaveClearTimer >= GameState.WaveClearPause)
        {
            State.Wave++;
            State.ShipsSpawnedThisWave = 0;
            State.ShipsSunkThisWave = 0;
            State.SpawnTimer = 0;
            State.TorpedoCount = GameState.MaxTorpedoes;
            State.IsReloading = false;
            State.TorpedosFired = 0;
            State.TorpedosHit = 0;
            State.ComboCount = 0;
            State.ComboTimer = 0;
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
                Size = 4f + Random.Shared.NextSingle() * 6f
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

    public void FireTorpedo()
    {
        if (State.Status != GameStatus.Playing) return;
        if (State.TorpedoCount <= 0 || State.IsReloading) return;

        State.Torpedoes.Add(new Torpedo { X = State.MouseX, Y = 550 });
        State.TorpedoCount--;
        State.TorpedosFired++;

        if (State.TorpedoCount == 0)
        {
            State.IsReloading = true;
            State.ReloadTimer = 0;
        }
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
        State.SpawnTimer = 0;
        State.Ships.Clear();
        State.Torpedoes.Clear();
        State.Explosions.Clear();
        State.FloatingTexts.Clear();
        State.ShipsEscaped = 0;
        State.TorpedosFired = 0;
        State.TorpedosHit = 0;
        State.ComboCount = 0;
        State.ComboTimer = 0;
        State.ShakeTimer = 0;
        State.Status = GameStatus.Playing;
    }
}
