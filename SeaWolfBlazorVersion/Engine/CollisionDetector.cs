using SeaWolfBlazorVersion.Engine.Models;
using SeaWolfBlazorVersion.Engine.Models.Enums;

namespace SeaWolfBlazorVersion.Engine;

public static class CollisionDetector
{
    public static void Detect(GameState state)
    {
        foreach (var torpedo in state.Torpedoes.Where(t => t.Active))
        {
            foreach (var ship in state.Ships.Where(s =>
                s.Active && s.DamageState != ShipDamageState.Sinking))
            {
                if (!ship.CheckCollision(torpedo.X, torpedo.Y)) continue;

                torpedo.Active = false;

                // Advance combo
                state.ComboCount++;
                state.ComboTimer = GameState.ComboTimeout;
                state.TorpedosHit++;
                int mult = GetMultiplier(state.ComboCount);

                if (ship.DamageState == ShipDamageState.Healthy)
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
                        SpawnLargeExplosion(state, ship.X, ship.Y);
                        SpawnScoreText(state, ship.X, ship.Y, pts, mult);
                    }
                }
                else if (ship.DamageState == ShipDamageState.Burning)
                {
                    ship.DamageState = ShipDamageState.Sinking;
                    ship.SinkTimer = 0;
                    int pts = ship.KillPoints * mult;
                    state.Score += pts;
                    state.ShipsSunkThisWave++;
                    SpawnLargeExplosion(state, ship.X, ship.Y);
                    SpawnScoreText(state, ship.X, ship.Y, pts, mult);

                    // Cargo kill: 30% chance to drop a bonus torpedo
                    if (ship.Type == ShipType.Cargo
                        && Random.Shared.NextSingle() < 0.30f
                        && state.TorpedoCount < GameState.MaxTorpedoes)
                    {
                        state.TorpedoCount++;
                        state.FloatingTexts.Add(new FloatingText
                        {
                            X = ship.X, Y = ship.Y - 35f,
                            Text = "+TORP", Color = "#00FFFF",
                            Life = 1.8f, MaxLife = 1.8f
                        });
                    }
                }

                break; // one torpedo hits one ship per frame
            }
        }
    }

    private static int GetMultiplier(int combo) => combo switch
    {
        <= 1 => 1,
        <= 3 => 2,
        <= 6 => 3,
        _    => 4
    };

    private static void SpawnScoreText(GameState state, float x, float y, int points, int mult)
    {
        var text = mult > 1 ? $"+{points} ×{mult}" : $"+{points}";
        state.FloatingTexts.Add(new FloatingText
        {
            X = x, Y = y - 20f, Text = text,
            Color = mult >= 4 ? "#FF6600" : mult == 3 ? "#FFD700" : "#aaffaa",
            Life = 1.5f, MaxLife = 1.5f
        });
    }

    private static void SpawnSmallExplosion(GameState state, float x, float y)
    {
        state.Explosions.Add(new Explosion
        {
            X = x, Y = y,
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
            X = x, Y = y,
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
                X = x, Y = y,
                Vx = MathF.Cos(angle) * speed,
                Vy = MathF.Sin(angle) * speed,
                Life = 0.5f + Random.Shared.NextSingle() * 0.5f
            });
        }
        return sparks;
    }
}
