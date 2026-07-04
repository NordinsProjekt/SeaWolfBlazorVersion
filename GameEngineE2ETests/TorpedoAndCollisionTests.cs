using GameEngine.Engine;
using GameEngine.Engine.Models;
using GameEngine.Engine.Models.Enums;

namespace GameEngineE2ETests;

/// <summary>
/// End-to-end tests for torpedoes: firing, trajectory, out-of-bounds removal,
/// collision detection (hit/sink) via real Update() calls.
/// </summary>
public class TorpedoAndCollisionTests
{
    // Ship.CheckCollision requires torpY to be inside the waterline band:
    //   bottomEdge - hitBand <= torpY <= bottomEdge + tolerance
    // where bottomEdge = Y + Height/2, hitBand = max(10, Height*0.30), tolerance = 6.
    // Placing at (bottomEdge - 1) is always inside the band.
    private static float WaterlineY(Ship ship) => ship.Y + ship.Height / 2f - 1f;

    [Fact]
    public void Torpedo_IsRemovedWhenAboveCanvas()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        sim.Engine.FireTorpedoFromTube(2); // any tube — trajectory isn't checked here
        var torp = sim.State.Torpedoes[0];

        // Run until torpedo leaves the top of the canvas (y < -50)
        sim.RunUntil(() => !sim.State.Torpedoes.Contains(torp), maxFrames: 500);

        Assert.DoesNotContain(torp, sim.State.Torpedoes);
    }

    [Fact]
    public void Torpedo_MovesUpward_EachFrame()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        sim.Engine.FireTorpedoFromTube(2);
        var torp    = sim.State.Torpedoes[0];
        float startY = torp.Y;

        sim.Engine.Update(GameSimulator.FrameDtPublic);

        Assert.True(torp.Y < startY, "Torpedo should move upward (decreasing Y)");
    }

    [Fact]
    public void Torpedo_HitsShip_SingleHit_TransitionsToBurning()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        // Place a two-hit ship (Tanker) directly in the torpedo path
        var ship = Ship.Create(ShipType.Tanker, 1f, 1, false);
        ship.X = 640f;
        ship.Y = 400f;
        sim.State.Ships.Add(ship);

        // Place torpedo at the waterline so CheckCollision returns true
        var torp = new Torpedo { X = ship.X, Y = WaterlineY(ship), Vx = 0f, Vy = -5f };
        sim.State.Torpedoes.Add(torp);

        sim.Engine.Update(GameSimulator.FrameDtPublic);

        Assert.Equal(ShipDamageState.Burning, ship.DamageState);
    }

    [Fact]
    public void Torpedo_HitsShip_TwoHitShip_SecondHitSinks()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        var ship = Ship.Create(ShipType.Tanker, 1f, 1, false);
        ship.X = 640f;
        ship.Y = 400f;
        sim.State.Ships.Add(ship);

        // First hit
        sim.State.Torpedoes.Add(new Torpedo { X = ship.X, Y = WaterlineY(ship), Vx = 0f, Vy = -5f });
        sim.Engine.Update(GameSimulator.FrameDtPublic);
        Assert.Equal(ShipDamageState.Burning, ship.DamageState);

        // Second hit
        sim.State.Torpedoes.Add(new Torpedo { X = ship.X, Y = WaterlineY(ship), Vx = 0f, Vy = -5f });
        sim.Engine.Update(GameSimulator.FrameDtPublic);
        Assert.Equal(ShipDamageState.Sinking, ship.DamageState);
    }

    [Fact]
    public void Torpedo_HitsSingleHitShip_SinksImmediately()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        var ship = Ship.Create(ShipType.Destroyer, 1f, 1, false);
        ship.X = 640f;
        ship.Y = 400f;
        sim.State.Ships.Add(ship);

        sim.State.Torpedoes.Add(new Torpedo { X = ship.X, Y = WaterlineY(ship), Vx = 0f, Vy = -5f });
        sim.Engine.Update(GameSimulator.FrameDtPublic);

        Assert.Equal(ShipDamageState.Sinking, ship.DamageState);
    }

    [Fact]
    public void Torpedo_OnKill_IncrementsScore()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        int scoreBefore = sim.State.Score;
        var ship = Ship.Create(ShipType.Destroyer, 1f, 1, false);
        ship.X = 640f;
        ship.Y = 400f;
        sim.State.Ships.Add(ship);

        sim.State.Torpedoes.Add(new Torpedo { X = ship.X, Y = WaterlineY(ship), Vx = 0f, Vy = -5f });
        sim.Engine.Update(GameSimulator.FrameDtPublic);

        Assert.True(sim.State.Score > scoreBefore);
    }

    [Fact]
    public void Torpedo_OnKill_SpawnsExplosion()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        var ship = Ship.Create(ShipType.Destroyer, 1f, 1, false);
        ship.X = 640f;
        ship.Y = 400f;
        sim.State.Ships.Add(ship);

        sim.State.Torpedoes.Add(new Torpedo { X = ship.X, Y = WaterlineY(ship), Vx = 0f, Vy = -5f });
        sim.Engine.Update(GameSimulator.FrameDtPublic);

        Assert.NotEmpty(sim.State.Explosions);
    }

    [Fact]
    public void Explosion_IsRemovedAfterExpanding()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        var ship = Ship.Create(ShipType.Destroyer, 1f, 1, false);
        ship.X = 640f;
        ship.Y = 400f;
        sim.State.Ships.Add(ship);

        sim.State.Torpedoes.Add(new Torpedo { X = ship.X, Y = WaterlineY(ship), Vx = 0f, Vy = -5f });
        sim.Engine.Update(GameSimulator.FrameDtPublic);

        Assert.NotEmpty(sim.State.Explosions);
        sim.RunUntil(() => sim.State.Explosions.Count == 0, maxFrames: 500);
        Assert.Empty(sim.State.Explosions);
    }

    [Fact]
    public void Torpedo_CannotFire_WhenNotPlaying()
    {
        var sim = new GameSimulator();
        // Status is StartScreen
        bool fired = sim.Engine.FireTorpedoFromTube(2);
        Assert.False(fired);
        Assert.Empty(sim.State.Torpedoes);
    }

    [Fact]
    public void Torpedo_CannotFire_WhenReloading()
    {
        var sim = new GameSimulator();
        sim.StartArcade();

        sim.Engine.FireTorpedoFromTube(2);
        sim.Engine.FireTorpedoFromTube(2);
        Assert.True(sim.State.IsReloading);

        bool fired = sim.Engine.FireTorpedoFromTube(2);
        Assert.False(fired);
    }
}
