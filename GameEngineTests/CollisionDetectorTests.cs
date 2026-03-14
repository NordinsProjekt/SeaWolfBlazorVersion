using GameEngine.Engine;
using GameEngine.Engine.Models;
using GameEngine.Engine.Models.Enums;

namespace GameEngineTests;

public class CollisionDetectorTests
{
    private static GameState BuildState() => new();

    private static Ship ShipAt(float x, float y, ShipType type = ShipType.Destroyer,
        bool twoHits = false)
    {
        return new Ship
        {
            X = x,
            Y = y,
            Width = 130,
            Height = 45,
            BasePoints = 100,
            Type = type
        };
    }

    private static Torpedo TorpedoAt(float x, float y) =>
        new() { X = x, Y = y };

    private static (GameState state, Ship ship, Torpedo torpedo) HitScenario(
        bool requiresTwoHits = false)
    {
        var state = BuildState();
        var ship = new Ship
        {
            X = 500,
            Y = 400,
            Width = 130,
            Height = 45,
            BasePoints = 100,
        };
        // Override RequiresTwoHits via a two-hit ship type if requested
        if (requiresTwoHits)
        {
            // Replace with a Cargo ship that RequiresTwoHits
            ship = new Ship
            {
                X = 500,
                Y = 400,
                Width = 160,
                Height = 55,
                BasePoints = 150,
                Type = ShipType.Cargo
            };
        }

        float bottomEdge = ship.Y + ship.Height / 2f;
        var torpedo = TorpedoAt(ship.X, bottomEdge);

        state.Ships.Add(ship);
        state.Torpedoes.Add(torpedo);
        return (state, ship, torpedo);
    }

    // ── Single-hit ship ──────────────────────────────────────────────────────

    [Fact]
    public void Detect_SingleHitShip_TransitionsToSinking()
    {
        var (state, ship, _) = HitScenario();
        CollisionDetector.Detect(state);
        Assert.Equal(ShipDamageState.Sinking, ship.DamageState);
    }

    [Fact]
    public void Detect_SingleHitShip_DeactivatesTorpedo()
    {
        var (state, _, torpedo) = HitScenario();
        CollisionDetector.Detect(state);
        Assert.False(torpedo.Active);
    }

    [Fact]
    public void Detect_SingleHitShip_IncrementsScore()
    {
        var (state, _, _) = HitScenario();
        CollisionDetector.Detect(state);
        Assert.True(state.Score > 0);
    }

    [Fact]
    public void Detect_SingleHitShip_IncrementsSunkCounter()
    {
        var (state, _, _) = HitScenario();
        CollisionDetector.Detect(state);
        Assert.Equal(1, state.ShipsSunkThisWave);
        Assert.Equal(1, state.TotalShipsSunk);
    }

    // ── Two-hit ship ─────────────────────────────────────────────────────────

    [Fact]
    public void Detect_TwoHitShip_FirstHitSetsBurning()
    {
        var state = BuildState();
        var ship = Ship.Create(ShipType.Cargo, 1.0f);
        ship.X = 500;
        ship.Y = 400;

        float bottomEdge = ship.Y + ship.Height / 2f;
        state.Ships.Add(ship);
        state.Torpedoes.Add(TorpedoAt(ship.X, bottomEdge));

        CollisionDetector.Detect(state);

        Assert.Equal(ShipDamageState.Burning, ship.DamageState);
        Assert.Equal(0, state.ShipsSunkThisWave);
    }

    [Fact]
    public void Detect_TwoHitShip_SecondHitSetsSinking()
    {
        var state = BuildState();
        var ship = Ship.Create(ShipType.Cargo, 1.0f);
        ship.X = 500;
        ship.Y = 400;
        ship.DamageState = ShipDamageState.Burning;

        float bottomEdge = ship.Y + ship.Height / 2f;
        state.Ships.Add(ship);
        state.Torpedoes.Add(TorpedoAt(ship.X, bottomEdge));

        CollisionDetector.Detect(state);

        Assert.Equal(ShipDamageState.Sinking, ship.DamageState);
        Assert.Equal(1, state.ShipsSunkThisWave);
    }

