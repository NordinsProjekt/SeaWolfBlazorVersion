using GameEngine.Engine.Models;
using GameEngine.Engine.Models.Enums;

namespace GameEngineTests;

public class ShipTests
{
    // ── Ship.Create — base-lane dimensions ───────────────────────────────────

    [Theory]
    [InlineData(ShipType.Destroyer,   130, 45,  100, false)]
    [InlineData(ShipType.PtBoat,       90, 30,  200, false)]
    [InlineData(ShipType.Cargo,       160, 55,  150, true)]
    [InlineData(ShipType.Cruiser,     155, 52,  300, true)]
    [InlineData(ShipType.FishingBoat,  95, 32,   75, false)]
    [InlineData(ShipType.Tanker,      185, 60,  400, true)]
    [InlineData(ShipType.Carrier,     220, 68,  700, true)]
    public void Create_NearLane_HasCorrectDimensions(
        ShipType type, int width, int height, int points, bool twoHits)
    {
        var ship = Ship.Create(type, 1.0f, direction: 1, farLane: false);

        Assert.Equal(type,   ship.Type);
        Assert.Equal(width,  ship.Width);
        Assert.Equal(height, ship.Height);
        Assert.Equal(points, ship.BasePoints);
        Assert.Equal(twoHits, ship.RequiresTwoHits);
        Assert.Equal(1.0f,  ship.DepthScale);
    }

    [Fact]
    public void Create_FarLane_ScalesDimensionsAndPoints()
    {
        var ship = Ship.Create(ShipType.Destroyer, 1.0f, direction: 1, farLane: true);

        Assert.Equal(0.55f, ship.DepthScale);
        Assert.Equal((int)(130 * 0.55f), ship.Width);
        Assert.Equal((int)(45  * 0.55f), ship.Height);
        Assert.Equal((int)(100 * 1.8f),  ship.BasePoints);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void Create_Direction_SetsDirectionAndStartsOffscreen(int dir)
    {
        var ship = Ship.Create(ShipType.Destroyer, 1.0f, direction: dir, farLane: false);

        Assert.Equal(dir, ship.Direction);
        if (dir == 1)
            Assert.True(ship.X < 0);
        else
            Assert.True(ship.X > 1280);
    }

    [Fact]
    public void Create_DifficultyMultiplier_ScalesSpeed()
    {
        var slow = Ship.Create(ShipType.Destroyer, 1.0f);
        var fast = Ship.Create(ShipType.Destroyer, 2.0f);

        Assert.Equal(fast.BaseSpeed, slow.BaseSpeed * 2.0f, precision: 4);
    }

    // ── CurrentSpeed ─────────────────────────────────────────────────────────

    [Fact]
    public void CurrentSpeed_WhenHealthy_EqualBaseSpeed()
    {
        var ship = Ship.Create(ShipType.Destroyer, 1.0f);
        Assert.Equal(ship.BaseSpeed, ship.CurrentSpeed);
    }

    [Fact]
    public void CurrentSpeed_WhenBurning_IsHalfBaseSpeed()
    {
        var ship = Ship.Create(ShipType.Destroyer, 1.0f);
        ship.DamageState = ShipDamageState.Burning;
        Assert.Equal(ship.BaseSpeed * 0.5f, ship.CurrentSpeed, precision: 4);
    }

    // ── HitPoints / KillPoints ───────────────────────────────────────────────

    [Fact]
    public void HitPoints_Is30PercentOfBasePoints()
    {
        var ship = Ship.Create(ShipType.Tanker, 1.0f);
        Assert.Equal((int)(ship.BasePoints * 0.30f), ship.HitPoints);
    }

    [Fact]
    public void KillPoints_Is70PercentOfBasePoints()
    {
        var ship = Ship.Create(ShipType.Tanker, 1.0f);
        Assert.Equal((int)(ship.BasePoints * 0.70f), ship.KillPoints);
    }

    // ── CheckCollision ───────────────────────────────────────────────────────

    [Fact]
    public void CheckCollision_TorpedoAtWaterline_ReturnsTrue()
    {
        var ship = new Ship
        {
            X = 500, Y = 400,
            Width = 130, Height = 45
        };
        // Bottom edge = 400 + 22.5 = 422.5; hit band = max(10, 45*0.3) = 13.5
        // torpedo at exactly the bottom edge should hit
        float bottomEdge = ship.Y + ship.Height / 2f;
        Assert.True(ship.CheckCollision(500, bottomEdge));
    }

    [Fact]
    public void CheckCollision_TorpedoAboveHitBand_ReturnsFalse()
    {
        var ship = new Ship
        {
            X = 500, Y = 400,
            Width = 130, Height = 45
        };
        float bottomEdge = ship.Y + ship.Height / 2f;
        float hitBand    = MathF.Max(10f, ship.Height * 0.30f);
        // Just above the hit band
        float torpY = bottomEdge - hitBand - 1f;
        Assert.False(ship.CheckCollision(500, torpY));
    }

    [Fact]
    public void CheckCollision_TorpedoTooFarRight_ReturnsFalse()
    {
        var ship = new Ship { X = 500, Y = 400, Width = 130, Height = 45 };
        float bottomEdge = ship.Y + ship.Height / 2f;
        Assert.False(ship.CheckCollision(500 + 130, bottomEdge));
    }

    [Fact]
    public void CheckCollision_TorpedoTooFarLeft_ReturnsFalse()
    {
        var ship = new Ship { X = 500, Y = 400, Width = 130, Height = 45 };
        float bottomEdge = ship.Y + ship.Height / 2f;
        Assert.False(ship.CheckCollision(500 - 130, bottomEdge));
    }

    [Fact]
    public void CheckCollision_TorpedoBelowToleranceBand_ReturnsFalse()
    {
        var ship = new Ship { X = 500, Y = 400, Width = 130, Height = 45 };
        float bottomEdge = ship.Y + ship.Height / 2f;
        Assert.False(ship.CheckCollision(500, bottomEdge + 7f));
    }

    // ── Ship.Create — invalid type ───────────────────────────────────────────

    [Fact]
    public void Create_InvalidShipType_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Ship.Create((ShipType)999, 1.0f));
    }

    // ── Ship.Create — direction spawns off-screen ────────────────────────────

    [Theory]
    [InlineData(ShipType.Destroyer)]
    [InlineData(ShipType.PtBoat)]
    [InlineData(ShipType.Cargo)]
    [InlineData(ShipType.Cruiser)]
    [InlineData(ShipType.FishingBoat)]
    [InlineData(ShipType.Tanker)]
    [InlineData(ShipType.Carrier)]
    public void Create_AllTypes_ReturnActiveShip(ShipType type)
    {
        var ship = Ship.Create(type, 1.0f);
        Assert.True(ship.Active);
        Assert.Equal(type, ship.Type);
        Assert.True(ship.Width > 0);
        Assert.True(ship.Height > 0);
        Assert.True(ship.BasePoints > 0);
    }

    // ── Ship.Create — difficulty multiplier scales speed ────────────────────

    [Fact]
    public void Create_DifficultyMultiplier_ScalesBaseSpeed()
    {
        var ship1x = Ship.Create(ShipType.Destroyer, 1.0f);
        var ship2x = Ship.Create(ShipType.Destroyer, 2.0f);
        Assert.Equal(ship1x.BaseSpeed * 2f, ship2x.BaseSpeed, 4);
    }
}
