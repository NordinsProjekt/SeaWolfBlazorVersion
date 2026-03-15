using GameEngine.Engine.Models;
using GameEngine.Engine.Models.Enums;

namespace GameEngine.Engine;

public static class CollisionDetector
{
    private static readonly HashSet<ShipType> BonusTorpedoShipTypes =
        [ShipType.Cargo, ShipType.Cruiser, ShipType.Tanker, ShipType.Carrier];

    public static void Detect(GameState state)
    {
        foreach (var torpedo in state.Torpedoes.Where(t => t.Active))
        {
            foreach (var ship in state.Ships.Where(s =>
                s.Active && s.DamageState != ShipDamageState.Sinking))
            {
                if (!ship.CheckCollision(torpedo.X, torpedo.Y)) continue;
                int mult = RegisterHit(state, torpedo);
                ApplyDamage(state, ship, mult);
                break;
            }
        }
    }

    private static int RegisterHit(GameState state, Torpedo torpedo)
    {
        torpedo.Active = false;
        state.ComboCount++;
        state.ComboTimer = GameState.ComboTimeout;
        state.TorpedosHit++;
        state.TotalTorpedosHit++;
        return GetMultiplier(state.ComboCount);
    }

    private static void ApplyDamage(GameState state, Ship ship, int mult)
    {
        if (ship.DamageState == ShipDamageState.Healthy)
            HandleHealthyHit(state, ship, mult);
        else if (ship.DamageState == ShipDamageState.Burning)
            HandleBurningKill(state, ship, mult);
    }

    private static void HandleHealthyHit(GameState state, Ship ship, int mult)
    {
        if (ship.RequiresTwoHits)
        {
            ship.DamageState = ShipDamageState.Burning;
            int pts = ship.HitPoints * mult;
            state.Score += pts;
            SpawnSmallExplosion(state, ship.X, ship.Y);
            SpawnScoreText(state, ship.X, ship.Y, pts, mult);
        }
        else
        {
            ship.DamageState = ShipDamageState.Sinking;
            ship.SinkTimer = 0;
            int pts = ship.BasePoints * mult;
            state.Score += pts;
            state.ShipsSunkThisWave++;
            state.TotalShipsSunk++;
            TrackSinkByType(state, ship.Type);
            SpawnLargeExplosion(state, ship.X, ship.Y);
            SpawnScoreText(state, ship.X, ship.Y, pts, mult);
        }
    }

    private static void HandleBurningKill(GameState state, Ship ship, int mult)
    {
        ship.DamageState = ShipDamageState.Sinking;
        ship.SinkTimer = 0;
        int pts = ship.KillPoints * mult;
        state.Score += pts;
        state.ShipsSunkThisWave++;
        state.TotalShipsSunk++;
        TrackSinkByType(state, ship.Type);
        SpawnLargeExplosion(state, ship.X, ship.Y);
        SpawnScoreText(state, ship.X, ship.Y, pts, mult);
        TryGrantBonusTorpedo(state, ship);
    }

    private static void TrackSinkByType(GameState state, ShipType type)
    {
        int idx = (int)type;
        if (idx >= 0 && idx < state.SinksByType.Length)
            state.SinksByType[idx]++;
        if (type == ShipType.FishingBoat)
            state.CivilianSinks++;
    }

    private static void TryGrantBonusTorpedo(GameState state, Ship ship)
    {
        if (!BonusTorpedoShipTypes.Contains(ship.Type)) return;
        if (state.TorpedoCount >= GameState.MaxTorpedoes) return;
        if (Random.Shared.NextSingle() >= 0.30f) return;

        state.TorpedoCount++;
        state.FloatingTexts.Add(new FloatingText
        {
            X = ship.X,
            Y = ship.Y - 35f,
            Text = "+TORP",
            Color = "#00FFFF",
            Life = 1.8f,
            MaxLife = 1.8f
        });
    }

    private static int GetMultiplier(int combo) => combo switch
    {
        <= 1 => 1,
        <= 3 => 2,
        <= 6 => 3,
        _ => 4
    };

    private static string GetScoreTextColor(int mult) => mult switch
    {
        >= 4 => "#FF6600",
        3 => "#FFD700",
        _ => "#aaffaa"
    };

    private static void SpawnScoreText(GameState state, float x, float y, int points, int mult)
    {
        state.FloatingTexts.Add(new FloatingText
        {
            X = x,
            Y = y - 20f,
            Text = mult > 1 ? $"+{points} x{mult}" : $"+{points}",
            Color = GetScoreTextColor(mult),
            Life = 1.5f,
            MaxLife = 1.5f
        });
    }

    private static void SpawnSmallExplosion(GameState state, float x, float y)
    {
        state.Explosions.Add(new Explosion
        {
            X = x,
            Y = y,
            MaxRadius = 25f,
            Opacity = 1f,
            Sparks = GenerateSparks(x, y, 6)
        });
    }

    private static void SpawnLargeExplosion(GameState state, float x, float y)
    {
        state.ShakeTimer = 0.25f;
        state.Explosions.Add(new Explosion
        {
            X = x,
            Y = y,
            MaxRadius = 55f,
            Opacity = 1f,
            Sparks = GenerateSparks(x, y, 14)
        });
    }

    private static List<ExplosionSpark> GenerateSparks(float x, float y, int count)
    {
        var sparks = new List<ExplosionSpark>(count);
        for (int i = 0; i < count; i++)
        {
            var angle = Random.Shared.NextSingle() * MathF.PI * 2;
            var speed = 50f + Random.Shared.NextSingle() * 120f;
            sparks.Add(new ExplosionSpark
            {
                X = x,
                Y = y,
                Vx = MathF.Cos(angle) * speed,
                Vy = MathF.Sin(angle) * speed,
                Life = 0.5f + Random.Shared.NextSingle() * 0.5f
            });
        }
        return sparks;
    }
}