    // ── Combo mechanics ──────────────────────────────────────────────────────

    [Fact]
    public void Detect_OnHit_IncrementsComboCount()
    {
        var (state, _, _) = HitScenario();
        CollisionDetector.Detect(state);
        Assert.Equal(1, state.ComboCount);
    }

    [Fact]
    public void Detect_OnHit_SetsComboTimer()
    {
        var (state, _, _) = HitScenario();
        CollisionDetector.Detect(state);
        Assert.Equal(GameState.ComboTimeout, state.ComboTimer);
    }

    [Fact]
    public void Detect_MultipleHits_IncrementsComboCountEachTime()
    {
        var state = BuildState();

        // Add all ships up-front but fire one torpedo at a time so each
        // Detect call processes exactly one hit and ComboCount steps by 1.
        for (int i = 0; i < 3; i++)
        {
            var ship = Ship.Create(ShipType.Destroyer, 1.0f);
            ship.X = 200 + i * 200f;
            ship.Y = 400;
            state.Ships.Add(ship);
        }

        for (int hit = 1; hit <= 3; hit++)
        {
            var target = state.Ships[hit - 1];
            float bottomEdge = target.Y + target.Height / 2f;
            state.Torpedoes.Add(TorpedoAt(target.X, bottomEdge));

            CollisionDetector.Detect(state);
            state.Torpedoes.RemoveAll(t => !t.Active);
            Assert.Equal(hit, state.ComboCount);
        }
    }

    // ── Sinking ships are not re-hit ─────────────────────────────────────────

    [Fact]
    public void Detect_SinkingShip_IsIgnored()
    {
        var state = BuildState();
        var ship = Ship.Create(ShipType.Destroyer, 1.0f);
        ship.X = 500; ship.Y = 400;
        ship.DamageState = ShipDamageState.Sinking;

        float bottomEdge = ship.Y + ship.Height / 2f;
        state.Ships.Add(ship);
        state.Torpedoes.Add(TorpedoAt(ship.X, bottomEdge));

        CollisionDetector.Detect(state);

        Assert.Equal(0, state.Score);
        Assert.True(state.Torpedoes[0].Active); // torpedo not consumed
    }

    // ── TorpedosHit counter ──────────────────────────────────────────────────

    [Fact]
    public void Detect_OnHit_IncrementsTorpedosHit()
    {
        var (state, _, _) = HitScenario();
        CollisionDetector.Detect(state);
        Assert.Equal(1, state.TorpedosHit);
        Assert.Equal(1, state.TotalTorpedosHit);
    }

    // ── Spawns explosion on kill ─────────────────────────────────────────────

    [Fact]
    public void Detect_OnKill_SpawnsExplosion()
    {
        var (state, _, _) = HitScenario();
        CollisionDetector.Detect(state);
        Assert.NotEmpty(state.Explosions);
    }

    // ── Score text pop-up ────────────────────────────────────────────────────

    [Fact]
    public void Detect_OnKill_SpawnsFloatingText()
    {
        var (state, _, _) = HitScenario();
        CollisionDetector.Detect(state);
        Assert.NotEmpty(state.FloatingTexts);
    }

    // ── Score text color by combo tier ───────────────────────────────────────

    [Fact]
    public void Detect_ScoreText_ComboX1_UsesGreenColor()
    {
        var (state, _, _) = HitScenario();
        state.ComboCount = 0; // will become 1 inside Detect → multiplier = 1
        CollisionDetector.Detect(state);
        var scoreText = state.FloatingTexts.First(t => t.Text.StartsWith("+"));
        Assert.Equal("#aaffaa", scoreText.Color);
    }

    [Fact]
    public void Detect_ScoreText_ComboX3_UsesGoldColor()
    {
        // ComboCount = 2 before hit → becomes 3 inside Detect → multiplier = 2
        // We need ComboCount to reach 3 for the gold tier (<=3 => 2x, but color is gold at mult==3)
        // Gold requires mult==3, which triggers at ComboCount 4-6. Set ComboCount=3 before hit → becomes 4 → mult=2
        // Actually: mult 3 comes from ComboCount <= 6 (4,5,6). Set ComboCount=3 → becomes 4 → mult=2 (silver)
        // Set ComboCount=5 → becomes 6 → mult=3 → gold color
        var (state, _, _) = HitScenario();
        state.ComboCount = 5;
        CollisionDetector.Detect(state);
        var scoreText = state.FloatingTexts.First(t => t.Text.StartsWith("+"));
        Assert.Equal("#FFD700", scoreText.Color);
    }

    [Fact]
    public void Detect_ScoreText_ComboX4_UsesOrangeColor()
    {
        // ComboCount >= 7 after hit → multiplier = 4 → orange
        var (state, _, _) = HitScenario();
        state.ComboCount = 6;
        CollisionDetector.Detect(state);
        var scoreText = state.FloatingTexts.First(t => t.Text.StartsWith("+"));
        Assert.Equal("#FF6600", scoreText.Color);
    }

    // ── Bonus torpedo drop — eligible ship types ─────────────────────────────

    private static (GameState state, Ship ship) BurningShipScenario(ShipType type)
    {
        var state = BuildState();
        var ship = Ship.Create(type, 1.0f);
        ship.X = 500;
        ship.Y = 400;
        ship.DamageState = ShipDamageState.Burning;
        state.Ships.Add(ship);
        float bottomEdge = ship.Y + ship.Height / 2f;
        state.Torpedoes.Add(TorpedoAt(ship.X, bottomEdge));
        return (state, ship);
    }

    [Theory]
    [InlineData(ShipType.Destroyer)]
    [InlineData(ShipType.PtBoat)]
    [InlineData(ShipType.FishingBoat)]
    public void Detect_IneligibleShipType_BurningKill_NeverDropsBonusTorpedo(ShipType type)
    {
        int initialTorpedoes = -1;
        for (int i = 0; i < 200; i++)
        {
            var (state, _) = BurningShipScenario(type);
            state.TorpedoCount = 0;
            if (initialTorpedoes < 0) initialTorpedoes = state.TorpedoCount;
            CollisionDetector.Detect(state);
            Assert.Equal(0, state.TorpedoCount);
        }
    }

    [Theory]
    [InlineData(ShipType.Cargo)]
    [InlineData(ShipType.Cruiser)]
    [InlineData(ShipType.Tanker)]
    [InlineData(ShipType.Carrier)]
    public void Detect_EligibleShipBurning_AtMaxTorpedoes_NeverDropsBonusTorpedo(ShipType type)
    {
        for (int i = 0; i < 200; i++)
        {
            var (state, _) = BurningShipScenario(type);
            state.TorpedoCount = GameState.MaxTorpedoes;
            CollisionDetector.Detect(state);
            Assert.Equal(GameState.MaxTorpedoes, state.TorpedoCount);
        }
    }

    [Theory]
    [InlineData(ShipType.Cargo)]
    [InlineData(ShipType.Cruiser)]
    [InlineData(ShipType.Tanker)]
    [InlineData(ShipType.Carrier)]
    public void Detect_EligibleShipBurning_BelowMaxTorpedoes_CanDropBonusTorpedo(ShipType type)
    {
        // Run enough iterations that at least one 30% drop occurs
        bool dropped = false;
        for (int i = 0; i < 500 && !dropped; i++)
        {
            var (state, _) = BurningShipScenario(type);
            state.TorpedoCount = 0;
            CollisionDetector.Detect(state);
            if (state.TorpedoCount == 1) dropped = true;
        }
        Assert.True(dropped, $"{type} should drop a bonus torpedo within 500 attempts");
    }
}
